//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenAI.Chat;
using Seal.AI;
using Seal.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    /// <summary>
    /// Limits of the AI agents set by the Security Provider on the user: check before a chat message, AI budget stopping the tool loop, consumption notified
    /// </summary>
    [TestClass]
    public class TestAIUsageLimits
    {
        /// <summary>
        /// Provider costing 1 million input tokens per call, requesting a tool call for the first calls then answering
        /// </summary>
        class TestProvider : IAIProvider
        {
            public int Calls = 0;
            public int ToolCallReplies = int.MaxValue;
            /// <summary>Optional tool calls of the replies (name, arguments), one per reply; by default a different call each time</summary>
            public List<(string Name, string Arguments)> Script = null;
            public AIUsage LastUsage { get; private set; } = new AIUsage();

            public string HandleChat(List<ChatMessage> messages)
            {
                Calls++;
                LastUsage = new AIUsage() { InputTokens = 1000000, Calls = 1 };
                return "final answer";
            }

            public string HandleChatWithTools(List<ChatMessage> messages, IList<AITool> tools, out IList<AIToolCall> toolCalls)
            {
                Calls++;
                LastUsage = new AIUsage() { InputTokens = 1000000, Calls = 1 };
                toolCalls = null;
                if (Calls > ToolCallReplies || (Script != null && Calls > Script.Count)) return "answer";
                (string Name, string Arguments) call = Script != null ? Script[Calls - 1] : ("no_such_tool", "{\"n\":" + Calls + "}");
                toolCalls = new List<AIToolCall>() { new AIToolCall() { Id = "call" + Calls, Name = call.Name, Arguments = call.Arguments } };
                return "";
            }
        }

        static Repository _repository;
        static AIAgentConfiguration _agent;

        [ClassInitialize]
        public static void Init(TestContext context)
        {
            _repository = Repository.Instance;
            _agent = _repository.AIConfiguration.AIAgents.First(a => a.IsEnabled && a.ToolGUIDs.Count > 0 && a.GetProviderConfiguration() != null);
        }

        static void withCosts(double inputCost, Action action)
        {
            var providerConfiguration = _agent.GetProviderConfiguration();
            var input = providerConfiguration.InputTokenCost;
            var output = providerConfiguration.OutputTokenCost;
            try
            {
                providerConfiguration.InputTokenCost = inputCost;
                providerConfiguration.OutputTokenCost = 0;
                action();
            }
            finally
            {
                providerConfiguration.InputTokenCost = input;
                providerConfiguration.OutputTokenCost = output;
            }
        }

        [TestMethod]
        public void CachedInputTokensCost()
        {
            var configuration = new AIProviderConfiguration() { InputTokenCost = 2.5, OutputTokenCost = 15 };
            var usage = new AIUsage() { InputTokens = 1000000, CachedInputTokens = 800000, OutputTokens = 100000 };
            //No cached cost: every input token at the full rate
            Assert.AreEqual(4.0, configuration.GetCost(usage).Value, 1e-9);
            configuration.CachedInputTokenCost = 0.25;
            Assert.AreEqual(0.2 * 2.5 + 0.8 * 0.25 + 1.5, configuration.GetCost(usage).Value, 1e-9);
            //Cached tokens never exceed the input tokens
            usage.CachedInputTokens = 2000000;
            Assert.AreEqual(0.25 + 1.5, configuration.GetCost(usage).Value, 1e-9);
            //Accumulated per exchange
            var total = new AIUsage();
            total.Add(new AIUsage() { InputTokens = 10, CachedInputTokens = 4 });
            total.Add(new AIUsage() { InputTokens = 20, CachedInputTokens = 15 });
            Assert.AreEqual(19, total.CachedInputTokens);
        }

        class TestLog : ReportExecutionLog
        {
            public List<string> Messages = new List<string>();
            public void LogMessage(string message, params object[] args) { Messages.Add(message); }
        }

        [TestMethod]
        public void ChatLogHasUserAndMessage()
        {
            var log = new TestLog();
            var provider = new TestProvider() { ToolCallReplies = 0 };
            var agent = new AIAgent(_agent, provider) { SecurityContext = new SecurityUser(_repository.Security) { WebUserName = "someone" } };
            agent.Chat("the question", null, log);
            StringAssert.StartsWith(log.Messages[0], "[AIAgent] User: someone, Provider: ");
            Assert.AreEqual("****** User Message:\r\nthe question", log.Messages[1]);
        }

        [TestMethod]
        public void CostText()
        {
            var user = new SecurityUser(_repository.Security);
            Assert.IsNull(user.GetAICostText(null));
            Assert.AreEqual(0.0123.ToString("0.0000", _repository.CultureInfo), user.GetAICostText(0.0123));
            //Text of the Security Provider instead of the cost of the AI provider
            user.FormatAICost = cost => (cost * 1.2).ToString("0.00") + " EUR";
            Assert.AreEqual((1.2).ToString("0.00") + " EUR", user.GetAICostText(1));
            user.FormatAICost = cost => null;
            Assert.IsNull(user.GetAICostText(1));
            //An exception of the script shows no cost
            user.FormatAICost = cost => throw new Exception("failure");
            Assert.IsNull(user.GetAICostText(1));
        }

        [TestMethod]
        public void CostLimitStopsTheToolLoop()
        {
            withCosts(1, () =>
            {
                var provider = new TestProvider();
                var notified = new List<double?>();
                var user = new SecurityUser(_repository.Security) { GetAICostLimit = () => 2.5, OnAIUsage = (usage, cost) => notified.Add(cost) };
                var agent = new AIAgent(_agent, provider) { SecurityContext = user };

                var reply = agent.Chat("hello");
                Assert.AreEqual(3, provider.Calls, "the third call reaches the limit, no final call");
                Assert.IsTrue(agent.LastChatCostLimitReached);
                Assert.IsNull(agent.LastChatRefusal);
                Assert.AreEqual(agent.CostLimitReply, reply);
                Assert.AreEqual(1, notified.Count);
                Assert.AreEqual(3, notified[0]);
                //Every tool call of the conversation has a result
                var callIds = agent.Messages.OfType<ToolChatMessage>().Select(m => m.ToolCallId).ToList();
                Assert.IsTrue(callIds.Contains("call3"), "the calls of the last reply are answered");

                //Under the limit: normal answer
                provider = new TestProvider() { ToolCallReplies = 1 };
                agent = new AIAgent(_agent, provider) { SecurityContext = user };
                Assert.AreEqual("answer", agent.Chat("hello"));
                Assert.IsFalse(agent.LastChatCostLimitReached);
                Assert.AreEqual(2, notified[1]);
            });
        }

        [TestMethod]
        public void MessageRefused()
        {
            withCosts(1, () =>
            {
                var provider = new TestProvider() { ToolCallReplies = 0 };
                var notified = 0;
                var user = new SecurityUser(_repository.Security) { GetAICostLimit = () => 0, OnAIUsage = (usage, cost) => notified++ };
                var agent = new AIAgent(_agent, provider) { SecurityContext = user };
                var messages = agent.Messages.Count;

                //No budget left
                Assert.AreEqual(agent.CostLimitReply, agent.Chat("hello"));
                Assert.AreEqual(agent.CostLimitReply, agent.LastChatRefusal);
                Assert.AreEqual(0, provider.Calls);
                Assert.AreEqual(messages, agent.Messages.Count, "a refused message is not added to the conversation");
                Assert.AreEqual(0, notified);
                Assert.IsNull(agent.GenerateTitle());
                Assert.AreEqual(0, provider.Calls, "no title generated without budget");

                //Refused by the check of the provider script, the budget is not asked
                user.CheckAIChat = () => "Subscribe to the AI option.";
                user.GetAICostLimit = () => throw new Exception("not called");
                Assert.AreEqual("Subscribe to the AI option.", agent.Chat("hello"));
                Assert.AreEqual(0, provider.Calls);

                //Error of the script: refused
                user.CheckAIChat = () => throw new Exception("database down");
                Assert.IsNotNull(agent.Chat("hello"));
                Assert.AreEqual(0, provider.Calls);
                user.CheckAIChat = null;
                Assert.AreEqual(agent.CostLimitReply, agent.Chat("hello"), "an error of the budget script gives no budget");
                Assert.AreEqual(0, provider.Calls);

                //Accepted
                user.GetAICostLimit = () => null;
                Assert.AreEqual("answer", agent.Chat("hello"));
                Assert.IsNull(agent.LastChatRefusal);
                Assert.AreEqual(1, notified);
            });
        }

        static List<string> toolResults(AIAgent agent) => agent.Messages.OfType<ToolChatMessage>().Select(m => m.Content[0].Text).ToList();

        [TestMethod]
        public void RepeatedToolCallsStopTheLoop()
        {
            //The same call 5 times in a row: the 2nd and 3rd are refused, the loop ends and the final answer is asked without tools
            var provider = new TestProvider() { Script = Enumerable.Repeat(("no_such_tool", "{\"a\":1,\"b\":\"x\"}"), 5).ToList() };
            var log = new TestLog();
            var agent = new AIAgent(_agent, provider);
            Assert.AreEqual("final answer", agent.Chat("hello", null, log));
            Assert.AreEqual(4, provider.Calls, "3 replies with tool calls, then the final call");
            var results = toolResults(agent);
            Assert.AreEqual(3, results.Count);
            Assert.AreNotEqual(AIAgent.RepeatedToolCallError, results[0], "the first call is executed");
            Assert.AreEqual(AIAgent.RepeatedToolCallError, results[1]);
            Assert.AreEqual(AIAgent.RepeatedToolCallError, results[2]);
            Assert.AreEqual(2, log.Messages.Count(m => m.StartsWith("****** Repeated Tool Call")));

            //Same arguments with another formatting and order: still identical
            provider = new TestProvider() { Script = new List<(string, string)>() { ("no_such_tool", "{\"a\":1,\"b\":\"x\"}"), ("no_such_tool", "{ \"b\" : \"x\", \"a\" : 1 }") } };
            agent = new AIAgent(_agent, provider);
            Assert.AreEqual("answer", agent.Chat("hello"));
            Assert.AreEqual(AIAgent.RepeatedToolCallError, toolResults(agent)[1]);
        }

        [TestMethod]
        public void RepeatedToolCallAfterAnotherCallIsExecuted()
        {
            //Execute, fix, execute again: the same call after another call is executed
            var provider = new TestProvider()
            {
                Script = new List<(string, string)>() { ("no_such_tool", "{\"path\":\"r\"}"), ("no_such_patch", "{\"path\":\"r\"}"), ("no_such_tool", "{\"path\":\"r\"}"), ("no_such_patch", "{\"path\":\"r\"}"), ("no_such_tool", "{\"path\":\"r\"}") }
            };
            var agent = new AIAgent(_agent, provider);
            Assert.AreEqual("answer", agent.Chat("hello"));
            Assert.AreEqual(6, provider.Calls);
            Assert.IsFalse(toolResults(agent).Contains(AIAgent.RepeatedToolCallError));

            //Different arguments: executed
            provider = new TestProvider() { Script = new List<(string, string)>() { ("no_such_tool", "{\"a\":1}"), ("no_such_tool", "{\"a\":2}") } };
            agent = new AIAgent(_agent, provider);
            Assert.AreEqual("answer", agent.Chat("hello"));
            Assert.IsFalse(toolResults(agent).Contains(AIAgent.RepeatedToolCallError));
        }

        [TestMethod]
        public void NormalizeToolArguments()
        {
            Assert.AreEqual(AIAgent.NormalizeToolArguments("{\"b\":[{\"y\":1,\"x\":2}],\"a\":null}"), AIAgent.NormalizeToolArguments("{ \"a\": null, \"b\": [ { \"x\": 2, \"y\": 1 } ] }"));
            Assert.AreNotEqual(AIAgent.NormalizeToolArguments("{\"a\":[1,2]}"), AIAgent.NormalizeToolArguments("{\"a\":[2,1]}"), "the order of an array matters");
            Assert.AreEqual("", AIAgent.NormalizeToolArguments(null));
            Assert.AreEqual("not json", AIAgent.NormalizeToolArguments(" not json "));
        }

        [TestMethod]
        public void BudgetWithoutProviderCosts()
        {
            withCosts(0, () =>
            {
                var provider = new TestProvider() { ToolCallReplies = 0 };
                double? notifiedCost = -1;
                var user = new SecurityUser(_repository.Security) { GetAICostLimit = () => 10, OnAIUsage = (usage, cost) => notifiedCost = cost };
                var agent = new AIAgent(_agent, provider) { SecurityContext = user };
                Assert.AreEqual(AIAgent.NoCostConfiguredRefusal, agent.Chat("hello"));
                Assert.AreEqual(0, provider.Calls);

                //No budget: the usage is notified without cost
                user.GetAICostLimit = null;
                Assert.AreEqual("answer", agent.Chat("hello"));
                Assert.IsNull(notifiedCost);
            });
        }
    }
}
