namespace Seal.AI
{
    /// <summary>
    /// Token and call usage of AI model invocations.
    /// Providers publish the usage of their last API call (<see cref="AIProvider.LastUsage"/>, one call);
    /// <see cref="AIAgent"/> accumulates it per chat exchange (<see cref="AIAgent.LastChatUsage"/>)
    /// and for the whole conversation (<see cref="AIAgent.TotalUsage"/>).
    /// </summary>
    public class AIUsage
    {
        /// <summary>
        /// Input (prompt) tokens consumed, including cached tokens when the API reports them separately.
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// Output (completion) tokens generated.
        /// </summary>
        public int OutputTokens { get; set; }

        /// <summary>
        /// Total tokens (input + output).
        /// </summary>
        public int TotalTokens => InputTokens + OutputTokens;

        /// <summary>
        /// Number of AI model API calls (LLM round-trips).
        /// </summary>
        public int Calls { get; set; }

        /// <summary>
        /// Number of tool calls executed (including skill loads). Only maintained at the agent level.
        /// </summary>
        public int ToolCalls { get; set; }

        /// <summary>
        /// Accumulates another usage into this one.
        /// </summary>
        public void Add(AIUsage other)
        {
            if (other == null) return;
            InputTokens += other.InputTokens;
            OutputTokens += other.OutputTokens;
            Calls += other.Calls;
            ToolCalls += other.ToolCalls;
        }
    }
}
