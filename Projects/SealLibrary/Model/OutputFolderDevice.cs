//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Xml.Serialization;

namespace Seal.Model
{
    public class OutputFolderDevice : OutputDevice
    {
        public static string DefaultGUID = "c428a6ba-061b-4a47-b9bc-f3f02442ab4b";

        static public OutputFolderDevice Create()
        {
            OutputFolderDevice result = new OutputFolderDevice() { GUID = DefaultGUID };
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
