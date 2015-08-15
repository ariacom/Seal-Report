//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Seal.Forms
{
    public interface ExecutionLogInterface
    {
        bool IsJobCancelled();
        void Log(string text, params object[] args);
        void LogNoCR(string text, params object[] args);
        void LogRaw(string text, params object[] args);
    }
}
