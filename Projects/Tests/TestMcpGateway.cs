//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.AI;
using Seal.Model;
using System;
using System.IO;
using System.Linq;

namespace Test
{
    /// <summary>
    /// MCP server independent of the transport: access tokens, authentication of a request, agents of the MCP channel, limit of the calls
    /// </summary>
    [TestClass]
    public class TestMcpGateway
    {
        static Repository _repository;
        static string _folder;
        static SecurityLogin _login;
        static SecurityGroup _group;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Instance;
            _folder = Path.Combine(Path.GetTempPath(), "seal_test_mcp_" + Guid.NewGuid().ToString("N"));
            McpGateway.TokenStore = new McpFileTokenStore(_folder);
            //A login of a group having AI agents, authenticated by the Basic Authentication provider
            _group = _repository.Security.Groups.First(g => g.AgentGUIDs.Count > 0);
            _login = new SecurityLogin() { Id = "mcp_test_user", GroupIds = new System.Collections.Generic.List<string>() { _group.GUID } };
            _repository.Security.Logins.Add(_login);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _repository.Security.Logins.Remove(_login);
            _repository.AIConfiguration.McpServerEnabled = false;
            McpGateway.TokenStore = null;
            if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        }

        [TestMethod]
        public void TokenStore()
        {
            var store = new McpFileTokenStore(Path.Combine(_folder, "store"));
            var token = store.Create("someone", "Claude Code", DateTime.Now.AddDays(30), out var clear);
            StringAssert.StartsWith(clear, McpFileTokenStore.TokenPrefix);
            Assert.IsFalse(File.ReadAllText(Directory.GetFiles(Path.Combine(_folder, "store")).Single()).Contains(clear), "the clear token must not be stored");

            Assert.AreEqual(token.Id, store.Find(clear)?.Id);
            Assert.IsNull(store.Find(clear + "x"));
            Assert.IsNull(store.Find("not a token"));
            Assert.AreEqual(1, store.GetUserTokens("SOMEONE").Count, "user names are not case sensitive");
            Assert.AreEqual(0, store.GetUserTokens("other").Count);
            store.Create("other", "Copilot", DateTime.Now.AddDays(30), out _);
            Assert.AreEqual(2, store.GetAllTokens().Count, "inventory of the tokens of all the users");
            Assert.AreEqual(1, store.GetUserTokens("other").Count);

            //Validity: expiration and inactivity
            var now = DateTime.Now;
            Assert.IsNull(token.CheckValidity(now, 0));
            Assert.IsNotNull(token.CheckValidity(now.AddDays(31), 0));
            Assert.IsNotNull(token.CheckValidity(now.AddDays(11), 10));
            store.Touch(token, now.AddDays(5));
            Assert.AreEqual(now.AddDays(5), store.Find(clear).LastUse, "first use written");
            Assert.IsNull(store.Find(clear).CheckValidity(now.AddDays(11), 10));
            store.Touch(token, now.AddDays(5).AddMinutes(1));
            Assert.AreEqual(now.AddDays(5), store.Find(clear).LastUse, "a use within the write interval is kept in memory");

            //Revocation by the owner only
            Assert.IsFalse(store.Revoke("other", token.Id));
            Assert.IsTrue(store.Revoke("someone", token.Id));
            Assert.IsNull(store.Find(clear));
            store.Touch(token, now.AddDays(6));
            Assert.IsNull(store.Find(clear), "a revoked token is not written again");
        }

        [TestMethod]
        public void AgentSlug()
        {
            Assert.AreEqual("report-designer", McpGateway.GetAgentSlug(new AIAgentConfiguration() { Name = "Report Designer" }));
            Assert.AreEqual("schedule-output", McpGateway.GetAgentSlug(new AIAgentConfiguration() { Name = "Schedule & Output" }));
            Assert.AreEqual("donnees-ete", McpGateway.GetAgentSlug(new AIAgentConfiguration() { Name = " Données été " }));
        }

        [TestMethod]
        public void AuthenticateRequest()
        {
            var agent = _repository.AIConfiguration.AIAgents.First(a => _group.AgentGUIDs.Contains(a.GUID) && a.IsEnabled);
            var slug = McpGateway.GetAgentSlug(agent);
            var channel = agent.Channel;
            var token = McpGateway.TokenStore.Create(_login.Id, "test", DateTime.Now.AddDays(1), out var clear);
            try
            {
                //Server not enabled: not found
                _repository.AIConfiguration.McpServerEnabled = false;
                Assert.IsNull(McpGateway.Authenticate("Bearer " + clear, slug, null, out var error, out var unauthorized));
                Assert.IsFalse(unauthorized);
                _repository.AIConfiguration.McpServerEnabled = true;

                //No token, invalid token
                Assert.IsNull(McpGateway.Authenticate(null, slug, null, out error, out unauthorized));
                Assert.IsTrue(unauthorized);
                Assert.IsNull(McpGateway.Authenticate("Bearer " + clear + "x", slug, null, out error, out unauthorized));
                Assert.IsTrue(unauthorized);

                //Valid token: user loaded from his name without password, with the groups of his login
                agent.Channel = AIAgentChannel.WebAndMcp;
                var context = McpGateway.Authenticate("Bearer " + clear, slug, "10.0.0.1", out error, out unauthorized, "claude-code/2.1");
                Assert.IsNotNull(context?.Agent, error);
                Assert.AreEqual("10.0.0.1", context.ClientAddress);
                Assert.AreEqual("claude-code/2.1", context.ClientApplication, "client audited with the tool calls");
                Assert.AreEqual(agent.GUID, context.Agent.GUID);
                Assert.IsTrue(context.User.SecurityGroups.Contains(_group));
                Assert.IsTrue(context.User.ForSchedule);

                //Unknown agent, agent served only on the web chat: not found
                Assert.IsNull(McpGateway.Authenticate("Bearer " + clear, "no-such-agent", null, out error, out unauthorized)?.Agent);
                Assert.IsFalse(unauthorized);
                agent.Channel = AIAgentChannel.Web;
                Assert.IsNull(McpGateway.Authenticate("Bearer " + clear, slug, null, out error, out unauthorized)?.Agent);
                Assert.IsFalse(context.User.McpAgentConfigurations.Contains(agent));
                Assert.IsTrue(context.User.AgentConfigurations.Contains(agent));
                agent.Channel = AIAgentChannel.Mcp;
                Assert.IsNotNull(McpGateway.Authenticate("Bearer " + clear, slug, null, out error, out unauthorized)?.Agent);
                Assert.IsFalse(context.User.AgentConfigurations.Contains(agent), "an agent of the MCP channel is not in the web chat");

                //Login removed from the group: refused even with a valid token
                _login.GroupIds.Clear();
                Assert.IsNull(McpGateway.Authenticate("Bearer " + clear, slug, null, out error, out unauthorized));
                Assert.IsTrue(unauthorized, error);
                _login.GroupIds.Add(_group.GUID);

                //Instructions and tools: all the tools of the agent and of its skills
                agent.Channel = AIAgentChannel.WebAndMcp;
                context = McpGateway.Authenticate("Bearer " + clear, slug, null, out error, out unauthorized);
                StringAssert.Contains(McpGateway.GetInstructions(context), "Seal Report MCP server");
                var tools = McpGateway.GetToolHost(context).GetTools();
                var skillTools = agent.GetSkillConfigurations().SelectMany(s => s.GetToolConfigurations()).Where(t => !agent.ToolGUIDs.Contains(t.GUID)).ToList();
                foreach (var t in skillTools) Assert.IsTrue(tools.Any(i => i.Name == t.Name && i.Description.StartsWith("Requires the skill")), $"tool '{t.Name}' missing");
                if (agent.GetSkillConfigurations().Count > 0) Assert.IsTrue(tools.Any(t => t.Name == AIToolHost.LoadSkillToolName));

                //Long system prompt: the instructions are an extract under the limit of the clients, the complete prompt is returned by load_skill('instructions')
                var host = McpGateway.GetToolHost(context);
                var fullPrompt = AIAgent.CreateWithoutProvider(context.Agent, context.User).SystemPrompt;
                StringAssert.Contains(host.GetTools().First(t => t.Name == AIToolHost.LoadSkillToolName).ParametersSchema, AIToolHost.InstructionsSkillName);
                var loaded = host.Execute(new AIToolCall() { Name = AIToolHost.LoadSkillToolName, Arguments = "{\"skill_name\":\"instructions\"}" });
                Assert.IsTrue(loaded.IsSkillLoad);
                Assert.AreEqual(fullPrompt, loaded.Text);
                var maxLength = McpGateway.McpInstructionsMaxLength;
                try
                {
                    McpGateway.McpInstructionsMaxLength = 1000;
                    var instructions = McpGateway.GetInstructions(context);
                    if (fullPrompt.Length > 1000)
                    {
                        Assert.IsTrue(instructions.Length <= 1000, $"instructions of {instructions.Length} characters");
                        Assert.IsTrue(instructions.StartsWith("IMPORTANT"), instructions);
                    }
                    StringAssert.Contains(instructions, "Seal Report MCP server");
                }
                finally
                {
                    McpGateway.McpInstructionsMaxLength = maxLength;
                }
            }
            finally
            {
                agent.Channel = channel;
                McpGateway.TokenStore.Revoke(_login.Id, token.Id);
            }
        }

        [TestMethod]
        public void ToolAccess()
        {
            //Every tool of the configuration declares its effect (published as MCP tool annotations)
            var unspecified = _repository.AIConfiguration.AITools.Where(t => t.Access == AIToolAccess.Unspecified).Select(t => t.Name).ToList();
            Assert.AreEqual(0, unspecified.Count, "tools without access: " + string.Join(", ", unspecified));
            Assert.AreEqual(AIToolAccess.ReadOnly, _repository.AIConfiguration.AITools.First(t => t.Name == "report_list").ToAITool().Access);
            Assert.AreEqual(AIToolAccess.Destructive, _repository.AIConfiguration.AITools.First(t => t.Name == "file_manage").ToAITool().Access);
        }

        [TestMethod]
        public void PromptsAndResources()
        {
            Assert.AreEqual("seal://report/Reports/Sales/my%20report.srex", McpGateway.GetReportResourceUri(@"Reports\Sales\my report.srex"));

            //Agent of the group having the most tools describing the resources
            var agent = _repository.AIConfiguration.AIAgents.Where(a => _group.AgentGUIDs.Contains(a.GUID) && a.IsEnabled)
                .OrderByDescending(a => new AIToolHost(a).GetTools().Count(t => t.Name == McpGateway.ReportResourceTool || t.Name == McpGateway.SourceResourceTool))
                .First();
            var channel = agent.Channel;
            var serverEnabled = _repository.AIConfiguration.McpServerEnabled;
            var token = McpGateway.TokenStore.Create(_login.Id, "test", DateTime.Now.AddDays(1), out var clear);
            try
            {
                _repository.AIConfiguration.McpServerEnabled = true;
                agent.Channel = AIAgentChannel.WebAndMcp;
                var context = McpGateway.Authenticate("Bearer " + clear, McpGateway.GetAgentSlug(agent), null, out var error, out _);
                Assert.IsNotNull(context?.Agent, error);

                //Annotations: the built-in load_skill only reads
                var tools = McpGateway.GetToolHost(context).GetTools();
                if (agent.GetSkillConfigurations().Count > 0) Assert.AreEqual(AIToolAccess.ReadOnly, tools.First(t => t.Name == AIToolHost.LoadSkillToolName).Access);

                //Prompts: the sample prompts of the agent
                var prompts = McpGateway.GetPrompts(context);
                Assert.AreEqual(AIAgent.CreateWithoutProvider(agent, context.User).GetSamplePrompts().Count(p => !string.IsNullOrWhiteSpace(p)), prompts.Count);
                for (int i = 0; i < prompts.Count; i++) Assert.AreEqual("sample-" + (i + 1), prompts[i].Name);

                //Resources: reports and data sources described by the tools of the agent
                var names = tools.Select(t => t.Name).ToList();
                var templates = McpGateway.GetResourceTemplates(context);
                Assert.AreEqual(names.Contains(McpGateway.ReportResourceTool), templates.Any(t => t.UriTemplate.StartsWith(McpGateway.ReportResourcePrefix)));
                Assert.AreEqual(names.Contains(McpGateway.SourceResourceTool), templates.Any(t => t.UriTemplate.StartsWith(McpGateway.SourceResourcePrefix)));
                var resources = McpGateway.GetResources(context);
                Assert.IsTrue(resources.Count <= McpGateway.MaxReportResources + _repository.Sources.Count);
                var report = resources.FirstOrDefault(r => r.Uri.StartsWith(McpGateway.ReportResourcePrefix));
                if (names.Contains(McpGateway.ReportResourceTool))
                {
                    Assert.IsNotNull(report, "the reports of the user are listed");
                    var result = McpGateway.ReadResource(context, report.Uri, out error);
                    Assert.IsNotNull(result, error);
                    Assert.IsFalse(McpGateway.IsError(result), result.Text);
                }
                var source = resources.FirstOrDefault(r => r.Uri.StartsWith(McpGateway.SourceResourcePrefix));
                if (source != null)
                {
                    var result = McpGateway.ReadResource(context, source.Uri, out error);
                    Assert.IsFalse(McpGateway.IsError(result), result.Text);
                }
                Assert.IsNull(McpGateway.ReadResource(context, "seal://other/x", out error));
                Assert.IsNotNull(error);
            }
            finally
            {
                agent.Channel = channel;
                _repository.AIConfiguration.McpServerEnabled = serverEnabled;
                McpGateway.TokenStore.Revoke(_login.Id, token.Id);
            }
        }

        [TestMethod]
        public void UserLimits()
        {
            var agent = _repository.AIConfiguration.AIAgents.First(a => _group.AgentGUIDs.Contains(a.GUID) && a.IsEnabled);
            var channel = agent.Channel;
            var serverEnabled = _repository.AIConfiguration.McpServerEnabled;
            var token = McpGateway.TokenStore.Create(_login.Id, "test", DateTime.Now.AddDays(1), out var clear);
            try
            {
                _repository.AIConfiguration.McpServerEnabled = true;
                agent.Channel = AIAgentChannel.WebAndMcp;
                var context = McpGateway.Authenticate("Bearer " + clear, McpGateway.GetAgentSlug(agent), null, out var error, out _);
                Assert.IsNotNull(context?.Agent, error);

                //Limit set by the Security Provider: the tool is not executed and not counted
                var calls = new System.Collections.Generic.List<string>();
                context.User.CheckMcpCall = tool => tool == "refused_tool" ? "The limit of 500 calls per day is reached." : null;
                context.User.OnMcpCall = (tool, isError) => calls.Add(tool + ":" + isError);
                var result = McpGateway.ExecuteTool(context, "refused_tool", "{}");
                Assert.IsTrue(McpGateway.IsError(result));
                StringAssert.Contains(result.Text, "The limit of 500 calls per day is reached.");
                Assert.AreEqual(0, calls.Count);

                //Executed call notified, here with an error (unknown tool)
                McpGateway.ExecuteTool(context, "no_such_tool", "{}");
                CollectionAssert.AreEqual(new[] { "no_such_tool:True" }, calls);

                //Error of the check script: refused
                context.User.CheckMcpCall = tool => throw new Exception("database down");
                Assert.IsTrue(McpGateway.IsError(McpGateway.ExecuteTool(context, "no_such_tool", "{}")));
                Assert.AreEqual(1, calls.Count);
            }
            finally
            {
                agent.Channel = channel;
                _repository.AIConfiguration.McpServerEnabled = serverEnabled;
                McpGateway.TokenStore.Revoke(_login.Id, token.Id);
            }
        }

        [TestMethod]
        public void CallRate()
        {
            var max = _repository.AIConfiguration.McpMaxCallsPerMinute;
            try
            {
                _repository.AIConfiguration.McpMaxCallsPerMinute = 3;
                var now = DateTime.Now;
                var user = "rate_" + Guid.NewGuid().ToString("N");
                for (int i = 0; i < 3; i++) Assert.IsTrue(McpGateway.CheckCallRate(user, now.AddSeconds(i)));
                Assert.IsFalse(McpGateway.CheckCallRate(user, now.AddSeconds(10)));
                Assert.IsTrue(McpGateway.CheckCallRate("other_" + user, now.AddSeconds(10)));
                Assert.IsTrue(McpGateway.CheckCallRate(user, now.AddSeconds(61)), "the first call is more than one minute old");
            }
            finally
            {
                _repository.AIConfiguration.McpMaxCallsPerMinute = max;
            }
        }
    }
}
