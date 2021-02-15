//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentFTP;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
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
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
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
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
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
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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

        public static string GetUniqueFileName(string filePath, string newExtension = "", bool lockFile = false)
        {
            return GetUniqueFileName(Path.GetDirectoryName(filePath), Path.GetFileName(filePath), newExtension, lockFile);
        }

        public static string GetTempUniqueFileName(string filePath, string newExtension = "", bool lockFile = false)
        {
            return GetUniqueFileName(TempApplicationDirectory, Path.GetFileName(filePath), newExtension, lockFile);
        }

        public static string GetUniqueFileName(string directory, string fileName, string newExtension = "", bool lockFile = false)
        {
            int cnt = 0;
            string result;
            while (true)
            {
                result = Path.Combine(directory, Path.GetFileNameWithoutExtension(fileName));
                if (cnt > 0) result += cnt.ToString();
                result += (newExtension == "" || newExtension == "." ? Path.GetExtension(fileName) : newExtension);
                if (!File.Exists(result)) break;
                cnt += 1;
            }

            if (lockFile)
            {
                try
                {
                    File.WriteAllText(result, "");
                }
                catch { }
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

        public static void CopyDirectory(string source, string destination, bool recursive, ReportExecutionLog log = null, string searchPattern = "*")
        {
            if (log != null) log.LogMessage("Copying directory '{0}' to '{1}'", source, destination);

            if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, searchPattern))
            {
                try
                {
                    var destinationFile = Path.Combine(destination, Path.GetFileName(file));
                    if (log != null) log.LogMessage("Copy '{0}' to '{1}'", file, destinationFile);
                    File.Copy(file, destinationFile, true);
                }
                catch (Exception ex)
                {
                    if (log != null) log.LogMessage(ex.Message);
                    Debug.WriteLine(ex.Message);
                }
            }

            if (recursive)
            {
                foreach (string directory in Directory.GetDirectories(source))
                {
                    CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)), recursive, log, searchPattern);
                }
            }
        }

        public static void FtpCopyDirectory(FtpClient client, string source, string destination, bool recursive, ReportExecutionLog log = null)
        {
            if (log != null) log.LogMessage("Copying directory '{0}' to '{1}'", source, destination);

            if (!destination.EndsWith("/")) destination += "/";

            if (recursive)
            {
                foreach (var folder in Directory.GetDirectories(source))
                {
                    var dest = destination + Path.GetFileName(folder);
                    if (log != null) log.LogMessage("Copying directory '{0}' to '{1}'", folder, dest);
                    var results = client.UploadDirectory(folder, dest, FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite);

                    if (log != null)
                    {
                        foreach (var result in results.Where(i => i.IsSuccess))
                        {
                            log.LogMessage("'{0}'", result.RemotePath);
                        }
                        foreach (var result in results.Where(i => i.Exception != null))
                        {
                            log.LogMessage("'{0}' Error: {1}", result.RemotePath, result.Exception.Message + (result.Exception.InnerException != null ? " " + result.Exception.InnerException.Message : ""));
                        }
                    }
                }
            }
            foreach (var file in Directory.GetFiles(source))
            {
                var dest = destination + Path.GetFileName(file);
                if (log != null) log.LogMessage("Copying file '{0}' to '{1}'", file, dest);
                try
                {
                    client.UploadFile(file, dest);
                }
                catch (Exception ex)
                {
                    if (log != null) log.LogMessage("'{0}' Error: '{1}'", file, ex.Message + (ex.InnerException != null ? " " + ex.InnerException.Message : ""));
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
                if (string.IsNullOrEmpty(newFolder)) newFolder = Path.DirectorySeparatorChar.ToString();
                choices.Add(prefix + newFolder);
                AddFolderChoices(folder, prefix, choices);
            }
        }

        public static void AddPersonalFolderChoices(string path, List<string> choices)
        {
            foreach (var folder in Directory.GetDirectories(path))
            {
                string newFolder = folder.StartsWith(Repository.Instance.PersonalFolder) ? folder.Substring(Repository.Instance.PersonalFolder.Length) : folder;
                choices.Add(newFolder);
                AddPersonalFolderChoices(folder, choices);
            }
        }

        public static string ConvertOSFilePath(string filePath)
        {
            return filePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        public static void CreateZIP(string inputPath, string entryName, string zipPath, string password)
        {
            var dic = new Dictionary<string, string>();
            dic.Add(inputPath, entryName);
            CreateZIP(dic, zipPath, password);
        }

        public static void CreateZIP(Dictionary<string, string> inputTargetPaths,  string zipPath, string password)
        {
            using (FileStream fsOut = File.Create(zipPath))
            using (var zipStream = new ZipOutputStream(fsOut))
            {
                //Code got from https://github.com/icsharpcode/SharpZipLib/wiki/Create-a-Zip-with-full-control-over-content
                //0-9, 9 being the highest level of compression
                zipStream.SetLevel(3);

                zipStream.Password = password;

                foreach (var inputPath in inputTargetPaths.Keys)
                {
                    var fi = new FileInfo(inputPath);
                    var newEntry = new ZipEntry(inputTargetPaths[inputPath]);

                    newEntry.DateTime = fi.LastWriteTime;

                    newEntry.Size = fi.Length;
                    zipStream.PutNextEntry(newEntry);

                    // Zip the file in buffered chunks
                    var buffer = new byte[4096];
                    using (FileStream fsInput = File.OpenRead(inputPath))
                    {
                        StreamUtils.Copy(fsInput, zipStream, buffer);
                    }
                    zipStream.Flush();
                    zipStream.CloseEntry();
                }
                zipStream.Dispose();
            }
        }

        public static void ExtractZipFile(string archivePath, string password, string outFolder)
        {
            //code got from https://github.com/icsharpcode/SharpZipLib/wiki/Unpack-a-Zip-with-full-control-over-the-operation
            using (Stream fsInput = File.OpenRead(archivePath))
            using (ZipFile zf = new ZipFile(fsInput))
            {

                if (!string.IsNullOrEmpty(password))
                {
                    // AES encrypted entries are handled automatically
                    zf.Password = password;
                }

                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        // Ignore directories
                        continue;
                    }
                    string entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:
                    //entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here
                    // to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    // Manipulate the output filename here as desired.
                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    // 4K is optimum
                    var buffer = new byte[4096];

                    // Unzip file in buffered chunks. This is just as fast as unpacking
                    // to a buffer the full size of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (var zipStream = zf.GetInputStream(zipEntry))
                    using (Stream fsOutput = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, fsOutput, buffer);
                    }
                }
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

