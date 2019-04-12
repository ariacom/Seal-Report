//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//

namespace Seal.Forms
{
    public interface ExecutionLogInterface
    {
        bool IsJobCancelled();
        void Log(string text, params object[] args);
        void LogNoCR(string text, params object[] args);
        void LogRaw(string text, params object[] args);
    }
    public class DummyLogInterface : ExecutionLogInterface
    {
        public bool IsJobCancelled() { return false; }
        public void Log(string text, params object[] args) { }
        public void LogNoCR(string text, params object[] args) { }
        public void LogRaw(string text, params object[] args) { }
    }
}
