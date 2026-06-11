//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Seal Report Dual-License version 1.0; you may not use this file except in compliance with the License described at https://github.com/ariacom/Seal-Report.
//
namespace Seal.AI
{
    /// <summary>
    /// Provides a simple cancellation flag that can be checked during long-running
    /// operations such as an AI agent chat loop.
    /// </summary>
    public interface ICancelOperation
    {
        /// <summary>
        /// When <c>true</c>, the current operation should be aborted as soon as possible.
        /// </summary>
        bool Cancel { get; }
    }
}
