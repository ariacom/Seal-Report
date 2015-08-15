//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// This code is licensed under GNU General Public License version 3, http://www.gnu.org/licenses/gpl-3.0.en.html.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class OutputFolderDevice : OutputDevice
    {
        static public OutputFolderDevice Create()
        {
            OutputFolderDevice result = new OutputFolderDevice() { GUID = Guid.NewGuid().ToString() };
            result.Name = "Folder Device";
            return result;
        }

        [XmlIgnore]
        public override string FullName
        {
            get { return "Folder Device"; }
        }


        public override string Process(Report report)
        {
            ReportOutput output = report.OutputToExecute;
            if (string.IsNullOrEmpty(output.FolderPath)) throw new Exception("The output folder path is not specified in the report output.");
            if (string.IsNullOrEmpty(output.FileName)) throw new Exception("The file name is not specified in the report output.");
            output.Information = report.Translate("Report result generated in '{0}'", report.DisplayResultFilePath);
            return string.Format("Report result generated in '{0}'", report.DisplayResultFilePath);
        }


        public override void SaveToFile()
        {
            throw new Exception("No need so far...");
        }

        public override void SaveToFile(string path)
        {
            throw new Exception("No need so far...");
        }


        public override void Validate()
        {
        }

    }
}
