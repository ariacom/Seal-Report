//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.IO;
using Seal.Model;

namespace Seal.Helpers
{
    public class FileHelper
    {
        public static string TempApplicationDirectory
        {
            get
            {
                string result = Path.Combine(Path.GetTempPath(), "_Seal_Report.Temp");
                if (!Directory.Exists(result))
                {
                    try
                    {
                        Directory.CreateDirectory(result);
                    }
                    catch { }
                }
                return result;
            }
        }

        public static void PurgeTempApplicationDirectory()
        {
            PurgeTempDirectory(TempApplicationDirectory);
            //Purge razor directories
            foreach (var dir in Directory.GetDirectories(Path.GetTempPath(), "RazorEngine_*"))
            {
                try
                {
                    //purge razor dir older than 120 min
                    if (Directory.GetLastWriteTime(dir).AddMinutes(120) < DateTime.Now)
                    Directory.Delete(dir, true);
                }
                catch { }
            }
        }

        public static void PurgeTempDirectory(string directoryPath)
        {
            try
            {
                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    //purge files older than 8 hours...
                    if (File.GetLastWriteTime(file).AddHours(8) < DateTime.Now)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch { };
                    }
                }
            }
            catch { };
        }

        public static string CleanFilePath(string filePath)
        {
            string result = filePath;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(c.ToString(), "");
            }
            foreach (char c in Path.GetInvalidPathChars())
            {
                result = result.Replace(c.ToString(), "");
            }
            return result;
        }

        public static string GetUniqueFileName(string filePath, string newExtension = "")
        {
            return GetUniqueFileName(Path.GetDirectoryName(filePath), Path.GetFileName(filePath), newExtension);
        }

        public static string GetTempUniqueFileName(string filePath, string newExtension = "")
        {
            return GetUniqueFileName(TempApplicationDirectory, Path.GetFileName(filePath), newExtension);
        }

        public static string GetUniqueFileName(string directory, string fileName, string newExtension = "")
        {
            int cnt = 0;
            string result = "";
            while (true)
            {
                result = Path.Combine(directory, Path.GetFileNameWithoutExtension(fileName));
                if (cnt > 0) result += cnt.ToString();
                result += (newExtension == "" || newExtension == "." ? Path.GetExtension(fileName) : newExtension);
                if (!File.Exists(result)) break;
                cnt += 1;
            }
            return result;
        }

        public static void WriteFile(string path, string data)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            StreamWriter sw = new StreamWriter(path);
            sw.Write(data.ToString());
            sw.Close();
        }

        public static string ReadFile(string path, string data)
        {
            StreamReader sr = new StreamReader(path);
            string result = sr.ReadToEnd();
            sr.Close();
            return result;
        }

        public static void CopyDirectory(string source, string destination, bool recursive)
        {
            if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }

            if (recursive)
            {
                foreach (string directory in Directory.GetDirectories(source))
                {
                    CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)), true);
                }
            }
        }


        public static string CreateAndGetDirectory(string root, string subFolder)
        {
            string result = root;
            if (!Directory.Exists(result)) Directory.CreateDirectory(result);
            if (!string.IsNullOrWhiteSpace(subFolder))
            {
                result = Path.Combine(root, subFolder);
                if (!Directory.Exists(result)) Directory.CreateDirectory(result);
            }
            return result;
        }

        public static void AddFolderChoices(string path, string prefix, List<string> choices)
        {
            foreach (var folder in Directory.GetDirectories(path))
            {
                string newFolder = folder.StartsWith(Repository.Instance.ReportsFolder) ? folder.Substring(Repository.Instance.ReportsFolder.Length) : folder;
                if (string.IsNullOrEmpty(newFolder)) newFolder = "\\";
                choices.Add(prefix + newFolder);
                AddFolderChoices(folder, prefix, choices);
            }
        }

        #region Seal Attachments
        public static string GetResultFilePrefix(string path)
        {
            return Path.GetFileNameWithoutExtension(path) + "_" + Path.GetExtension(path).Replace(".", "");
        }

        public static bool IsSealReportFile(string path)
        {
            return path.EndsWith("." + Repository.SealReportFileExtension);
        }

        public static bool ReportHasSchedule(string path)
        {
            string content = File.ReadAllText(path);
            return content.Contains("<ReportSchedule>") && content.Contains("</ReportSchedule>");
        }

        public static void DeleteSealFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void MoveSealFile(string source, string destination, bool copy)
        {
            if (source != destination)
            {
                if (copy) File.Copy(source, destination, true);
                else File.Move(source, destination);
            }
        }

        #endregion
    }
}
