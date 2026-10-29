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
                if (Calls > ToolCallReplies) return "answer";
                toolCalls = new List<AIToolCall>() { new AIToolCall() { Id = "call" + Calls, Name = "no_such_tool", Arguments = "{}" } };
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
