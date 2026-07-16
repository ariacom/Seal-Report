//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FluentFTP;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Seal.Model;

namespace Seal.Helpers
{
    /// <summary>
    /// Static helper methods to manipulate files, directories and ZIP archives, available in Razor scripts
    /// </summary>
    public class FileHelper
    {
        /// <summary>
        /// If set, overrides the temporary directory returned by TempPath
        /// </summary>
        public static string AlternateTemporaryDirectory = null;
        /// <summary>
        /// Path of the temporary directory (AlternateTemporaryDirectory if set, otherwise the local user temp directory)
        /// </summary>
        public static string TempPath
        {
            get
            {
                //Try local user temp first
                if (!string.IsNullOrEmpty(AlternateTemporaryDirectory)) return AlternateTemporaryDirectory;
                var result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Local\Temp");
                if (!Directory.Exists(result)) result = Path.GetTempPath();
                return result;
            }
        }


        /// <summary>
        /// Path of the '_Seal_Report.Temp' application temporary directory (created if necessary)
        /// </summary>
        public static string TempApplicationDirectory
        {
            get
            {
                string result = Path.Combine(TempPath, "_Seal_Report.Temp");
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

        /// <summary>
        /// Purge the application temporary directory (files older than 8 hours) and RazorEngine directories older than 120 minutes
        /// </summary>
        public static void PurgeTempApplicationDirectory()
        {
            PurgeTempDirectory(TempApplicationDirectory);
            //Purge razor directories
            foreach (var dir in Directory.GetDirectories(TempPath, "RazorEngine_*"))
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

        /// <summary>
        /// Delete the files older than 8 hours in a directory
        /// </summary>
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

        /// <summary>
        /// Replace invalid file name and path characters in a file path by a given string
        /// </summary>
        public static string CleanFilePath(string filePath, string replace = "")
        {
            string result = filePath;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(c.ToString(), replace);
            }
            foreach (char c in Path.GetInvalidPathChars())
            {
                result = result.Replace(c.ToString(), replace);
            }
            return result;
        }

        /// <summary>
        /// Returns true if the file name contains invalid characters
        /// </summary>
        public static bool HasInvalidFileNameChars(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return fileName.IndexOfAny(invalidChars) >= 0;
        }

        /// <summary>
        /// Returns a unique file name in the directory of the given path, appending a number if the file exists. If lockFile is true, an empty file is created.
        /// </summary>
        public static string GetUniqueFileName(string filePath, string newExtension = "", bool lockFile = false)
        {
            return GetUniqueFileName(Path.GetDirectoryName(filePath), Path.GetFileName(filePath), newExtension, lockFile);
        }

        /// <summary>
        /// Returns a unique file name in the application temporary directory for the given file name
        /// </summary>
        public static string GetTempUniqueFileName(string filePath, string newExtension = "", bool lockFile = false)
        {
            return GetUniqueFileName(TempApplicationDirectory, Path.GetFileName(filePath), newExtension, lockFile);
        }

        /// <summary>
        /// Returns a unique file name in a directory, appending a number if the file exists and optionally changing the extension. If lockFile is true, an empty file is created.
        /// </summary>
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

        /// <summary>
        /// Write a string to a file, creating the directory if necessary
        /// </summary>
        public static void WriteFile(string path, string data)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            StreamWriter sw = new StreamWriter(path);
            sw.Write(data.ToString());
            sw.Close();
        }

        /// <summary>
        /// Returns the content of a file as a string
        /// </summary>
        public static string ReadFile(string path, string data)
        {
            StreamReader sr = new StreamReader(path);
            string result = sr.ReadToEnd();
            sr.Close();
            return result;
        }

        /// <summary>
        /// Copy the files of a directory to a destination, optionally recursive, with a search pattern and a pattern of file name starts to skip
        /// </summary>
        public static void CopyDirectory(string source, string destination, bool recursive, ReportExecutionLog log = null, string searchPattern = "*", string skipStartsPattern = "")
        {
            if (log != null) log.LogMessage("Copying directory '{0}' to '{1}'", source, destination);

            if (!Directory.Exists(destination)) Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, searchPattern))
            {
                try
                {
                    if (!string.IsNullOrEmpty(skipStartsPattern) && Path.GetFileName(file).ToLower().StartsWith(skipStartsPattern.ToLower())) continue;

                    var destinationFile = Path.Combine(destination, Path.GetFileName(file));
                    if (log != null) log.LogMessage("Copy '{0}' to '{1}'", file, destinationFile);
                    File.Copy(file, destinationFile, true);
                }
                catch (Exception ex)
                {
                    if (log != null) log.LogMessage(ex.Message);
                    else throw;
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

            if (!Directory.EnumerateFileSystemEntries(destination).Any()) Directory.Delete(destination);
        }

        /// <summary>
        /// Upload the files of a directory to an FTP destination, optionally recursive
        /// </summary>
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
                    client.CreateDirectory(dest, true);
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
                    else throw;
                }
            }
        }

        /// <summary>
        /// Create the root directory and an optional sub-folder if they do not exist, and return the resulting path
        /// </summary>
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

        /// <summary>
        /// Add recursively the sub-folder paths of a directory to a list of choices (paths relative to the repository Reports folder, with a prefix)
        /// </summary>
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

        /// <summary>
        /// Add recursively the sub-folder paths of a directory to a list of choices (paths relative to the repository Personal folder)
        /// </summary>
        public static void AddPersonalFolderChoices(string path, List<string> choices)
        {
            foreach (var folder in Directory.GetDirectories(path))
            {
                string newFolder = folder.StartsWith(Repository.Instance.PersonalFolder) ? folder.Substring(Repository.Instance.PersonalFolder.Length) : folder;
                choices.Add(newFolder);
                AddPersonalFolderChoices(folder, choices);
            }
        }

        /// <summary>
        /// Convert a file path to use the directory separator of the current operating system
        /// </summary>
        public static string ConvertOSFilePath(string filePath)
        {
            return string.IsNullOrEmpty(filePath) ? "" : filePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Create a ZIP archive from the files of a folder matching a filter, with an optional password
        /// </summary>
        public static void CreateZIPFromFolder(string inputFolder, string filter, bool recursive, string zipPath, string password)
        {
            var dic = new Dictionary<string, string>();
            FillZipDictionaryFiles(dic, inputFolder, "", filter, recursive);
            CreateZIP(dic, zipPath, password);
        }

        /// <summary>
        /// Create a ZIP archive containing a single file, with an optional password
        /// </summary>
        public static void CreateZIPFromFile(string inputPath, string zipPath, string password)
        {
            CreateZIP(inputPath, Path.GetFileName(inputPath), zipPath, password);
        }

        /// <summary>
        /// Create a ZIP archive containing a single file with a given entry name, with an optional password
        /// </summary>
        public static void CreateZIP(string inputPath, string entryName, string zipPath, string password)
        {
            var dic = new Dictionary<string, string>();
            dic.Add(inputPath, entryName);
            CreateZIP(dic, zipPath, password);
        }

        /// <summary>
        /// Create a ZIP archive from a dictionary of file paths and their entry names, with an optional password
        /// </summary>
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

        /// <summary>
        /// Extract all files of a ZIP archive to a folder, with an optional password
        /// </summary>
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

        /// <summary>
        /// Fill a dictionary of files for CreateZIP
        /// </summary>
        public static void FillZipDictionaryFiles(Dictionary<string, string> files, string dir, string targetDir, string filter, bool recursive)
        {
            foreach (var f in Directory.GetFiles(dir, filter))
            {
                files.Add(f, targetDir + Path.GetFileName(f));
            }

            if (recursive)
            {
                foreach (var d in Directory.GetDirectories(dir))
                {
                    FillZipDictionaryFiles(files, d, targetDir + Path.GetFileName(d) + "\\", filter, recursive);
                }
            }
        }


        #region Seal Attachments
        /// <summary>
        /// Returns the prefix used for report result file names (file name plus extension without the dot)
        /// </summary>
        public static string GetResultFilePrefix(string path)
        {
            return Path.GetFileNameWithoutExtension(path) + "_" + Path.GetExtension(path).Replace(".", "");
        }

        /// <summary>
        /// Returns true if the file path has the Seal report file extension
        /// </summary>
        public static bool IsReportFile(string path)
        {
            return path.EndsWith("." + Repository.SealReportFileExtension);
        }

        /// <summary>
        /// Returns true if the file path has the Seal report shortcut file extension
        /// </summary>
        public static bool IsShortcutFile(string path)
        {
            return path.EndsWith("." + Repository.SealReportShortcutFileExtension);
        }

        /// <summary>
        /// Returns true if the report file contains a schedule definition
        /// </summary>
        public static bool ReportHasSchedule(string path)
        {
            string content = File.ReadAllText(path);
            return content.Contains("<ReportSchedule>") && content.Contains("</ReportSchedule>");
        }

        /// <summary>
        /// Replace a GUID by a new one in a report file
        /// </summary>
        public static void ChangeGUID(string path, string oldGUID, string newGUID)
        {
            string content = File.ReadAllText(path);
            content = content.Replace(oldGUID, newGUID);
            File.WriteAllText(path, content, new UTF8Encoding(false)); //No bom encoding
        }

        /// <summary>
        /// Delete a file if it exists
        /// </summary>
        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Move (or copy if the copy flag is true) a file to a destination
        /// </summary>
        public static void MoveFile(string source, string destination, bool copy)
        {
            if (source != destination)
            {
                if (copy) File.Copy(source, destination, true);
                else File.Move(source, destination);
            }
        }

        /// <summary>
        /// Returns the total size in bytes of a directory including its sub-directories
        /// </summary>
        public static long GetDirectorySize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += GetDirectorySize(di);
            }
            return size;
        }


        #endregion
    }
}
