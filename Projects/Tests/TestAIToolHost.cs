//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seal.AI;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Test
{
    /// <summary>
    /// Execution of the AI tools independently of the provider: AIToolHost (tools of an agent, load_skill, errors) and concurrent calls of a shared tool configuration
    /// </summary>
    [TestClass]
    public class TestAIToolHost
    {
        static Repository _repository;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Instance;
        }

        /// <summary>Adds a tool running <paramref name="script"/> to the repository and returns an agent having only this tool</summary>
        static AIAgentConfiguration agentWithScript(string name, string script, out AIToolConfiguration tool, out string scriptPath)
        {
            scriptPath = Path.Combine(Path.GetTempPath(), $"seal_test_{name}_{Guid.NewGuid():N}.cshtml");
            File.WriteAllText(scriptPath, script);
            tool = new AIToolConfiguration() { Name = name, Description = "Test tool", ExecutionScriptFile = scriptPath };
            _repository.AIConfiguration.AITools.Add(tool);
            return new AIAgentConfiguration() { Name = "Test agent", ToolGUIDs = new List<string>() { tool.GUID } };
        }

        static void removeScript(AIToolConfiguration tool, string scriptPath)
        {
            _repository.AIConfiguration.AITools.Remove(tool);
            File.Delete(scriptPath);
        }

        [TestMethod]
        public void ConcurrentCallsOfSameTool()
        {
            //The script reads its call after a pause: with a shared CurrentToolCall, a concurrent call would replace it
            var agent = agentWithScript("test_echo", "@{ Seal.AI.AIToolConfiguration tool = Model; var arguments = tool.CurrentToolCall.Arguments; System.Threading.Thread.Sleep(50); tool.ExecResult = arguments + \"|\" + tool.CurrentToolCall.Arguments; }", out var tool, out var scriptPath);
            try
            {
                tool.Execute(new AIToolCall() { Arguments = "warmup" }); //compilation
                var results = new string[20];
                Parallel.For(0, results.Length, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, i =>
                {
                    results[i] = tool.Execute(new AIToolCall() { Arguments = "call" + i });
                });
                for (int i = 0; i < results.Length; i++) Assert.AreEqual($"call{i}|call{i}", results[i]);
                Assert.IsNull(tool.CurrentToolCall, "the shared configuration must not keep the call");
                Assert.IsNull(tool.ExecResult, "the shared configuration must not keep the result");
            }
            finally
            {
                removeScript(tool, scriptPath);
            }
        }

        [TestMethod]
        public void SkillProgressiveDisclosure()
        {
            //An agent having a skill that unlocks a tool not in its base tools
            var agent = _repository.AIConfiguration.AIAgents.FirstOrDefault(a => a.IsEnabled && a.GetSkillConfigurations().Any(s => s.GetToolConfigurations().Any(t => !a.ToolGUIDs.Contains(t.GUID))));
            Assert.IsNotNull(agent, "no agent with a skill unlocking a tool");
            var skill = agent.GetSkillConfigurations().First(s => s.GetToolConfigurations().Any(t => !agent.ToolGUIDs.Contains(t.GUID)));
            var skillTool = skill.GetToolConfigurations().First(t => !agent.ToolGUIDs.Contains(t.GUID));

            var host = new AIToolHost(agent);
            Assert.IsTrue(host.HasTools);
            var tools = host.GetTools();
            Assert.IsTrue(tools.Any(t => t.Name == AIToolHost.LoadSkillToolName));
            Assert.IsFalse(tools.Any(t => t.Name == skillTool.Name), $"'{skillTool.Name}' must not be active before the skill is loaded");

            //Tool of a skill not loaded: actionable error, never an empty result
            var result = host.Execute(new AIToolCall() { Name = skillTool.Name, Arguments = "{}" });
            StringAssert.StartsWith(result.Text, "Error:");
            StringAssert.Contains(result.Text, AIToolHost.LoadSkillToolName);
            Assert.IsFalse(result.IsSkillLoad);

            //Load the skill: instructions returned and tool unlocked
            var loadCall = new AIToolCall() { Name = AIToolHost.LoadSkillToolName, Arguments = "{\"skill_name\":\"" + skill.Name + "\"}" };
            result = host.Execute(loadCall);
            Assert.IsTrue(result.IsSkillLoad);
            Assert.IsTrue(result.ToolsChanged);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Text));
            Assert.IsTrue(host.LoadedSkills.Contains(skill.Name));
            Assert.IsTrue(host.GetTools().Any(t => t.Name == skillTool.Name));

            //Loaded again: same instructions, no new tools
            result = host.Execute(loadCall);
            Assert.IsFalse(result.ToolsChanged);

            //Unknown skill
            result = host.Execute(new AIToolCall() { Name = AIToolHost.LoadSkillToolName, Arguments = "{\"skill_name\":\"no-such-skill\"}" });
            StringAssert.StartsWith(result.Text, "Unknown skill 'no-such-skill'");

            //A skill activated for a resumed conversation
            var resumed = new AIToolHost(agent);
            Assert.IsTrue(resumed.ActivateSkill(skill.Name));
            Assert.IsFalse(resumed.ActivateSkill(skill.Name));
            Assert.IsTrue(resumed.GetTools().Any(t => t.Name == skillTool.Name));
        }

        [TestMethod]
        public void ToolContextAndErrors()
        {
            var agent = agentWithScript("test_context", "@{ Seal.AI.AIToolConfiguration tool = Model; if (tool.CurrentToolCall.Arguments == \"crash\") { throw new System.Exception(\"script failure\"); } tool.ExecResult = (tool.CurrentToolCall.SecurityContext?.WebUserName == \"test_user\" && tool.CurrentToolCall.AgentConfiguration?.Name == \"Test agent\" ? \"context\" : \"no context\") + \"|\" + new string('x', 300000); }", out var tool, out var scriptPath);
            try
            {
                var user = new SecurityUser(_repository.Security) { WebUserName = "test_user" };
                var host = new AIToolHost(agent) { SecurityContext = user };
                Assert.IsFalse(host.GetTools().Any(t => t.Name == AIToolHost.LoadSkillToolName), "no load_skill without skills");

                //The security context and the agent of the host are given to the call, the result is truncated
                var result = host.Execute(new AIToolCall() { Name = "test_context", Arguments = "ok" });
                StringAssert.StartsWith(result.Text, "context|");
                StringAssert.Contains(result.Text, "[Result truncated:");
                Assert.IsTrue(result.Text.Length < 300000);

                //A script failure becomes an error text
                result = host.Execute(new AIToolCall() { Name = "test_context", Arguments = "crash" });
                StringAssert.StartsWith(result.Text, "Error: tool 'test_context' failed: script failure");
                Assert.IsNotNull(result.Error);
            }
            finally
            {
                removeScript(tool, scriptPath);
            }
        }
    }
}
