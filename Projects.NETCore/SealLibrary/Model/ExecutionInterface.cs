//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
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
        public bool IsJobCancelled() { return false; }
        public void Log(string text, params object[] args) { }
        public void LogNoCR(string text, params object[] args) { }
        public void LogRaw(string text, params object[] args) { }
    }

    /// <summary>
    /// Console implementation of the Log interface
    /// </summary>
    public class ConsoleLog : ExecutionLogInterface
    {
        public bool IsJobCancelled() { return false; }
        public void Log(string text, params object[] args) {
            Console.WriteLine(text, args);
        }
        public void LogNoCR(string text, params object[] args) {
            Console.WriteLine(text, args);
        }
        public void LogRaw(string text, params object[] args) {
            Console.WriteLine(text, args);
        }
    }

    /// <summary>
    /// String implementation of the Log interface
    /// </summary>
    public class StringLog : ExecutionLogInterface
    {
        public StringBuilder Result = new StringBuilder("");
        public bool IsJobCancelled() { return false; }
        public void Log(string text, params object[] args)
        {
            Result.AppendFormat(text, args);
        }
        public void LogNoCR(string text, params object[] args)
        {
            Result.AppendFormat(text, args);
        }
        public void LogRaw(string text, params object[] args)
        {
            Result.AppendFormat(text, args);
        }
    }
}

