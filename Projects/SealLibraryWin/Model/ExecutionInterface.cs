//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//

using System;
using System.Text;

namespace Seal.Model
{
    /// <summary>
    /// Log interface used during the report execution
    /// </summary>
    public interface ExecutionLogInterface
    {
        /// <summary>
        /// True if the Job is cancelled
        /// </summary>
        bool IsJobCancelled();

        /// <summary>
        /// Log the text with optional parameters
        /// </summary>
        void Log(string text, params object[] args);

        /// <summary>
        /// Log the text with optional parameters with no Carriage Return/Line Feed
        /// </summary>
        void LogNoCR(string text, params object[] args);

        /// <summary>
        /// Log the text with optional parameters without any Timestamp
        /// </summary>
        void LogRaw(string text, params object[] args);
    }

    /// <summary>
    /// Dummy implementation of the Log interface
    /// </summary>
    public class DummyLogInterface : ExecutionLogInterface
    {
        /// <summary>
        /// Always false
        /// </summary>
        public bool IsJobCancelled() { return false; }
        /// <summary>
        /// Does nothing
        /// </summary>
        public void Log(string text, params object[] args) { }
        /// <summary>
        /// Does nothing
        /// </summary>
        public void LogNoCR(string text, params object[] args) { }
        /// <summary>
        /// Does nothing
        /// </summary>
        public void LogRaw(string text, params object[] args) { }
    }

    /// <summary>
    /// Console implementation of the Log interface
    /// </summary>
    public class ConsoleLog : ExecutionLogInterface
    {
        /// <summary>
        /// Always false
        /// </summary>
        public bool IsJobCancelled() { return false; }
        /// <summary>
        /// Log the text to the console
        /// </summary>
        public void Log(string text, params object[] args) {
            Console.WriteLine(text, args);
        }
        /// <summary>
        /// Log the text to the console
        /// </summary>
        public void LogNoCR(string text, params object[] args) {
            Console.WriteLine(text, args);
        }
        /// <summary>
        /// Log the text to the console
        /// </summary>
        public void LogRaw(string text, params object[] args) {
            Console.WriteLine(text, args);
        }
    }

    /// <summary>
    /// String implementation of the Log interface
    /// </summary>
    public class StringLog : ExecutionLogInterface
    {
        /// <summary>
        /// String builder containing the log result
        /// </summary>
        public StringBuilder Result = new StringBuilder("");
        /// <summary>
        /// Always false
        /// </summary>
        public bool IsJobCancelled() { return false; }
        /// <summary>
        /// Append the text to the log result
        /// </summary>
        public void Log(string text, params object[] args)
        {
            Result.AppendFormat(text, args);
        }
        /// <summary>
        /// Append the text to the log result
        /// </summary>
        public void LogNoCR(string text, params object[] args)
        {
            Result.AppendFormat(text, args);
        }
        /// <summary>
        /// Append the text to the log result
        /// </summary>
        public void LogRaw(string text, params object[] args)
        {
            Result.AppendFormat(text, args);
        }
    }
}
