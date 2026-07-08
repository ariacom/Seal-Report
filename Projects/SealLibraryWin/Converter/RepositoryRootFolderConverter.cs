//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Seal.Model;

namespace Seal.Forms
{
    /// <summary>
    /// Lists the folders available under the repository root for a SecurityRepositoryFolder.Path.
    /// The 'Reports' folder and its sub-folders are excluded on purpose: they are managed by the
    /// 'Report Folders' configuration (conflict handling, Danger Zone). The list is exclusive, so the
    /// editor cannot configure a path inside the Reports tree.
    /// </summary>
    public class RepositoryRootFolderConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true; //true means show a combobox
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true; //limit to the list: a path inside the Reports tree cannot be entered
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var choices = new List<string>();
            choices.Add(Path.DirectorySeparatorChar.ToString());
            addFolderChoices(Repository.Instance.RepositoryPath, choices);
            return new StandardValuesCollection(choices);
        }

        //Large or system folders that are listed but never expanded (they hold thousands of files or are internal)
        static readonly string[] NoRecursePaths = new string[]
        {
            Path.DirectorySeparatorChar + "Assemblies" + Path.DirectorySeparatorChar + "Chrome",
            Path.DirectorySeparatorChar + "Assemblies" + Path.DirectorySeparatorChar + "ChromeHeadlessShell",
        };

        static void addFolderChoices(string path, List<string> choices)
        {
            var repositoryPath = Repository.Instance.RepositoryPath;
            foreach (var folder in Directory.GetDirectories(path))
            {
                string relative = folder.StartsWith(repositoryPath) ? folder.Substring(repositoryPath.Length) : folder;
                string normalized = SecurityRepositoryFolder.Normalize(relative);
                //Exclude the Reports tree: it is managed by the Report Folders configuration
                if (SecurityRepositoryFolder.IsUnderReports(normalized)) continue;
                choices.Add(relative);
                //Do not recurse into large or internal folders (Chrome assemblies, hidden _Agents folders)
                if (Path.GetFileName(folder) == AgentFolders.FolderName) continue;
                if (System.Array.Exists(NoRecursePaths, p => string.Equals(normalized, p, System.StringComparison.OrdinalIgnoreCase))) continue;
                addFolderChoices(folder, choices);
            }
        }
    }
}
