//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Seal.Model
{
    /// <summary>
    /// A ReportShortcut is a lightweight file (.srln) that references a real report (.srex) or a file located elsewhere in the repository.
    /// In the Web Report Server it is resolved to its target and behaves like the target (execute, view, edit, download...).
    /// </summary>
    public class ReportShortcut
    {
        /// <summary>
        /// Identifier of the shortcut
        /// </summary>
        [DisplayName("GUID"), Description("Identifier of the shortcut."), Category("Definition")]
        public string GUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Repository relative path of the target report or file (e.g. /Sales/Orders.srex)
        /// </summary>
        [DisplayName("Target path"), Description("Repository relative path of the target report or file."), Category("Definition")]
        public string TargetPath { get; set; }

        /// <summary>
        /// Optional display name override. If empty, the target file name is used.
        /// </summary>
        [DisplayName("Name"), Description("Optional display name override. If empty, the target file name is used."), Category("Definition")]
        public string Name { get; set; }

        /// <summary>
        /// Login of the user who created the shortcut
        /// </summary>
        [DisplayName("Created by"), Description("Login of the user who created the shortcut."), Category("Definition")]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Current file path of the shortcut (not serialized)
        /// </summary>
        [XmlIgnore]
        public string FilePath { get; set; }

        /// <summary>
        /// Load a shortcut from a file
        /// </summary>
        public static ReportShortcut LoadFromFile(string path)
        {
            ReportShortcut result;
            try
            {
                path = FileHelper.ConvertOSFilePath(path);
                if (!File.Exists(path)) throw new Exception("File not found: " + path);

                XmlSerializer serializer = new XmlSerializer(typeof(ReportShortcut));
                using (XmlReader xr = XmlReader.Create(path))
                {
                    result = (ReportShortcut)serializer.Deserialize(xr);
                    xr.Close();
                }
                result.FilePath = path;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to read the shortcut file '{0}'.\r\n{1}", path, ex.Message));
            }
            return result;
        }

        /// <summary>
        /// Save the shortcut to its file
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(FilePath);
        }

        /// <summary>
        /// Save the shortcut to a file
        /// </summary>
        public void SaveToFile(string path)
        {
            Helper.Serialize(path, this);
            FilePath = path;
        }
    }
}
