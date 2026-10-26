//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.Helpers;
using Seal.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Seal.AI
{
    /// <summary>
    /// Read-only knowledge of Seal for the AI tools: reading and searching the files of the knowledge folders of the repository
    /// (view templates, sample reports, skills, see <see cref="AIServerConfiguration.KnowledgeFolders"/>) and description of the classes serialized in a report.
    /// </summary>
    public static class AIKnowledge
    {
        /// <summary>
        /// Maximum number of characters returned by a read
        /// </summary>
        public const int MaxReadLength = 40000;

        /// <summary>
        /// Default number of lines returned by a read
        /// </summary>
        public const int DefaultReadLines = 500;

        /// <summary>
        /// Maximum number of lines returned by a search
        /// </summary>
        public const int MaxSearchResults = 100;

        /// <summary>
        /// Files larger than this size (bytes) are not searched
        /// </summary>
        public const int MaxSearchFileSize = 2 * 1024 * 1024;

        const int MaxLineLength = 300;
        const int MaxFolderEntries = 300;

        static bool IsWindows => Path.DirectorySeparatorChar == '\\';
        static StringComparison PathComparison => IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        static string NormalizeFolder(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// True if the path is the folder or is located under the folder (both full and normalized)
        /// </summary>
        static bool IsUnder(string path, string folder)
        {
            return string.Equals(path, folder, PathComparison) || path.StartsWith(folder + Path.DirectorySeparatorChar, PathComparison);
        }

        /// <summary>
        /// True if a full path is never readable: Settings (except Settings\AI\Skills), Sources, Security, Logs
        /// </summary>
        public static bool IsForbidden(Repository repository, string fullPath)
        {
            var root = NormalizeFolder(repository.RepositoryPath);
            fullPath = NormalizeFolder(fullPath);
            if (IsUnder(fullPath, NormalizeFolder(repository.SettingsFolder)) && !IsUnder(fullPath, NormalizeFolder(repository.AISkillsFolder))) return true;
            foreach (var folder in new[] { repository.SourcesFolder, repository.SecurityFolder, repository.LogsFolder, Path.Combine(root, "Sources"), Path.Combine(root, "Security"), Path.Combine(root, "Logs") })
            {
                if (IsUnder(fullPath, NormalizeFolder(folder))) return true;
            }
            return false;
        }

        /// <summary>
        /// Full paths of the knowledge folders that exist and are not forbidden
        /// </summary>
        public static List<string> GetAllowedFolders(Repository repository, IEnumerable<string> folders)
        {
            var root = NormalizeFolder(repository.RepositoryPath);
            var result = new List<string>();
            foreach (var folder in folders ?? Enumerable.Empty<string>())
            {
                var relative = FileHelper.ConvertOSFilePath(folder.Trim()).Trim(Path.DirectorySeparatorChar);
                if (string.IsNullOrEmpty(relative) || relative.Contains(':')) continue;
                var full = NormalizeFolder(Path.Combine(root, relative));
                if (!IsUnder(full, root) || IsForbidden(repository, full) || !Directory.Exists(full)) continue;
                if (!result.Contains(full)) result.Add(full);
            }
            return result;
        }

        /// <summary>
        /// Path relative to the repository root
        /// </summary>
        public static string GetRelativePath(Repository repository, string fullPath)
        {
            return Path.GetRelativePath(NormalizeFolder(repository.RepositoryPath), fullPath);
        }

        static string AllowedFoldersText(Repository repository, List<string> allowed)
        {
            return allowed.Count == 0 ? "none" : string.Join(", ", allowed.Select(i => "'" + GetRelativePath(repository, i) + "'"));
        }

        /// <summary>
        /// Resolves a path relative to the repository root (file or folder): returns its full path, or null with an error if it is not in a knowledge folder.
        /// The path is normalized first ('..' and separators), then checked against the knowledge folders and the forbidden folders.
        /// </summary>
        public static string ResolvePath(Repository repository, IEnumerable<string> folders, string path, out string error)
        {
            error = null;
            var allowed = GetAllowedFolders(repository, folders);
            var root = NormalizeFolder(repository.RepositoryPath);
            var relative = FileHelper.ConvertOSFilePath((path ?? "").Trim()).TrimStart(Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(relative))
            {
                error = $"Error: a path relative to the repository is required. Readable folders: {AllowedFoldersText(repository, allowed)}.";
                return null;
            }
            if (Path.IsPathRooted(relative) || relative.Contains(':') || relative.IndexOfAny(new[] { '*', '?', '"', '<', '>', '|' }) >= 0)
            {
                error = $"Error: '{path}' is not a valid path: give a path relative to the repository (e.g. 'Views\\Model.cshtml'), without drive, wildcard or stream. Readable folders: {AllowedFoldersText(repository, allowed)}.";
                return null;
            }

            string full;
            try { full = NormalizeFolder(Path.Combine(root, relative)); }
            catch (Exception ex)
            {
                error = $"Error: '{path}' is not a valid path ({ex.Message}).";
                return null;
            }

            if (!IsUnder(full, root) || IsForbidden(repository, full) || !allowed.Any(i => IsUnder(full, i)))
            {
                error = $"Error: access denied to '{path}': only the knowledge folders can be read ({AllowedFoldersText(repository, allowed)}). The data sources, the security, the settings and the logs are never readable.";
                return null;
            }
            if (!File.Exists(full) && !Directory.Exists(full))
            {
                error = $"Error: '{path}' does not exist. Use seal_search or read a folder (e.g. '{GetRelativePath(repository, allowed.First(i => IsUnder(full, i)))}') to list its files.";
                return null;
            }
            return full;
        }

        /// <summary>
        /// True if the file content looks binary (a NUL character in its first bytes)
        /// </summary>
        public static bool IsBinaryFile(string path)
        {
            var buffer = new byte[8000];
            int read;
            using (var stream = File.OpenRead(path)) read = stream.Read(buffer, 0, buffer.Length);
            return Array.IndexOf(buffer, (byte)0, 0, read) >= 0;
        }

        /// <summary>
        /// Lines of a text file: an XML file saved on a few lines (e.g. a report) is indented, so that it can be read by pages and searched by line
        /// </summary>
        public static string[] GetLines(string path)
        {
            var lines = File.ReadAllLines(path);
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (lines.Length < 5 && (extension == ".srex" || extension == ".xml" || extension == ".srln") && lines.Sum(i => i.Length) > 1000)
            {
                try
                {
                    var doc = XDocument.Load(path);
                    var text = (doc.Declaration != null ? doc.Declaration.ToString() + "\n" : "") + doc.ToString();
                    return text.Replace("\r\n", "\n").Split('\n');
                }
                catch { }
            }
            return lines;
        }

        /// <summary>
        /// Reads a file of a knowledge folder: lines from offset (1-based) for length lines, limited to <see cref="MaxReadLength"/> characters.
        /// For a folder, returns the list of its files and sub-folders.
        /// </summary>
        public static string Read(Repository repository, IEnumerable<string> folders, string path, int offset = 1, int length = DefaultReadLines)
        {
            var full = ResolvePath(repository, folders, path, out string error);
            if (full == null) return error;

            if (Directory.Exists(full)) return ListFolder(repository, full);

            if (IsBinaryFile(full)) return $"Error: '{path}' is a binary file, only text files can be read.";

            var lines = GetLines(full);
            if (offset < 1) offset = 1;
            if (length < 1) length = DefaultReadLines;
            if (offset > lines.Length) return $"Error: offset {offset} is beyond the end of '{path}' ({lines.Length} lines).";

            var sb = new StringBuilder();
            var last = Math.Min(lines.Length, offset + length - 1);
            int i = offset;
            for (; i <= last; i++)
            {
                if (sb.Length + lines[i - 1].Length + 2 > MaxReadLength && i > offset) break;
                //A single line too long is truncated
                sb.AppendLine(lines[i - 1].Length > MaxReadLength ? lines[i - 1].Substring(0, MaxReadLength) + $"… [line truncated, {lines[i - 1].Length} characters]" : lines[i - 1]);
            }
            var lastRead = i - 1;
            var header = $"File '{GetRelativePath(repository, full)}', lines {offset}-{lastRead} of {lines.Length}";
            var footer = lastRead < lines.Length ? $"\r\n[{lines.Length - lastRead} more line(s): call seal_read again with offset={lastRead + 1}]" : "";
            return header + ":\r\n" + sb.ToString().TrimEnd() + footer;
        }

        static string ListFolder(Repository repository, string full)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Folder '{GetRelativePath(repository, full)}':");
            int count = 0;
            foreach (var directory in Directory.GetDirectories(full).OrderBy(i => i))
            {
                if (IsForbidden(repository, directory)) continue;
                if (++count > MaxFolderEntries) break;
                sb.AppendLine($"[folder] {GetRelativePath(repository, directory)}");
            }
            foreach (var file in Directory.GetFiles(full).OrderBy(i => i))
            {
                if (++count > MaxFolderEntries) break;
                sb.AppendLine($"{GetRelativePath(repository, file)} ({new FileInfo(file).Length} bytes)");
            }
            if (count > MaxFolderEntries) sb.AppendLine($"[more than {MaxFolderEntries} entries: read a sub-folder or use seal_search]");
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Searches a regular expression in the text files of the knowledge folders (or of one of their sub-folders), returns the matching lines as 'file:line: text'.
        /// </summary>
        public static string Search(Repository repository, IEnumerable<string> folders, string pattern, string folder = null, string filePattern = null, bool ignoreCase = true)
        {
            if (string.IsNullOrEmpty(pattern)) return "Error: a regular expression is required.";
            Regex regex;
            try { regex = new Regex(pattern, RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None), TimeSpan.FromMilliseconds(500)); }
            catch (ArgumentException ex) { return $"Error: invalid regular expression: {ex.Message}"; }

            List<string> roots;
            if (!string.IsNullOrWhiteSpace(folder))
            {
                var full = ResolvePath(repository, folders, folder, out string error);
                if (full == null) return error;
                roots = new List<string>() { full };
            }
            else roots = GetAllowedFolders(repository, folders);

            if (string.IsNullOrWhiteSpace(filePattern)) filePattern = "*";
            var sb = new StringBuilder();
            int results = 0, filesSearched = 0;
            bool more = false;
            var done = new HashSet<string>(IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (var root in roots)
            {
                IEnumerable<string> files = File.Exists(root) ? new[] { root } : Directory.EnumerateFiles(root, filePattern, SearchOption.AllDirectories).OrderBy(i => i);
                foreach (var file in files)
                {
                    if (more) break;
                    if (!done.Add(file) || IsForbidden(repository, file)) continue;
                    try
                    {
                        if (new FileInfo(file).Length > MaxSearchFileSize || IsBinaryFile(file)) continue;
                        filesSearched++;
                        var lineNumber = 0;
                        foreach (var line in GetLines(file))
                        {
                            lineNumber++;
                            bool match;
                            try { match = regex.IsMatch(line); }
                            catch (RegexMatchTimeoutException) { return "Error: the regular expression is too slow, simplify it."; }
                            if (!match) continue;
                            if (++results > MaxSearchResults) { more = true; break; }
                            var text = line.Trim();
                            if (text.Length > MaxLineLength) text = text.Substring(0, MaxLineLength) + "…";
                            sb.AppendLine($"{GetRelativePath(repository, file)}:{lineNumber}: {text}");
                        }
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }

            if (results == 0) return $"No match in {filesSearched} file(s). Searched folders: {AllowedFoldersText(repository, roots)}.";
            if (more) sb.AppendLine($"[more than {MaxSearchResults} matches: refine the expression or the folder]");
            sb.AppendLine("Read a file with seal_read (path and offset = line number) to see the context.");
            return sb.ToString().TrimEnd();
        }

        #region Class description

        static Dictionary<string, string> _xmlDocs = null;
        static readonly object _docsLock = new object();

        /// <summary>
        /// Summaries of the XML documentation file of the library (SealLibrary.xml beside the assembly), by member name (e.g. 'P:Seal.Model.ReportElement.PivotPosition')
        /// </summary>
        static Dictionary<string, string> XmlDocs
        {
            get
            {
                lock (_docsLock)
                {
                    if (_xmlDocs == null)
                    {
                        _xmlDocs = new Dictionary<string, string>();
                        try
                        {
                            var path = Path.ChangeExtension(typeof(Report).Assembly.Location, ".xml");
                            if (!File.Exists(path)) path = Path.Combine(Path.GetDirectoryName(typeof(Report).Assembly.Location), "SealLibrary.xml");
                            if (File.Exists(path))
                            {
                                foreach (var member in XDocument.Load(path).Descendants("member"))
                                {
                                    var name = (string)member.Attribute("name");
                                    var summary = member.Element("summary");
                                    if (string.IsNullOrEmpty(name) || summary == null) continue;
                                    _xmlDocs[name] = DocText(summary);
                                }
                            }
                        }
                        catch { }
                    }
                    return _xmlDocs;
                }
            }
        }

        static string DocText(XElement element)
        {
            var sb = new StringBuilder();
            foreach (var node in element.Nodes())
            {
                if (node is XText text) sb.Append(text.Value);
                else if (node is XElement child)
                {
                    var cref = (string)child.Attribute("cref") ?? (string)child.Attribute("name") ?? (string)child.Attribute("langword");
                    if (!string.IsNullOrEmpty(cref) && !child.Nodes().Any()) sb.Append(cref.Substring(cref.IndexOf(':') + 1).Split('.').Last());
                    else sb.Append(DocText(child));
                }
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        static string GetDoc(string memberName)
        {
            return XmlDocs.TryGetValue(memberName, out string result) ? result : null;
        }

        static string GetDescription(MemberInfo member, Type declaringType)
        {
            //Property grid attributes (Windows build), then the XML documentation
            var description = member.GetCustomAttribute<DescriptionAttribute>(true)?.Description;
            if (string.IsNullOrWhiteSpace(description))
            {
                var prefix = member is PropertyInfo ? "P:" : member is FieldInfo ? "F:" : "T:";
                description = member is Type type ? GetDoc("T:" + type.FullName) : GetDoc(prefix + declaringType.FullName + "." + member.Name);
            }
            return string.IsNullOrWhiteSpace(description) ? "" : Regex.Replace(description, @"\s+", " ").Trim();
        }

        static bool IsCollection(Type type)
        {
            return type != typeof(string) && (type.IsArray || (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type)));
        }

        static Type GetItemType(Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType) return type.GetGenericArguments().FirstOrDefault();
            return null;
        }

        /// <summary>
        /// Serialized members of a class (public properties with a setter or collections, public fields), without the ones ignored by the XML serialization
        /// </summary>
        static List<MemberInfo> GetSerializedMembers(Type type)
        {
            var result = new List<MemberInfo>();
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0 || property.GetGetMethod() == null) continue;
                if (property.GetCustomAttribute<XmlIgnoreAttribute>(true) != null) continue;
                if (property.GetSetMethod() == null && !IsCollection(property.PropertyType)) continue;
                //A property hidden with 'new' appears twice: keep the most derived one
                if (result.Any(i => i.Name == property.Name)) continue;
                result.Add(property);
            }
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsInitOnly || field.GetCustomAttribute<XmlIgnoreAttribute>(true) != null || result.Any(i => i.Name == field.Name)) continue;
                result.Add(field);
            }
            return result;
        }

        static List<Type> _reportTypes = null;

        /// <summary>
        /// Classes that can be described: the classes reachable from a report and a data source by the XML serialization, with their derived classes.
        /// </summary>
        public static List<Type> ReportTypes
        {
            get
            {
                lock (_docsLock)
                {
                    if (_reportTypes == null)
                    {
                        var sealTypes = typeof(Report).Assembly.GetTypes().Where(i => i.IsClass && i.IsPublic && (i.Namespace == "Seal.Model" || i.Namespace == "Seal.Renderer")).ToList();
                        var result = new List<Type>();
                        var queue = new Queue<Type>(new[] { typeof(Report), typeof(MetaSource) });
                        while (queue.Count > 0)
                        {
                            var type = queue.Dequeue();
                            if (result.Contains(type) || !sealTypes.Contains(type)) continue;
                            result.Add(type);
                            foreach (var derived in sealTypes.Where(i => i.IsSubclassOf(type))) queue.Enqueue(derived);
                            foreach (var member in GetSerializedMembers(type))
                            {
                                var memberType = member is PropertyInfo p ? p.PropertyType : ((FieldInfo)member).FieldType;
                                queue.Enqueue(IsCollection(memberType) ? GetItemType(memberType) : memberType);
                            }
                        }
                        _reportTypes = result.Where(i => i != null).OrderBy(i => i.Name).ToList();
                    }
                    return _reportTypes;
                }
            }
        }

        static string TypeText(Type type, MemberInfo member)
        {
            if (IsCollection(type))
            {
                var itemType = GetItemType(type);
                var itemName = member.GetCustomAttribute<XmlArrayItemAttribute>(true)?.ElementName;
                if (string.IsNullOrEmpty(itemName)) itemName = itemType == typeof(string) ? "string" : itemType?.Name;
                return $"list of <{itemName}>" + (itemType != null && itemType.Name != itemName ? $" ({itemType.Name})" : "");
            }
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null) return TypeText(underlying, member) + " (optional)";
            if (type.IsEnum) return "enum " + type.Name;
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool (true/false)";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return "integer";
            if (type == typeof(double) || type == typeof(decimal) || type == typeof(float)) return "number";
            if (type == typeof(DateTime)) return "date time (yyyy-MM-ddTHH:mm:ss)";
            return type.Name;
        }

        static string EnumValueName(Type enumType, string name)
        {
            var xmlEnum = enumType.GetField(name)?.GetCustomAttribute<XmlEnumAttribute>();
            return !string.IsNullOrEmpty(xmlEnum?.Name) ? xmlEnum.Name : name;
        }

        static string ValueText(object value, Type type)
        {
            if (value == null) return "";
            if (type.IsEnum) return EnumValueName(type, value.ToString());
            if (value is bool b) return b ? "true" : "false";
            if (value is DateTime d) return d == DateTime.MinValue ? "" : d.ToString("yyyy-MM-ddTHH:mm:ss");
            if (value is string s) return s.Length > 80 ? s.Substring(0, 80) + "…" : s;
            if (value is IEnumerable) return "";
            if (!type.IsValueType) return "";
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Describes a class serialized in a report (e.g. ReportElement): its XML elements with their type, default value, enumeration values, description and the code level required to write them (see <see cref="SealCodeAttribute"/>).
        /// </summary>
        public static string DescribeClass(string name)
        {
            name = (name ?? "").Trim().Trim('<', '>', '/');
            var types = ReportTypes;
            var type = types.FirstOrDefault(i => i.Name == name) ?? types.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
            if (type == null)
            {
                return $"Error: '{name}' is not a class of a report. Classes: {string.Join(", ", types.Select(i => i.Name))}.";
            }

            object instance = null;
            try { instance = Activator.CreateInstance(type); } catch { }

            var sb = new StringBuilder();
            sb.AppendLine($"# Class {type.Name} (XML element <{type.Name}>)");
            var classDescription = GetDescription(type, type);
            if (!string.IsNullOrEmpty(classDescription)) sb.AppendLine(classDescription);
            if (type.BaseType != null && types.Contains(type.BaseType)) sb.AppendLine($"Base class: {type.BaseType.Name}");
            var derived = types.Where(i => i.BaseType == type).Select(i => i.Name).ToList();
            if (derived.Count > 0) sb.AppendLine($"Derived classes: {string.Join(", ", derived)}");
            var classLevel = ReportCodeGuard.GetRequiredLevel(type, null);
            if (classLevel > ReportCodeLevel.Metadata) sb.AppendLine($"Writing an element of this class requires the '{classLevel}' code level.");
            sb.AppendLine();
            sb.AppendLine("## XML elements (name | type | default | code level | description)");
            sb.AppendLine("An element omitted in the XML keeps its default value. Code level: Metadata unless stated (Sql or Scripts: refused above the level of the writer).");

            var enums = new List<Type>();
            foreach (var member in GetSerializedMembers(type).OrderBy(i => i.Name))
            {
                var property = member as PropertyInfo;
                var memberType = property != null ? property.PropertyType : ((FieldInfo)member).FieldType;
                var elementName = member.GetCustomAttribute<XmlArrayAttribute>(true)?.ElementName;
                if (string.IsNullOrEmpty(elementName)) elementName = member.GetCustomAttribute<XmlElementAttribute>(true)?.ElementName;
                if (string.IsNullOrEmpty(elementName)) elementName = member.GetCustomAttribute<XmlAttributeAttribute>(true) != null ? "@" + member.Name : member.Name;

                string defaultValue = "";
                var defaultAttribute = member.GetCustomAttribute<DefaultValueAttribute>(true);
                if (defaultAttribute != null) defaultValue = ValueText(defaultAttribute.Value, Nullable.GetUnderlyingType(memberType) ?? memberType);
                else if (instance != null)
                {
                    try { defaultValue = ValueText(property != null ? property.GetValue(instance) : ((FieldInfo)member).GetValue(instance), Nullable.GetUnderlyingType(memberType) ?? memberType); } catch { }
                }

                var level = property != null ? ReportCodeGuard.GetRequiredLevel(type, property) : classLevel;
                var levelText = level > ReportCodeLevel.Metadata ? level.ToString() : "";
                if (property != null && ReportCodeGuard.IsHtmlProperty(type, property)) levelText = (levelText + " HTML: active content requires Scripts").Trim();

                var description = GetDescription(member, member.DeclaringType);
                var displayName = member.GetCustomAttribute<DisplayNameAttribute>(true)?.DisplayName?.Trim();
                if (!string.IsNullOrEmpty(displayName) && displayName != member.Name) description = $"(Designer: '{displayName}') " + description;

                sb.AppendLine($"- {elementName} | {TypeText(memberType, member)} | {defaultValue} | {levelText} | {description}".TrimEnd());

                var enumType = Nullable.GetUnderlyingType(memberType) ?? memberType;
                if (IsCollection(memberType)) enumType = GetItemType(memberType);
                if (enumType != null && enumType.IsEnum && !enums.Contains(enumType)) enums.Add(enumType);
            }

            if (enums.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Enumerations (XML value: description)");
                foreach (var enumType in enums)
                {
                    var values = Enum.GetNames(enumType).Select(i =>
                    {
                        var field = enumType.GetField(i);
                        var description = field.GetCustomAttribute<DescriptionAttribute>()?.Description;
                        if (string.IsNullOrWhiteSpace(description)) description = GetDoc("F:" + enumType.FullName + "." + i);
                        var value = EnumValueName(enumType, i);
                        return string.IsNullOrWhiteSpace(description) ? value : $"{value} ({description.Trim()})";
                    });
                    sb.AppendLine($"- {enumType.Name}: {string.Join("; ", values)}{(enumType.GetCustomAttribute<FlagsAttribute>() != null ? " [flags: several values separated by a space]" : "")}");
                }
            }
            return sb.ToString().TrimEnd();
        }

        #endregion
    }
}
