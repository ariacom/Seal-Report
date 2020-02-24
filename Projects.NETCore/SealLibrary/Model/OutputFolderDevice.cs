//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// OutputFolderDevice is an implementation of device that save the report result to a file. 
    /// </summary>
    public class OutputFolderDevice : OutputDevice
    {
        /// <summary>
        /// Default device identifier
        /// </summary>
        public static string DefaultGUID = "c428a6ba-061b-4a47-b9bc-f3f02442ab4b";

        /// <summary>
        /// Create a basic OutputFolderDevice
        /// </summary>
        static public OutputFolderDevice Create()
        {
            OutputFolderDevice result = new OutputFolderDevice() { GUID = DefaultGUID };
            result.Name = "Folder Device";
            return result;
        }

        /// <summary>
        /// Full name
        /// </summary>
        [XmlIgnore]
        public override string FullName
        {
            get { return "Folder Device"; }
        }

        /// <summary>
        /// Check that the report result has been saved and set information
        /// </summary>
        public override string Process(Report report)
        {
            ReportOutput output = report.OutputToExecute;
            if (string.IsNullOrEmpty(output.FolderPath)) throw new Exception("The output folder path is not specified in the report output.");
            if (string.IsNullOrEmpty(output.FileName)) throw new Exception("The file name is not specified in the report output.");
            output.Information = report.Translate("Report result generated in '{0}'", report.DisplayResultFilePath);
            return string.Format("Report result generated in '{0}'", report.DisplayResultFilePath);
        }

        /// <summary>
        /// Dummy function
        /// </summary>
        public override void SaveToFile()
        {
            throw new Exception("No need so far...");
        }

        /// <summary>
        /// Dummy function
        /// </summary>
        public override void SaveToFile(string path)
        {
            throw new Exception("No need so far...");
        }

        /// <summary>
        /// Dummy function
        /// </summary>
        public override void Validate()
        {
        }
    }
}

