//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using PuppeteerSharp;
using Seal.Helpers;
using Seal.Model;
using Seal.Renderer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Seal.AI
{
    /// <summary>
    /// Operation of the AI tool report_patch_xml on the XML of a report
    /// </summary>
    public class AIPatchOperation
    {
        /// <summary>
        /// 'set' (text of the elements or attributes selected, or replace the elements by an XML fragment), 'insert' (XML fragment) or 'remove'
        /// </summary>
        public string Op;

        /// <summary>
        /// XPath selecting the target nodes, e.g. /Report/Models/ReportModel[Name='Model']/Elements
        /// </summary>
        public string XPath;

        /// <summary>
        /// Text for 'set'
        /// </summary>
        public string Value;

        /// <summary>
        /// XML fragment for 'insert', or for 'set' to replace the selected elements
        /// </summary>
        public string Xml;

        /// <summary>
        /// Position of an 'insert': append (last child, default), prepend (first child), before or after (sibling)
        /// </summary>
        public string Position;
    }

    /// <summary>
    /// Writes the reports of the AI tools report_create_from_xml and report_patch_xml: report code level check, validation of the references to the data sources
    /// (columns, joins, enumerated lists), restrictions and internal GUIDs, record limit, save, then test execution and rendering of the views.
    /// </summary>
    public class AIReportWriter
    {
        /// <summary>
        /// Record limit set on the models of the reports written by the AI agent when they have no positive limit
        /// </summary>
        public const int DefaultMaxNumberOfRecords = 5000;

        /// <summary>
        /// Maximum duration of the test execution
        /// </summary>
        public const int TestTimeoutSeconds = 30;

        /// <summary>
        /// Maximum number of operations of a patch
        /// </summary>
        public const int MaxPatchOperations = 100;

        /// <summary>
        /// Maximum height in pixels of the picture of the rendered report
        /// </summary>
        public static int MaxPictureHeight = 2400;

        const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";

        readonly AIToolConfiguration _tool;

        /// <summary>
        /// Constructor for the tool executing the call
        /// </summary>
        public AIReportWriter(AIToolConfiguration tool)
        {
            _tool = tool;
        }

        string ToolName => _tool?.Name ?? "report_create_from_xml";
        SecurityUser SecurityContext => _tool?.CurrentToolCall?.SecurityContext;
        Repository Repository => _tool != null ? _tool.GetRepository() : Repository.Instance;

        void log(string message)
        {
            _tool?.CurrentToolCall?.LogMessage(message);
        }

        #region Paths

        Dictionary<string, string> getRoots()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Reports"] = Repository.ReportsFolder,
                ["Personal"] = Repository.GetPersonalFolder(SecurityContext),
            };
        }

        /// <summary>
        /// Full path of a report to create from its path relative to a root (Reports or Personal, Reports by default). Returns an error or null.
        /// </summary>
        public string ResolveNewReportPath(string path, out string fullPath)
        {
            fullPath = null;
            path = FileHelper.ConvertOSFilePath(path ?? "");
            var roots = getRoots();
            //A folder is not a report path: 'Personal\' would resolve outside the user's folders and end in a misleading 'write access denied'
            if (string.IsNullOrWhiteSpace(path) || path.EndsWith(Path.DirectorySeparatorChar) || roots.ContainsKey(path))
                return $"Error: 'path' must end with a report file name, for instance '{path.TrimEnd(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}my_report.srex' (got the folder '{path}'). You have not been denied access: call the tool again with a file name.";

            foreach (var kv in roots)
            {
                if (path.StartsWith(kv.Key + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) { fullPath = Path.Combine(kv.Value, path.Substring(kv.Key.Length + 1)); break; }
            }
            if (fullPath == null) fullPath = Path.Combine(Repository.ReportsFolder, path);
            if (!fullPath.EndsWith("." + Repository.SealReportFileExtension, StringComparison.OrdinalIgnoreCase)) fullPath += "." + Repository.SealReportFileExtension;
            return null;
        }

        /// <summary>
        /// Full path of an existing report from its path relative to a root (Reports or Personal). Returns an error or null.
        /// </summary>
        public string ResolveExistingReportPath(string path, out string fullPath)
        {
            fullPath = null;
            path = FileHelper.ConvertOSFilePath(path ?? "");
            foreach (var kv in getRoots())
            {
                var candidate = path.StartsWith(kv.Key + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? Path.Combine(kv.Value, path.Substring(kv.Key.Length + 1)) : Path.Combine(kv.Value, path);
                if (!candidate.EndsWith("." + Repository.SealReportFileExtension, StringComparison.OrdinalIgnoreCase) && !File.Exists(candidate)) candidate += "." + Repository.SealReportFileExtension;
                if (File.Exists(candidate)) { fullPath = candidate; return null; }
            }
            return $"Error: report '{path}' not found. Use report_list to get the path of a report.";
        }

        #endregion

        #region Tools

        /// <summary>
        /// Creates or replaces a report from its complete XML (tool report_create_from_xml): all internal GUIDs are regenerated.
        /// </summary>
        public string CreateFromXml(string path, string xml, bool overwrite, bool noRecordLimit, bool renderPicture = false)
        {
            if (SecurityContext == null) return "Error: no security context available.";
            var error = ResolveNewReportPath(path, out string fullPath);
            if (error != null) return error;

            if (File.Exists(fullPath) && !overwrite) return $"Error: '{path}' already exists. Set overwrite=true to replace it.";
            if (!SecurityContext.HasFolderRight(Path.GetDirectoryName(fullPath), (int)FolderRight.Edit)) return $"Error: write access denied to '{path}'.";

            XDocument doc;
            try { doc = XDocument.Parse(xml); }
            catch (Exception ex) { return $"Error: {ex.Message}"; }

            var existingXml = overwrite && File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
            return Write(path, fullPath, doc, xml, existingXml, true, null, noRecordLimit, renderPicture, "Created");
        }

        /// <summary>
        /// Applies XPath operations to an existing report (tool report_patch_xml): the GUIDs of the report are kept, only the placeholders (and the GUIDs duplicated by an inserted fragment) are regenerated.
        /// </summary>
        public string Patch(string path, List<AIPatchOperation> operations, bool noRecordLimit, bool renderPicture = false)
        {
            if (SecurityContext == null) return "Error: no security context available.";
            var error = ResolveExistingReportPath(path, out string fullPath);
            if (error != null) return error;
            if (!SecurityContext.HasFolderRight(Path.GetDirectoryName(fullPath), (int)FolderRight.Edit)) return $"Error: write access denied to '{path}'.";

            var existingXml = File.ReadAllText(fullPath);
            XDocument doc;
            try { doc = XDocument.Parse(existingXml); }
            catch (Exception ex) { return $"Error: the saved report is not valid XML: {ex.Message}"; }

            var inserted = new List<XElement>();
            error = ApplyPatch(doc, operations, inserted);
            if (error != null) return error + " Nothing was saved: fix the operation and call report_patch_xml again (call report_get_xml to check the current XML).";

            return Write(path, fullPath, doc, ToXmlText(doc), existingXml, false, inserted, noRecordLimit, renderPicture, "Patched");
        }

        /// <summary>
        /// Parses the operations of a report_patch_xml call (JSON array of {op, xpath, value, xml, position})
        /// </summary>
        public static List<AIPatchOperation> ParseOperations(string json)
        {
            var result = new List<AIPatchOperation>();
            using (var document = JsonDocument.Parse(json))
            {
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object) root = root.TryGetProperty("operations", out var ops) ? ops : root;
                if (root.ValueKind != JsonValueKind.Array) throw new Exception("'operations' must be an array of {op, xpath, value, xml, position}.");
                foreach (var item in root.EnumerateArray())
                {
                    string get(string name) => item.TryGetProperty(name, out var value) ? (value.ValueKind == JsonValueKind.String ? value.GetString() : value.ValueKind == JsonValueKind.Null ? null : value.GetRawText()) : null;
                    result.Add(new AIPatchOperation() { Op = get("op"), XPath = get("xpath"), Value = get("value"), Xml = get("xml"), Position = get("position") });
                }
            }
            return result;
        }

        #endregion

        #region Patch

        static IEnumerable<XElement> parseFragment(string xml)
        {
            var root = XElement.Parse($"<_fragment xmlns:xsi=\"{XsiNamespace}\" xmlns:xsd=\"{XsdNamespace}\">{xml}</_fragment>");
            var result = root.Elements().ToList();
            if (result.Count == 0) throw new Exception("the 'xml' fragment contains no element");
            //Detached: an element having a parent is cloned when it is added, the elements returned must be the ones inserted
            foreach (var element in result) element.Remove();
            return result;
        }

        /// <summary>
        /// Applies the operations to the document: returns an error (nothing applied is kept by the caller) or null. The elements inserted are added to the list.
        /// </summary>
        public static string ApplyPatch(XDocument doc, List<AIPatchOperation> operations, List<XElement> inserted)
        {
            if (operations == null || operations.Count == 0) return "Error: no operation.";
            if (operations.Count > MaxPatchOperations) return $"Error: too many operations ({operations.Count}, maximum {MaxPatchOperations}): split the patch or use report_create_from_xml.";

            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                var label = $"Error: operation {i + 1} ({op.Op} '{op.XPath}'):";
                if (string.IsNullOrWhiteSpace(op.XPath)) return $"{label} 'xpath' is required.";
                List<object> targets;
                try
                {
                    var result = doc.XPathEvaluate(op.XPath);
                    if (result is IEnumerable<object> nodes) targets = nodes.ToList();
                    else return $"{label} the XPath must select elements or attributes (it returns the value '{result}').";
                }
                catch (XPathException ex) { return $"{label} invalid XPath: {ex.Message}"; }
                catch (Exception ex) { return $"{label} {ex.Message}"; }

                try
                {
                    switch ((op.Op ?? "").Trim().ToLowerInvariant())
                    {
                        case "set":
                            if (op.Xml != null)
                            {
                                if (targets.Count == 0) return $"{label} the XPath matches nothing.";
                                foreach (var target in targets)
                                {
                                    if (target is not XElement element || element.Parent == null) return $"{label} with 'xml', the XPath must select elements that are not the root (they are replaced by the fragment).";
                                    var fragment = parseFragment(op.Xml).ToList();
                                    element.ReplaceWith(fragment);
                                    inserted.AddRange(fragment);
                                }
                                break;
                            }
                            if (op.Value == null) return $"{label} 'value' (or 'xml' to replace the elements) is required.";
                            if (targets.Count == 0)
                            {
                                //Element not in the XML (default value): created in its parent
                                var match = Regex.Match(op.XPath.Trim(), @"^(.+)/([A-Za-z_][\w\-\.]*)$");
                                if (!match.Success) return $"{label} the XPath matches nothing.";
                                var parents = doc.XPathSelectElements(match.Groups[1].Value).ToList();
                                if (parents.Count == 0) return $"{label} the XPath matches nothing, nor its parent '{match.Groups[1].Value}'.";
                                foreach (var parent in parents) parent.Add(new XElement(match.Groups[2].Value, op.Value));
                                break;
                            }
                            foreach (var target in targets)
                            {
                                if (target is XAttribute attribute) attribute.Value = op.Value;
                                else if (target is XElement element)
                                {
                                    if (element.HasElements) return $"{label} the element <{element.Name.LocalName}> has child elements: set a child element, or use 'xml' to replace it, or 'remove' and 'insert'.";
                                    element.Value = op.Value;
                                }
                                else if (target is XText text) text.Value = op.Value;
                                else return $"{label} the XPath must select elements, attributes or texts.";
                            }
                            break;

                        case "insert":
                            if (string.IsNullOrWhiteSpace(op.Xml)) return $"{label} 'xml' is required.";
                            if (targets.Count == 0) return $"{label} the XPath matches nothing.";
                            var position = string.IsNullOrWhiteSpace(op.Position) ? "append" : op.Position.Trim().ToLowerInvariant();
                            if (position != "append" && position != "prepend" && position != "before" && position != "after") return $"{label} 'position' must be append, prepend, before or after.";
                            foreach (var target in targets)
                            {
                                if (target is not XElement element) return $"{label} the XPath must select elements.";
                                if ((position == "before" || position == "after") && element.Parent == null) return $"{label} cannot insert a sibling of the root element.";
                                var fragment = parseFragment(op.Xml).ToList();
                                if (position == "append") element.Add(fragment);
                                else if (position == "prepend") element.AddFirst(fragment);
                                else if (position == "before") element.AddBeforeSelf(fragment);
                                else element.AddAfterSelf(fragment);
                                inserted.AddRange(fragment);
                            }
                            break;

                        case "remove":
                            if (targets.Count == 0) return $"{label} the XPath matches nothing.";
                            foreach (var target in targets)
                            {
                                if (target is XElement element)
                                {
                                    if (element.Parent == null) return $"{label} the root element cannot be removed.";
                                    element.Remove();
                                }
                                else if (target is XAttribute attribute) attribute.Remove();
                                else if (target is XNode node) node.Remove();
                            }
                            break;

                        default:
                            return $"{label} 'op' must be set, insert or remove.";
                    }
                }
                catch (XmlException ex) { return $"{label} invalid 'xml' fragment: {ex.Message}"; }
                catch (Exception ex) { return $"{label} {ex.Message}"; }
            }
            if (doc.Root == null || doc.Root.Name.LocalName != "Report") return "Error: the root element must stay <Report>.";
            return null;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Columns of a data source used by a report source: the columns of the data source and of the tables defined in the report source
        /// </summary>
        class SourceColumns
        {
            public MetaSource Source;
            public HashSet<string> ValidGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> GuidByName = new Dictionary<string, string>();
            public Dictionary<string, string> NameByGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> GuidByColName = new Dictionary<string, string>();
            public HashSet<string> AmbiguousColNames = new HashSet<string>();
            public Dictionary<string, MetaColumn> ColumnByGuid = new Dictionary<string, MetaColumn>(StringComparer.OrdinalIgnoreCase);

            public void Add(string guid, string name, string displayName, MetaColumn column)
            {
                if (string.IsNullOrEmpty(guid)) return;
                ValidGuids.Add(guid);
                GuidByName[norm(name)] = guid;
                if (!string.IsNullOrEmpty(displayName)) GuidByName[norm(displayName)] = guid;
                NameByGuid[guid] = name ?? "";
                var colKey = stripCast(norm(name));
                if (GuidByColName.ContainsKey(colKey)) AmbiguousColNames.Add(colKey);
                else GuidByColName[colKey] = guid;
                if (column != null) ColumnByGuid[guid] = column;
            }
        }

        static string norm(string s) => (s ?? "").Replace("[", "").Replace("]", "").Trim().ToLowerInvariant();
        static string stripCast(string s) { var i = s.IndexOf("::"); return i < 0 ? s : s.Substring(0, i).Trim(); }
        static string text(XElement parent, string name) => parent?.Element(name) == null ? null : parent.Element(name).Value?.Trim();

        /// <summary>
        /// Returns the XML text of a document with its declaration
        /// </summary>
        public static string ToXmlText(XDocument doc)
        {
            var sb = new StringBuilder();
            if (doc.Declaration != null) sb.Append(doc.Declaration).Append("\r\n");
            sb.Append(doc.ToString());
            return sb.ToString();
        }

        /// <summary>
        /// Validates and completes the report XML in place: data sources, columns, joins, enumerated lists, restrictions, maps, record limit and internal GUIDs.
        /// Returns an error to send back to the agent, or null if the XML can be saved.
        /// </summary>
        /// <param name="doc">The report XML</param>
        /// <param name="existingXml">The XML of the report already saved (null for a new report)</param>
        /// <param name="regenerateAllGuids">True to regenerate all the internal GUIDs (complete XML), false to keep them and regenerate only the placeholders (patch)</param>
        /// <param name="inserted">For a patch, the elements inserted: their GUIDs already used in the report are regenerated</param>
        /// <param name="noRecordLimit">True to keep the models without record limit</param>
        /// <param name="warnings">Warnings to return with the result</param>
        /// <param name="limitedModels">Number of models limited to the default number of records</param>
        public string Validate(XDocument doc, string existingXml, bool regenerateAllGuids, List<XElement> inserted, bool noRecordLimit, List<string> warnings, out int limitedModels)
        {
            limitedModels = 0;
            var available = _tool != null ? _tool.GetAvailableSources().ToList() : Repository.Sources.ToList();

            //--- Data sources of the report: each <MetaSourceGUID> must be a source the user can access (Private Data Sources granted included)
            var reportSources = doc.Descendants("ReportSource").ToList();
            var sourceColumnsByReportSource = new Dictionary<XElement, SourceColumns>();
            foreach (var reportSource in reportSources)
            {
                var msGuid = text(reportSource, "MetaSourceGUID");
                if (string.IsNullOrEmpty(msGuid)) continue;
                var metaSource = available.FirstOrDefault(s => string.Equals(s.GUID, msGuid, StringComparison.OrdinalIgnoreCase));
                if (metaSource == null) return $"Error: MetaSourceGUID '{msGuid}' does not match any data source you can access. Use the GUID from datasource_list.";
                var columns = new SourceColumns() { Source = metaSource };
                foreach (var table in metaSource.MetaData.Tables)
                    foreach (var col in table.Columns) columns.Add(col.GUID, col.Name, col.DisplayName, col);
                //Tables defined in the report source
                foreach (var col in reportSource.Elements("MetaData").Elements("Tables").Elements("MetaTable").Elements("Columns").Elements("MetaColumn"))
                    columns.Add(text(col, "GUID"), text(col, "Name"), text(col, "DisplayName"), null);
                sourceColumnsByReportSource[reportSource] = columns;
            }
            if (sourceColumnsByReportSource.Count == 0)
            {
                var msGuid = doc.Descendants("MetaSourceGUID").Select(e => e.Value?.Trim()).FirstOrDefault(v => !string.IsNullOrEmpty(v));
                return $"Error: MetaSourceGUID '{msGuid}' does not match any data source you can access. Use the GUID from datasource_list.";
            }
            var defaultColumns = sourceColumnsByReportSource.Values.First();
            var columnsByInternalGuid = new Dictionary<string, SourceColumns>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in sourceColumnsByReportSource)
            {
                var guid = text(kv.Key, "GUID");
                if (!string.IsNullOrEmpty(guid)) columnsByInternalGuid[guid] = kv.Value;
            }
            //Columns of the source of a node: the source of its model, the source containing it, or the first source
            SourceColumns columnsOf(XElement node)
            {
                var model = node.AncestorsAndSelf("ReportModel").FirstOrDefault();
                var sourceGuid = model != null ? text(model, "SourceGUID") : null;
                if (!string.IsNullOrEmpty(sourceGuid) && columnsByInternalGuid.TryGetValue(sourceGuid, out var result)) return result;
                var reportSource = node.AncestorsAndSelf("ReportSource").FirstOrDefault();
                if (reportSource != null && sourceColumnsByReportSource.TryGetValue(reportSource, out result)) return result;
                return defaultColumns;
            }

            //--- Restrictions are editable criteria by default: a restriction without <Prompt> is prompted
            foreach (var rest in doc.Descendants("ReportRestriction").Where(r => r.Element("Prompt") == null || string.IsNullOrWhiteSpace(r.Element("Prompt").Value)))
            {
                if (rest.Element("Prompt") != null) rest.Element("Prompt").Value = "Prompt";
                else if (rest.Element("Name") != null) rest.Element("Name").AddAfterSelf(new XElement("Prompt", "Prompt"));
                else rest.Add(new XElement("Prompt", "Prompt"));
            }

            //--- Elements and restrictions already saved (element GUID -> MetaColumnGUID): when the designer changed their column, their <Name> may be stale; such a Name is corrected instead of being rejected
            var existingColByElement = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(existingXml))
            {
                try
                {
                    foreach (var e in XDocument.Parse(existingXml).Descendants().Where(x => x.Name.LocalName == "ReportElement" || x.Name.LocalName == "ReportRestriction"))
                    {
                        var eGuid = text(e, "GUID") ?? "";
                        var eCol = text(e, "MetaColumnGUID") ?? "";
                        if (eGuid != "" && eCol != "") existingColByElement[eGuid] = eCol;
                    }
                }
                catch { }
            }

            //--- Metadata column references (guard against fabricated GUIDs)
            var badRefs = new List<string>();
            var mismatchedRefs = new List<string>();
            foreach (var mc in doc.Descendants("MetaColumnGUID").ToList())
            {
                var columns = columnsOf(mc);
                var metaSource = columns.Source;
                var val = mc.Value == null ? "" : mc.Value.Trim();
                var refName = mc.Parent == null ? null : (string)mc.Parent.Element("Name");
                if (!columns.ValidGuids.Contains(val))
                {
                    if (refName != null && columns.GuidByName.TryGetValue(norm(refName), out string realGuid))
                    {
                        log($"Resolved MetaColumnGUID for '{refName.Trim()}': '{val}' -> '{realGuid}'");
                        mc.Value = realGuid;
                        val = realGuid;
                    }
                    else
                    {
                        badRefs.Add((refName != null ? $"'{refName.Trim()}' (supplied GUID '{val}')" : $"GUID '{val}'") + (sourceColumnsByReportSource.Count > 1 ? $" in source '{metaSource.Name}'" : ""));
                        continue;
                    }
                }
                else if (!metaSource.IsNoSQL && !string.IsNullOrWhiteSpace(refName))
                {
                    //A valid GUID copied from another column: the element shows the wrong column under the expected title.
                    //Names are compared without their SQL cast suffix (e.g. 'fr.v_table.name::citext').
                    var nameKey = stripCast(norm(refName));
                    var guidColKey = stripCast(norm(columns.NameByGuid[val]));
                    var elementGuid = text(mc.Parent, "GUID") ?? "";
                    if (nameKey != guidColKey && elementGuid != "" && existingColByElement.TryGetValue(elementGuid, out string savedCol) && string.Equals(savedCol, val, StringComparison.OrdinalIgnoreCase))
                    {
                        //Same element with the same column as in the saved report: only its <Name> is stale
                        log($"Corrected stale Name '{refName.Trim()}' -> '{columns.NameByGuid[val]}'");
                        mc.Parent.Element("Name").Value = columns.NameByGuid[val];
                    }
                    else if (nameKey != guidColKey)
                    {
                        if (!columns.AmbiguousColNames.Contains(nameKey) && columns.GuidByColName.TryGetValue(nameKey, out string namedGuid))
                            mismatchedRefs.Add($"'{refName.Trim()}' has the MetaColumnGUID '{val}' of column '{columns.NameByGuid[val]}' (the GUID of '{refName.Trim()}' is '{namedGuid}')");
                        else if (nameKey.Contains('.') && guidColKey.Contains('.') && nameKey.Substring(0, nameKey.LastIndexOf('.')) == guidColKey.Substring(0, guidColKey.LastIndexOf('.')))
                            mismatchedRefs.Add($"'{refName.Trim()}' is not a column of the source, its MetaColumnGUID '{val}' is the one of column '{columns.NameByGuid[val]}' (check in which table the column really is)");
                    }
                }

                //NoSQL sources resolve each element/restriction in memory by matching its <Name> to a result-table column, whose name is the metadata column's raw
                //field name (e.g. 'year', 'transactions.amount'). A table-qualified name like 'transactions.year' matches nothing and the report fails.
                if (metaSource.IsNoSQL && mc.Parent != null && columns.NameByGuid.TryGetValue(val, out string rawName))
                {
                    var nameEl = mc.Parent.Element("Name");
                    if (nameEl == null) mc.Parent.AddFirst(new XElement("Name", rawName));
                    else if (nameEl.Value != rawName)
                    {
                        log($"NoSQL: corrected element Name '{nameEl.Value}' -> '{rawName}'");
                        nameEl.Value = rawName;
                    }
                }
            }
            if (badRefs.Count > 0)
            {
                var sourceNames = string.Join(", ", sourceColumnsByReportSource.Values.Select(i => "'" + i.Source.Name + "'").Distinct());
                return $"Error: these MetaColumnGUID values are not columns of source {sourceNames}: " + string.Join("; ", badRefs) +
                    ". Call datasource_get_detail and copy the exact column GUIDs - never invent or sequence GUIDs.";
            }
            if (mismatchedRefs.Count > 0)
            {
                return "Error: the <Name> and the <MetaColumnGUID> designate different columns: " + string.Join("; ", mismatchedRefs) +
                    $". Each element and restriction must use the GUID of its own column from datasource_get_detail. Fix the XML and call {ToolName} again.";
            }

            //--- Join overrides: a JoinGUID that is not a join of the source is silently ignored by the engine
            var badJoins = doc.Descendants("JoinOverride")
                .Select(jo => new { Guid = text(jo, "JoinGUID") ?? "", Source = columnsOf(jo).Source })
                .Where(j => !j.Source.MetaData.Joins.Any(i => string.Equals(i.GUID, j.Guid, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (badJoins.Count > 0)
            {
                return "Error: these JoinGUID values are not joins of source '" + badJoins.First().Source.Name + "': " + string.Join("; ", badJoins.Select(j => "'" + j.Guid + "'")) +
                    ". Call datasource_get_detail and copy the exact join GUID listed under '## Joins'.";
            }

            //--- Restrictions (guard against definitions the engine silently ignores): an enumerated restriction is applied only when its operator is Equal/NotEqual and
            //<EnumValues> is filled: a <Value1> or a text operator (Contains, StartsWith...) makes the engine treat it as "not filled" and skip it, so the report runs
            //UNFILTERED with no error. Such definitions are rejected so the caller can fix the XML.
            var restrictionErrors = new List<string>();
            var valueLessOperators = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IsNull", "IsNotNull", "IsEmpty", "IsNotEmpty" };

            //Enumerated lists defined in the report itself (<ReportSource><MetaData><Enums><MetaEnum>), referenced by <EnumGUIDRE> (restriction) or <EnumGUIDEL> (element).
            //A column enum of the data source is inherited without reference.
            var reportEnumGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var re in doc.Descendants("ReportSource").Elements("MetaData").Elements("Enums").Elements("MetaEnum"))
            {
                var reGuid = text(re, "GUID") ?? "";
                var reName = text(re, "Name") ?? "";
                var reSql = text(re, "Sql") ?? "";
                var reDynamic = (text(re, "IsDynamic") ?? "").ToLowerInvariant() == "true";
                var reHasValues = re.Element("Values") != null && re.Element("Values").Elements("MetaEV").Any();
                if (string.IsNullOrEmpty(reGuid) || string.IsNullOrEmpty(reName)) { restrictionErrors.Add("a report enumerated list <MetaEnum> must have a <GUID> and a <Name>"); continue; }
                reportEnumGuids.Add(reGuid);
                if (reDynamic && string.IsNullOrEmpty(reSql)) { restrictionErrors.Add($"enumerated list '{reName}': a dynamic list (<IsDynamic>true</IsDynamic>) needs a <Sql> select statement"); continue; }
                if (!reDynamic && !reHasValues) { restrictionErrors.Add($"enumerated list '{reName}': a static list needs <Values><MetaEV><Id>…</Id><Val>…</Val></MetaEV></Values>, or set <IsDynamic>true</IsDynamic> with a <Sql>"); continue; }
                var source = columnsOf(re).Source;
                if (reDynamic && !source.IsNoSQL)
                {
                    //Check the SQL now: a failing list shows an empty prompt at execution
                    var check = MetaEnum.Create();
                    check.Name = reName;
                    check.IsDynamic = true;
                    check.Sql = reSql;
                    check.Source = source;
                    check.RefreshEnum();
                    if (!string.IsNullOrEmpty(check.Error)) restrictionErrors.Add($"enumerated list '{reName}': the <Sql> failed: {check.Error}");
                    else log($"Enumerated list '{reName}': {check.Values.Count} value(s) loaded");
                }
            }

            //'No enumerated list' choice of the Designer: CLEAR (ReportElement.kClearEnumGUID), or its label saved by older versions.
            //Not a reference to a list: the engine then uses the list of the column, if any (an element with CLEAR has none)
            bool isNoListValue(string v) => v == ReportElement.kClearEnumGUID || v == "<No Enumerated List>";

            foreach (var el in doc.Descendants("ReportElement").Where(e => !string.IsNullOrWhiteSpace(text(e, "EnumGUIDEL")) && !isNoListValue(text(e, "EnumGUIDEL"))))
            {
                var g = text(el, "EnumGUIDEL");
                if (!reportEnumGuids.Contains(g) && !columnsOf(el).Source.MetaData.Enums.Any(e => string.Equals(e.GUID, g, StringComparison.OrdinalIgnoreCase)))
                    restrictionErrors.Add($"element '{(string)el.Element("Name")}': <EnumGUIDEL> '{g}' matches no enumerated list of the report or of the data source");
            }

            foreach (var rest in doc.Descendants("ReportRestriction"))
            {
                var columns = columnsOf(rest);
                var colGuid = text(rest, "MetaColumnGUID");
                if (colGuid == null || !columns.ColumnByGuid.TryGetValue(colGuid, out MetaColumn rcol)) continue;
                var metaSource = columns.Source;

                var restName = !string.IsNullOrEmpty(text(rest, "Name")) ? text(rest, "Name") : rcol.Name;
                var restOp = text(rest, "Operator") ?? "Equal";
                var enumEls = rest.Element("EnumValues") == null ? new List<XElement>() : rest.Element("EnumValues").Elements("string").ToList();
                var hasVal1 = rest.Element("Value1") != null && !string.IsNullOrEmpty(rest.Element("Value1").Value);
                var prompted = rest.Element("Prompt") != null && rest.Element("Prompt").Value.Trim() != "None";

                //Enumerated list of the restriction: the report list referenced by <EnumGUIDRE> wins over the column list of the data source
                MetaEnum menum = null;
                var isEnum = false;
                var enumRef = text(rest, "EnumGUIDRE") ?? "";
                if (isNoListValue(enumRef)) enumRef = "";  //the restriction then uses the list of the column (ReportRestriction.EnumRE)
                if (!string.IsNullOrEmpty(enumRef))
                {
                    if (reportEnumGuids.Contains(enumRef)) isEnum = true;
                    else
                    {
                        menum = metaSource.MetaData.Enums.FirstOrDefault(e => string.Equals(e.GUID, enumRef, StringComparison.OrdinalIgnoreCase));
                        if (menum == null)
                        {
                            restrictionErrors.Add($"'{restName}': <EnumGUIDRE> '{enumRef}' matches no enumerated list: define it under <ReportSource><MetaData><Enums>");
                            continue;
                        }
                        isEnum = true;
                    }
                }
                else if (!string.IsNullOrEmpty(rcol.EnumGUID))
                {
                    menum = metaSource.MetaData.Enums.FirstOrDefault(e => e.GUID == rcol.EnumGUID);
                    isEnum = menum != null;
                }

                if (!isEnum)
                {
                    //<EnumValues> on a column without enumerated list are ignored: the prompt is a text box and the filter is never applied
                    if (enumEls.Count > 0)
                        restrictionErrors.Add($"'{restName}': the column has no enumerated list, so <EnumValues> is ignored (the prompt stays a text box and the filter is not applied). For a drop-down list, define an enumerated list in the report (<ReportSource><MetaData><Enums>) and reference it with <EnumGUIDRE>; otherwise use <Value1>");
                    continue;
                }

                if (valueLessOperators.Contains(restOp)) continue;

                if (restOp != "Equal" && restOp != "NotEqual") { restrictionErrors.Add($"'{restName}': operator '{restOp}' is invalid on an enumerated column (only Equal or NotEqual are supported)"); continue; }
                if (hasVal1 && enumEls.Count == 0) { restrictionErrors.Add($"'{restName}': <Value1> is ignored on an enumerated column, list the values as <EnumValues><string>value</string></EnumValues>"); continue; }
                if (enumEls.Count == 0 && !prompted) { restrictionErrors.Add($"'{restName}': static (not prompted) restriction has no <EnumValues>, it would be skipped at execution"); continue; }

                //EnumValues must contain the enum value ids: map display labels back to their id, and warn about values unknown to the enumerated list
                //(dynamic lists may be stale, so an unknown value is not a hard error).
                if (enumEls.Count > 0 && menum != null && menum.Values.Count > 0)
                {
                    foreach (var vEl in enumEls)
                    {
                        var v = vEl.Value;
                        if (menum.Values.Any(ev => ev.Id == v)) continue;
                        var byLabel = menum.Values.FirstOrDefault(ev => (!string.IsNullOrEmpty(ev.Val) && ev.Val == v) || ev.DisplayValue == v);
                        if (byLabel != null)
                        {
                            log($"Restriction '{restName}': mapped enum label '{v}' -> id '{byLabel.Id}'");
                            vEl.Value = byLabel.Id;
                        }
                        else warnings.Add($"'{restName}': value '{v}' is not in the enumerated list '{menum.Name}' (the filter may match nothing). Verify the values with database_get_sample_values and fix the report if they are wrong");
                    }
                }
            }

            //A restriction defined in <Restrictions>/<AggregateRestrictions> but not referenced in the model's <Restriction>/<AggregateRestriction> expression is silently inactive.
            foreach (var model in doc.Descendants("ReportModel"))
            {
                foreach (var pair in new[] { new[] { "Restriction", "Restrictions" }, new[] { "AggregateRestriction", "AggregateRestrictions" } })
                {
                    var exprText = model.Element(pair[0]) == null ? "" : model.Element(pair[0]).Value;
                    var defs = model.Element(pair[1]) == null ? new List<XElement>() : model.Element(pair[1]).Elements("ReportRestriction").ToList();
                    foreach (var rest in defs)
                    {
                        var g = text(rest, "GUID");
                        if (string.IsNullOrEmpty(g) || exprText.Contains("[" + g + "]")) continue;
                        var n = text(rest, "Name") ?? g;
                        restrictionErrors.Add($"'{n}': defined in <{pair[1]}> but not referenced in the <{pair[0]}> expression, it would be inactive - add [{g}] to the expression or remove the restriction");
                    }
                }
            }

            if (restrictionErrors.Count > 0)
            {
                return "Error: invalid restriction definition(s): " + string.Join("; ", restrictionErrors) +
                    ". Enumerated columns accept only Equal/NotEqual with <EnumValues><string>value</string></EnumValues>" +
                    $" (a <Value1> or another operator is silently ignored at execution). Fix the XML and call {ToolName} again.";
            }

            //--- A Map view needs a latitude and a longitude column: set by the map_latitude/map_longitude parameters, or found from the element names.
            var mapErrors = new List<string>();
            foreach (var mapView in doc.Descendants("ReportView").Where(v => (string)v.Element("TemplateName") == "Map"))
            {
                string mapParam(string n) => mapView.Element("Parameters")?.Elements("Parameter").Where(p => (string)p.Element("Name") == n).Select(p => (string)p.Element("Value")).FirstOrDefault();
                var elementNames = doc.Descendants("ReportElement").SelectMany(e =>
                {
                    var colGuid = text(e, "MetaColumnGUID");
                    var columns = columnsOf(e);
                    return new[] { (string)e.Element("DisplayName"), (string)e.Element("Name"), colGuid != null && columns.ColumnByGuid.ContainsKey(colGuid) ? columns.ColumnByGuid[colGuid].DisplayName : null };
                }).Where(n => !string.IsNullOrEmpty(n)).ToList();
                foreach (var coord in new[] { "latitude", "longitude" })
                {
                    if (!string.IsNullOrEmpty(mapParam("map_" + coord))) continue;
                    if (!elementNames.Any(n => n.ToLowerInvariant().Contains(coord)))
                        mapErrors.Add($"the Map view needs a {coord} column: add it as a Row element (its name must contain '{coord}', or set the 'map_{coord}' parameter to its display name). If the source has no coordinates, do not add a Map view and tell the user that a map is not possible");
                }
            }
            if (mapErrors.Count > 0) return "Error: " + string.Join("; ", mapErrors.Distinct()) + ".";

            //--- Default record limit of the reports written by the AI agent: a model without a positive limit gets the default, unless the user explicitly asked for no limit.
            if (!noRecordLimit)
            {
                foreach (var model in doc.Descendants("ReportModel"))
                {
                    var maxEl = model.Element("MaxNumberOfRecords");
                    if (maxEl != null && int.TryParse(maxEl.Value.Trim(), out int maxVal) && maxVal > 0) continue;
                    if (maxEl != null) maxEl.Value = DefaultMaxNumberOfRecords.ToString();
                    else
                    {
                        var anchor = model.Element("Alias") ?? model.Element("SourceGUID") ?? model.Element("Name") ?? model.Element("GUID");
                        if (anchor != null) anchor.AddAfterSelf(new XElement("MaxNumberOfRecords", DefaultMaxNumberOfRecords));
                        else model.AddFirst(new XElement("MaxNumberOfRecords", DefaultMaxNumberOfRecords));
                    }
                    limitedModels++;
                }
            }

            //--- Colors and color mappings of the views saved in their canonical form (as checked by the code level)
            foreach (var parameter in doc.Descendants("ReportView").Elements("Parameters").Elements("Parameter").Concat(doc.Descendants("ReportOutput").Elements("ViewParameters").Elements("OutputParameter")))
            {
                var name = text(parameter, "Name") ?? "";
                var value = text(parameter, "Value");
                if (string.IsNullOrEmpty(value) || !(name.EndsWith("_colors") || name.EndsWith("_color_mappings"))) continue;
                if (ReportCodeGuard.CheckRawParameter(name, value, SecurityContext, out string canonical) == null && canonical != value) parameter.Element("Value").Value = canonical;
            }

            //--- Internal GUIDs
            RegenerateGuids(doc, regenerateAllGuids, inserted);

            //--- Internal references: a view pointing to no model (e.g. real GUIDs mixed with placeholders) loses its model and the report shows nothing
            var modelGuids = doc.Descendants("ReportModel").Select(m => text(m, "GUID")).Where(g => !string.IsNullOrEmpty(g)).ToList();
            var sourceGuids = doc.Descendants("ReportSource").Select(s => text(s, "GUID")).Where(g => !string.IsNullOrEmpty(g)).ToList();
            var refErrors = new List<string>();
            foreach (var view in doc.Descendants("ReportView").Where(v => !string.IsNullOrWhiteSpace(text(v, "ModelGUID"))))
                if (!modelGuids.Contains(text(view, "ModelGUID"))) refErrors.Add($"view '{(string)view.Element("Name")}' has a <ModelGUID> that is the <GUID> of no <ReportModel>");
            foreach (var model in doc.Descendants("ReportModel").Where(m => !string.IsNullOrWhiteSpace(text(m, "SourceGUID"))))
                if (!sourceGuids.Contains(text(model, "SourceGUID"))) refErrors.Add($"model '{(string)model.Element("Name")}' has a <SourceGUID> that is the <GUID> of no <ReportSource>");
            var viewGuids = doc.Descendants("ReportView").Select(v => text(v, "GUID")).Where(g => !string.IsNullOrEmpty(g)).ToList();
            var rootViewGuid = text(doc.Root, "ViewGUID");
            if (!string.IsNullOrEmpty(rootViewGuid) && !viewGuids.Contains(rootViewGuid)) refErrors.Add("the report <ViewGUID> is the <GUID> of no <ReportView>");
            if (refErrors.Count > 0)
            {
                return "Error: broken internal references: " + string.Join("; ", refErrors) +
                    $". Each <ModelGUID> must be the exact <GUID> of a <ReportModel> and each <SourceGUID> the <GUID> of a <ReportSource> of the same XML: use either the real GUIDs everywhere (as returned by report_get_xml) or short placeholders everywhere, never a mix. Fix the XML and call {ToolName} again.";
            }
            return null;
        }

        /// <summary>
        /// Enumerated lists of the report defined from a column of the data source (<c>&lt;MetaEnum&gt;&lt;FromColumnGUID&gt;</c>): the list becomes dynamic with the SQL
        /// of <see cref="MetaSource.GetEnumSqlFromColumn"/>, added to <paramref name="generatedSql"/> (accepted by the code level check as it is not written by the agent).
        /// Returns an error or null.
        /// </summary>
        public string ResolveEnumsFromColumns(XDocument doc, List<string> generatedSql)
        {
            var available = _tool != null ? _tool.GetAvailableSources().ToList() : Repository.Sources.ToList();
            foreach (var menum in doc.Descendants("ReportSource").Elements("MetaData").Elements("Enums").Elements("MetaEnum").Where(e => e.Element("FromColumnGUID") != null).ToList())
            {
                var enumName = text(menum, "Name") ?? "";
                var columnGuid = text(menum, "FromColumnGUID") ?? "";
                var reportSource = menum.Ancestors("ReportSource").First();
                var msGuid = text(reportSource, "MetaSourceGUID");
                var source = available.FirstOrDefault(s => string.Equals(s.GUID, msGuid, StringComparison.OrdinalIgnoreCase));
                if (source == null) return $"Error: enumerated list '{enumName}': MetaSourceGUID '{msGuid}' does not match any data source you can access. Use the GUID from datasource_list.";
                if (source.IsNoSQL) return $"Error: enumerated list '{enumName}': <FromColumnGUID> is not supported on the NoSQL source '{source.Name}', define a static list with <Values><MetaEV><Id>…</Id><Val>…</Val></MetaEV></Values> (values from database_get_sample_values).";
                var column = source.MetaData.Tables.SelectMany(t => t.Columns).FirstOrDefault(c => string.Equals(c.GUID, columnGuid, StringComparison.OrdinalIgnoreCase));
                if (column == null || column.MetaTable == null) return $"Error: enumerated list '{enumName}': <FromColumnGUID> '{columnGuid}' is not a column of source '{source.Name}'. Copy the exact column GUID from datasource_get_detail.";

                var sql = MetaSource.GetEnumSqlFromColumn(column);
                menum.Element("FromColumnGUID").Remove();
                menum.Elements("IsDynamic").Remove();
                menum.Elements("Sql").Remove();
                var anchor = menum.Element("Name") ?? menum.Element("GUID");
                var elements = new[] { new XElement("IsDynamic", "true"), new XElement("Sql", sql) };
                if (anchor != null) anchor.AddAfterSelf(elements);
                else menum.AddFirst(elements);
                generatedSql.Add(sql);
                log($"Enumerated list '{enumName}': values of the column '{column.Name}'");
            }
            return null;
        }

        static bool isGuid(string value) => Guid.TryParse(value, out _);

        /// <summary>
        /// Regenerates the internal GUIDs (the <c>&lt;GUID&gt;</c> elements) and their references (elements ending with GUID, items of a list ending with GUIDs,
        /// [GUID] tokens of the restriction expressions). The references to the data sources (MetaSourceGUID, the columns of a data source...) are not in the report and are kept.
        /// </summary>
        /// <param name="doc">The report XML</param>
        /// <param name="all">True to regenerate all the internal GUIDs, false to regenerate only the placeholders (values that are not GUIDs) and the GUIDs of the inserted elements already used in the report</param>
        /// <param name="inserted">Elements inserted by a patch</param>
        public static void RegenerateGuids(XDocument doc, bool all, List<XElement> inserted)
        {
            var guidMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var definitions = doc.Descendants("GUID").Where(e => !e.HasElements && e.Parent != null && !e.Parent.Name.LocalName.EndsWith("GUIDs")).ToList();
            foreach (var el in definitions)
            {
                var val = el.Value?.Trim();
                if (string.IsNullOrEmpty(val) || guidMap.ContainsKey(val)) continue;
                if (all || !isGuid(val)) guidMap[val] = Guid.NewGuid().ToString();
            }

            //Patch: a GUID of an inserted element (e.g. copied from a sibling) that is also defined outside the inserted element gets a new value, its references in the inserted element follow
            var localMaps = new List<(XElement, Dictionary<string, string>)>();
            if (!all && inserted != null)
            {
                foreach (var fragment in inserted.Where(i => i.Document == doc))
                {
                    var fragmentDefinitions = fragment.DescendantsAndSelf("GUID").Where(e => definitions.Contains(e)).ToList();
                    var localMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var el in fragmentDefinitions)
                    {
                        var val = el.Value?.Trim();
                        if (string.IsNullOrEmpty(val) || guidMap.ContainsKey(val) || localMap.ContainsKey(val)) continue;
                        if (definitions.Any(d => !fragmentDefinitions.Contains(d) && string.Equals(d.Value?.Trim(), val, StringComparison.OrdinalIgnoreCase))) localMap[val] = Guid.NewGuid().ToString();
                    }
                    if (localMap.Count > 0) localMaps.Add((fragment, localMap));
                }
            }
            if (guidMap.Count == 0 && localMaps.Count == 0) return;

            void remap(IEnumerable<XElement> elements, Dictionary<string, string> map)
            {
                foreach (var el in elements.ToList())
                {
                    if (el.HasElements) continue;
                    var name = el.Name.LocalName;
                    var val = el.Value?.Trim();
                    if (string.IsNullOrEmpty(val)) continue;
                    if (name == "Restriction" || name == "AggregateRestriction")
                    {
                        el.Value = Regex.Replace(el.Value, @"\[([^\]]+)\]", m => map.TryGetValue(m.Groups[1].Value, out string rep) ? "[" + rep + "]" : m.Value);
                        continue;
                    }
                    var isReference = name.EndsWith("GUID") || (el.Parent != null && el.Parent.Name.LocalName.EndsWith("GUIDs"));
                    //The sources are external, '1' is the connection of the data source
                    if (!isReference || name == "MetaSourceGUID" || (name == "ConnectionGUID" && val == ReportSource.DefaultRepositoryConnectionGUID)) continue;
                    if (map.TryGetValue(val, out string newValue)) el.Value = newValue;
                }
            }

            foreach (var (fragment, localMap) in localMaps) remap(fragment.DescendantsAndSelf(), localMap);
            if (guidMap.Count > 0) remap(doc.Descendants(), guidMap);
        }

        #endregion

        #region Write and test

        /// <summary>
        /// Checks the code level, validates, saves the report, then executes it and renders its views. Returns the result for the agent.
        /// </summary>
        string Write(string path, string fullPath, XDocument doc, string xml, string existingXml, bool regenerateAllGuids, List<XElement> inserted, bool noRecordLimit, bool renderPicture, string action)
        {
            try
            {
                //--- Enumerated lists built from a column (<FromColumnGUID>): their SQL is generated here, so it is accepted at any level
                var generatedSql = new List<string>();
                var enumError = ResolveEnumsFromColumns(doc, generatedSql);
                if (enumError != null) return enumError;
                if (generatedSql.Count > 0) xml = ToXmlText(doc);

                //--- Report code level (SQL, Razor scripts) of the user and the agent: checked before any save or SQL execution.
                //Content of the report already saved is kept as is, only new or modified content is checked
                if (_tool != null)
                {
                    var codeError = _tool.CheckReportCode(xml, existingXml, fullPath, generatedSql);
                    if (codeError != null) return codeError;
                }

                var warnings = new List<string>();
                var error = Validate(doc, existingXml, regenerateAllGuids, inserted, noRecordLimit, warnings, out int limitedModels);
                if (error != null) return error;

                //--- The report must deserialize (e.g. a label in an enumerated element): never save a file that does not load
                var text = ToXmlText(doc);
                error = CheckDeserialization(text);
                if (error != null) return error;

                var dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(fullPath, text, Encoding.UTF8);

                var result = new StringBuilder($"{action}: {path}");
                if (limitedModels > 0) result.Append($"\r\nNote: the result is limited to {DefaultMaxNumberOfRecords} records (default limit of the reports created by the AI agent, <MaxNumberOfRecords>). Mention it to the user.");
                if (warnings.Count > 0) result.Append("\r\nWarning: " + string.Join("; ", warnings) + ".");
                result.Append("\r\n" + TestReport(fullPath, renderPicture));
                result.Append("\r\nReport these test results to the user (row count; an error or a warning to fix first) and compare the row count with any figure already given in the conversation: explain a difference before concluding.");
                return result.ToString();
            }
            catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        /// <summary>
        /// Deserializes the report XML as Report.LoadFromFile does: returns the cause and the line in error, or null if the report can be read.
        /// </summary>
        public static string CheckDeserialization(string xml)
        {
            try
            {
                using var reader = new StringReader(xml);
                using var xr = XmlReader.Create(reader);
                new System.Xml.Serialization.XmlSerializer(typeof(Report)).Deserialize(xr);
                return null;
            }
            catch (Exception ex)
            {
                var cause = ex.InnerException ?? ex;
                while (cause.InnerException != null) cause = cause.InnerException;
                var line = "";
                var match = Regex.Match(ex.Message, @"\((\d+), (\d+)\)");
                if (match.Success)
                {
                    var lines = xml.Split('\n');
                    var index = int.Parse(match.Groups[1].Value) - 1;
                    if (index >= 0 && index < lines.Length) line = $" at '{lines[index].Trim()}'";
                }
                return $"Error: the report XML cannot be read{line}: {cause.Message} Check the type and the allowed values of the element with seal_describe_class. The report is not saved: fix the XML and save it again.";
            }
        }

        static void viewTree(StringBuilder sb, ReportView view, int level)
        {
            sb.Append($"\r\n{new string(' ', 2 * level)}- '{view.Name}' [{view.TemplateName}]" + (view.Model != null ? $" model '{view.Model.Name}'" : ""));
            foreach (var child in view.Views.OrderBy(i => i.SortOrder)) viewTree(sb, child, level + 1);
        }

        /// <summary>
        /// Chart views whose model has no Data element with a serie of the chart engine: such a chart renders empty without any error.
        /// </summary>
        public static List<string> EmptyCharts(Report report)
        {
            var result = new List<string>();
            foreach (var view in report.Views) emptyCharts(view, null, result);
            return result;
        }

        static void emptyCharts(ReportView view, ReportModel model, List<string> result)
        {
            if (view.TemplateName == ReportViewTemplate.ModelName && view.Model != null) model = view.Model;
            if (model != null)
            {
                var chart = chartEngine(view.TemplateName);
                if (chart != null && !model.Elements.Exists(i => i.PivotPosition == PivotPosition.Data && chart.Value.isSerie(i)))
                    result.Add($"'{view.Name}' [{view.TemplateName}] of model '{model.Name}' (no Data element with a <{chart.Value.serie}>)");
            }
            foreach (var child in view.Views) emptyCharts(child, model, result);
        }

        /// <summary>
        /// For a chart view template: the XML element of the serie of its engine, the test of this serie on an element and the parameter of the title of its secondary Y axis; null for another template
        /// </summary>
        static (string serie, Func<ReportElement, bool> isSerie, string y2Title)? chartEngine(string templateName)
        {
            if (templateName == ReportViewTemplate.ChartJSName) return ("ChartJSSerie", i => i.ChartJSSerie != ChartJSSerieDefinition.None, "chartjs_yaxis2_title");
            if (templateName == ReportViewTemplate.ChartEChartsName) return ("EChartsSerie", i => i.EChartsSerie != EChartsSerieDefinition.None, "echarts_yaxis2_title");
            if (templateName == ReportViewTemplate.ChartPlotlyName) return ("PlotlySerie", i => i.PlotlySerie != PlotlySerieDefinition.None, "plotly_yaxis2_title");
            if (templateName == ReportViewTemplate.ChartScottplotName) return ("ScottPlotSerie", i => i.ScottPlotSerie != ScottPlotSerieDefinition.None, "chartsp_yaxis2_label");
            return null;
        }

        /// <summary>
        /// Chart wiring errors that render without any error: a SerieDefinition on a Data element (ignored, only Row and Column elements make the X axis and the splitters),
        /// a chart whose model has dimensions but no Axis (only the first row is drawn), a secondary Y axis title without any serie on the secondary axis.
        /// </summary>
        public static List<string> ChartWiring(Report report)
        {
            var result = new List<string>();
            foreach (var model in report.Models)
            {
                foreach (var element in model.Elements.Where(i => i.PivotPosition == PivotPosition.Data && (i.SerieDefinition == SerieDefinition.Axis || i.SerieDefinition == SerieDefinition.Splitter || i.SerieDefinition == SerieDefinition.SplitterBoth)))
                    result.Add($"'{element.Name}' of model '{model.Name}' is a Data element: its <SerieDefinition>{element.SerieDefinition}</SerieDefinition> is ignored (Axis and Splitter apply only to Row or Column elements), remove it");
            }
            foreach (var view in report.Views) chartWiring(view, null, result);
            return result;
        }

        static void chartWiring(ReportView view, ReportModel model, List<string> result)
        {
            if (view.TemplateName == ReportViewTemplate.ModelName && view.Model != null) model = view.Model;
            var chart = chartEngine(view.TemplateName);
            if (model != null && chart != null)
            {
                var series = model.Elements.Where(i => i.PivotPosition == PivotPosition.Data && chart.Value.isSerie(i)).ToList();
                var dimensions = model.Elements.Where(i => i.PivotPosition == PivotPosition.Row || i.PivotPosition == PivotPosition.Column).ToList();
                var name = $"'{view.Name}' [{view.TemplateName}] of model '{model.Name}'";
                //Without Axis, the chart draws only the first row with the measure names as labels: wrong as soon as the model has dimensions
                if (series.Count > 0 && dimensions.Count > 0 && !dimensions.Exists(i => i.SerieDefinition == SerieDefinition.Axis))
                    result.Add($"{name} has no X axis: only the first row is drawn. Set <SerieDefinition>Axis</SerieDefinition> on the Row element of the axis (e.g. '{dimensions[0].Name}')");
                if (series.Count > 0 && !string.IsNullOrEmpty(view.GetValue(chart.Value.y2Title)) && !series.Exists(i => i.YAxisType == AxisType.Secondary))
                    result.Add($"{name} has a secondary Y axis title ({chart.Value.y2Title}) but no serie on it: set <YAxisType>Secondary</YAxisType> on the Data element(s) of the secondary axis");
            }
            foreach (var child in view.Views) chartWiring(child, model, result);
        }

        /// <summary>
        /// Executes a saved report with its default criteria values (as the SealReportRunner does), then renders its views in HTML: returns the row count and a sample of each model,
        /// the errors of the execution and of the views (file and line), and optionally a picture of the result for the agent.
        /// </summary>
        public string TestReport(string fullPath, bool renderPicture)
        {
            var test = new StringBuilder();
            string htmlPath = null;
            try
            {
                var testReport = Report.LoadFromFile(fullPath, Repository);
                if (!string.IsNullOrEmpty(testReport.LoadErrors))
                {
                    test.Append($"TEST FAILED: the report does not load: {testReport.LoadErrors}");
                    return test.ToString();
                }

                testReport.SkipExternalViewer = true;
                testReport.ExecutionContext = ReportExecutionContext.TaskScheduler;
                testReport.CurrentViewGUID = testReport.ViewGUID;
                var notDisplayed = testReport.Models.Where(m => m.Elements.Count > 0 && !testReport.ExecutionModels.Contains(m)).Select(m => m.Name).ToList();
                var emptyCriteria = testReport.ExecutionModels.SelectMany(m => m.Restrictions.Concat(m.AggregateRestrictions))
                    .Where(r => r.Prompt != PromptType.None && !r.HasValue).Select(r => r.DisplayNameEl).Distinct().ToList();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var execution = new ReportExecution() { Report = testReport };
                execution.Execute();
                var timedOut = false;
                while (testReport.Status != ReportStatus.Executed && !testReport.HasErrors && !testReport.Cancel)
                {
                    if (_tool?.CurrentToolCall?.CancelOperation?.Cancel == true) { testReport.Cancel = true; break; }
                    if (watch.Elapsed.TotalSeconds > TestTimeoutSeconds) { testReport.Cancel = true; timedOut = true; break; }
                    Thread.Sleep(200);
                }

                if (timedOut)
                    test.Append($"TEST WARNING: the execution was still running after {TestTimeoutSeconds} s and was cancelled: with its default criteria values the report is too broad (or too slow). Set default values that restrict the result, then save it again.");
                else if (testReport.HasErrors)
                    test.Append($"TEST FAILED: the report does not execute: {testReport.ExecutionErrors}\r\nThe file is saved but broken: fix it and save it again. Never present this report as working.");
                else if (!testReport.Cancel)
                {
                    test.Append($"Test execution with the default criteria values ({watch.Elapsed.TotalSeconds:0.0} s):");
                    var noRow = false;
                    foreach (var m in testReport.ExecutionModels)
                    {
                        var rows = m.ResultTable == null ? 0 : m.ResultTable.Rows.Count;
                        test.Append($"\r\n- model '{m.Name}': {rows} row(s)");
                        if (m.MaxNumberOfRecords > 0 && rows >= m.MaxNumberOfRecords) test.Append($" (the limit of {m.MaxNumberOfRecords} records is reached: the real result is larger)");
                        if (rows == 0) noRow = true;
                        else
                        {
                            //Sample only: drop the truncation note, which would invite to add restrictions
                            var sample = System.Text.Json.Nodes.JsonNode.Parse(m.GetResultTableTranslatedJson(3).Item1);
                            if (sample is System.Text.Json.Nodes.JsonObject sampleObj) { sampleObj.Remove("note"); sampleObj.Remove("row_count"); sampleObj.Remove("rows_shown"); }
                            test.Append(", first rows: " + sample?.ToJsonString());
                        }
                    }
                    if (noRow)
                        test.Append("\r\nTEST WARNING: no row with the default criteria values. Check the filter values (spelling, case, enumerated ids) with database_get_sample_values or database_execute_query; if no row is really the expected answer, tell the user.");
                    var executionWarnings = (testReport.ExecutionMessages ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Contains("Warning", StringComparison.OrdinalIgnoreCase)).Distinct().ToList();
                    if (executionWarnings.Count > 0) test.Append("\r\nTEST WARNING: " + string.Join(" ", executionWarnings));

                    //--- Rendering of the views (as the SealReportRunner): a view template or a view parameter in error fails here, with its file and line
                    htmlPath = FileHelper.GetUniqueFileName(Path.Combine(testReport.GenerationFolder, "ai_test.html"), "", true);
                    try
                    {
                        execution.GenerateHTMLResult(false, htmlPath);
                    }
                    catch (Exception ex)
                    {
                        testReport.ExecutionErrors += ex.Message;
                    }
                    if (testReport.HasErrors)
                    {
                        test.Append($"\r\nVIEW RENDER FAILED: the views do not render: {testReport.ExecutionErrors.Trim()}\r\nThe file is saved but broken: fix the view (template, parameters, structure) and save it again. Never present this report as working.");
                    }
                    else
                    {
                        test.Append($"\r\nViews rendered without error ({new FileInfo(htmlPath).Length / 1024} KB of HTML):");
                        if (testReport.ExecutionView != null) viewTree(test, testReport.ExecutionView, 0);
                        if (renderPicture) test.Append("\r\n" + takePicture(htmlPath));
                    }
                }
                if (notDisplayed.Count > 0)
                    test.Append($"\r\nTEST WARNING: model(s) {string.Join(", ", notDisplayed.Select(n => "'" + n + "'"))} are displayed by no view, so they are never executed.");
                var charts = EmptyCharts(testReport);
                if (charts.Count > 0)
                    test.Append($"\r\nTEST WARNING: empty chart(s), each chart view reads the serie of its own engine: {string.Join("; ", charts)}. Set the serie on the Data element(s) of the model, or remove the chart view.");
                var wiring = ChartWiring(testReport);
                if (wiring.Count > 0)
                    test.Append($"\r\nTEST WARNING: chart(s) wrongly built, rendered without error but not as described: {string.Join("; ", wiring)}. Fix the model and save it again; never describe these charts as working.");
                if (emptyCriteria.Count > 0)
                    test.Append($"\r\nNote: prompted criteria without default value (not applied until the user fills them): {string.Join(", ", emptyCriteria)}.");
            }
            catch (Exception ex) { test.Append($"TEST FAILED: {ex.Message}"); }
            finally
            {
                try { if (htmlPath != null && File.Exists(htmlPath)) File.Delete(htmlPath); } catch { }
            }
            return test.ToString();
        }

        /// <summary>
        /// Takes a picture of the HTML result with the headless browser and adds it to the result of the tool call when the provider of the agent accepts images.
        /// </summary>
        string takePicture(string htmlPath)
        {
            var toolCall = _tool?.CurrentToolCall;
            if (toolCall == null || !toolCall.AcceptsImages) return "Picture not taken: the AI provider of this agent does not accept images in a tool result, rely on the text result.";
            try
            {
                var picture = Task.Run(async () =>
                {
                    await using (var browser = await HeadlessBrowser.Open(Repository, htmlPath))
                    {
                        //Charts are animated
                        await Task.Delay(1000);
                        var width = await browser.Page.EvaluateExpressionAsync<int>("document.documentElement.scrollWidth");
                        var height = await browser.Page.EvaluateExpressionAsync<int>("document.documentElement.scrollHeight");
                        return await browser.Page.ScreenshotDataAsync(new ScreenshotOptions()
                        {
                            Type = ScreenshotType.Jpeg,
                            Quality = 75,
                            CaptureBeyondViewport = true,
                            Clip = new PuppeteerSharp.Media.Clip() { X = 0, Y = 0, Width = Math.Max(1, Math.Min(width, HeadlessBrowser.ViewportWidth)), Height = Math.Max(1, Math.Min(height, MaxPictureHeight)) }
                        });
                    }
                }).Result;
                toolCall.ResultImages.Add(new AIToolResultImage() { Data = picture, MediaType = "image/jpeg" });
                return $"A picture of the rendered report is attached ({picture.Length / 1024} KB, top {MaxPictureHeight} pixels at most).";
            }
            catch (Exception ex)
            {
                return $"Picture not taken: {(ex.InnerException ?? ex).Message}";
            }
        }

        #endregion
    }
}
