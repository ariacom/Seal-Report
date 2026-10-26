//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using Seal.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Seal.Helpers
{
    /// <summary>
    /// Checks the code (SQL, Razor scripts, JavaScript) written in a report XML against the <see cref="ReportCodeLevel"/> of the writer (web user, AI Agent).
    /// Each XML element is mapped to a property of the Seal.Model classes by reflection, the level required by a property or a class is given by <see cref="SealCodeAttribute"/>.
    /// Content above the level is refused only if it is new or modified: it is accepted if the same class and property have the same text in the report already saved.
    /// Removing content is always allowed, and the check applies only to the writing of a report (existing reports can always be executed).
    /// </summary>
    public static class ReportCodeGuard
    {
        class CodeItem
        {
            public string Key;
            public ReportCodeLevel Level;
            public string Message;
        }

        /// <summary>
        /// Returns the view template of a given name (null if unknown), used to know the view parameters rendered as code. By default, the templates of the repository.
        /// </summary>
        public static Func<string, ReportViewTemplate> ViewTemplateResolver = (name) =>
        {
            try { return RepositoryServer.ViewTemplates.FirstOrDefault(i => i.Name == name); }
            catch { return null; }
        };

        static Dictionary<string, Type> _types = null;
        static readonly object _typesLock = new object();
        static readonly Regex _restrictionTokenRegex = new Regex(@"\[[\w\-]+\]", RegexOptions.Compiled);
        static readonly Regex _restrictionMetadataRegex = new Regex(@"^(\s|\(|\)|\bAND\b|\bOR\b|\bNOT\b|&&|\|\||!)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //Plain value of a parameter rendered without encoding: no quote, parenthesis, brace, angle bracket, equal sign, ampersand or backslash, so it cannot leave its JavaScript, CSS or HTML context
        static readonly Regex _plainValueRegex = new Regex(@"^[\p{L}\p{N}\s.,:;%#+\-/_*$!?|^~]*$", RegexOptions.Compiled);
        //Active HTML content: scripts, frames, embedded objects, event handlers, script URLs
        static readonly Regex _activeHtmlRegex = new Regex(@"<\s*/?\s*(script|iframe|frame|frameset|object|embed|applet|link|meta|base|form|style|svg|math)\b|\bon[a-z]+\s*=|(javascript|vbscript|livescript)\s*:|data\s*:\s*text/html|\bsrcdoc\s*=|expression\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Checks a report XML.
        /// </summary>
        /// <param name="newXml">The report XML to write</param>
        /// <param name="existingXml">The XML of the report already saved (null or empty for a new report): its content is accepted as is</param>
        /// <param name="level">The level of the writer</param>
        /// <param name="trustedValues">Optional texts accepted for any property (e.g. the default scripts set by a tool)</param>
        /// <returns>The list of the content refused (empty if the XML is accepted), in English</returns>
        public static List<string> Check(string newXml, string existingXml, ReportCodeLevel level, IEnumerable<string> trustedValues = null)
        {
            var result = new List<string>();
            if (level >= ReportCodeLevel.Scripts || string.IsNullOrWhiteSpace(newXml)) return result;

            XDocument newDoc;
            try
            {
                newDoc = XDocument.Parse(newXml);
            }
            catch (Exception ex)
            {
                result.Add($"the report XML is invalid ({ex.Message})");
                return result;
            }

            var existingKeys = new HashSet<string>();
            if (!string.IsNullOrWhiteSpace(existingXml))
            {
                try
                {
                    var existingDoc = XDocument.Parse(existingXml);
                    foreach (var item in GetItems(existingDoc.Root)) existingKeys.Add(item.Key);
                }
                catch { /* an unreadable saved report gives no accepted content */ }
            }
            var trusted = new HashSet<string>((trustedValues ?? Enumerable.Empty<string>()).Where(i => !string.IsNullOrEmpty(i)).Select(Normalize));

            foreach (var item in GetItems(newDoc.Root))
            {
                if (item.Level <= level || existingKeys.Contains(item.Key)) continue;
                if (trusted.Contains(item.Key.Substring(item.Key.IndexOf('\n') + 1))) continue;
                if (!result.Contains(item.Message)) result.Add(item.Message);
            }
            return result;
        }

        /// <summary>
        /// Writes a refused report write in the audit (AuditType.ReportCodeRefused).
        /// </summary>
        public static void LogRefusal(SecurityUser user, string path, ReportCodeLevel level, IEnumerable<string> messages, string origin)
        {
            Audit.LogAudit(AuditType.ReportCodeRefused, user, path, $"{origin}: content above the '{level}' level: {string.Join("; ", messages)}");
        }

        /// <summary>
        /// Normalizes a text for the comparison: trim, CRLF to LF.
        /// </summary>
        public static string Normalize(string text)
        {
            return (text ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        }

        /// <summary>
        /// True if the HTML text contains active content (script, frame, embedded object, event handler, script URL...).
        /// </summary>
        public static bool HasActiveHtml(string text)
        {
            return !string.IsNullOrEmpty(text) && _activeHtmlRegex.IsMatch(text);
        }

        /// <summary>
        /// True if the value of a parameter rendered without encoding cannot leave its JavaScript, CSS or HTML context.
        /// </summary>
        public static bool IsPlainValue(string text)
        {
            return string.IsNullOrEmpty(text) || _plainValueRegex.IsMatch(text);
        }

        static Dictionary<string, Type> Types
        {
            get
            {
                lock (_typesLock)
                {
                    if (_types == null)
                    {
                        _types = new Dictionary<string, Type>();
                        foreach (var type in typeof(Report).Assembly.GetTypes().Where(i => i.IsClass && i.IsPublic && (i.Namespace == "Seal.Model" || i.Namespace == "Seal.Renderer")).OrderBy(i => i.Namespace == "Seal.Model" ? 0 : 1))
                        {
                            if (!_types.ContainsKey(type.Name)) _types.Add(type.Name, type);
                        }
                    }
                    return _types;
                }
            }
        }

        /// <summary>
        /// Type of an XML element: its xsi:type or its name if they designate a Seal class compatible with the expected type, otherwise the expected type.
        /// </summary>
        static Type ResolveType(XElement element, Type expected)
        {
            var xsiType = element.Attributes().FirstOrDefault(i => i.Name.LocalName == "type" && i.Name.NamespaceName.Contains("XMLSchema-instance"))?.Value;
            foreach (var name in new[] { xsiType, element.Name.LocalName })
            {
                if (!string.IsNullOrEmpty(name) && Types.TryGetValue(name, out Type type) && (expected == null || expected.IsAssignableFrom(type))) return type;
            }
            return expected;
        }

        /// <summary>
        /// Property declared by the type or its nearest base class (a property hidden with 'new', e.g. ReportRestriction.SQL, is found first).
        /// </summary>
        static PropertyInfo FindProperty(Type type, string name)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                var property = current.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (property != null) return property;
            }
            return null;
        }

        static SealCodeAttribute GetAttribute(PropertyInfo property, Type type)
        {
            var direct = property.GetCustomAttribute<SealCodeAttribute>(false);
            var attribute = direct ?? property.GetCustomAttribute<SealCodeAttribute>(true);
            if (attribute == null) return null;
            //Not applied to a derived class (e.g. MetaColumn.Name for a ReportElement)
            if (!attribute.ApplyToDerived && (direct == null || property.DeclaringType != type)) return null;
            return attribute;
        }

        static SealCodeAttribute GetAttribute(Type type)
        {
            var attribute = type.GetCustomAttribute<SealCodeAttribute>(true);
            if (attribute != null && !attribute.ApplyToDerived && type.GetCustomAttribute<SealCodeAttribute>(false) == null) return null;
            return attribute;
        }

        static Type GetItemType(Type type)
        {
            if (type == null || type == typeof(string)) return null;
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type)) return type.GetGenericArguments().FirstOrDefault();
            return null;
        }

        static string GetLabel(XElement element, Type type)
        {
            string kind;
            switch (type?.Name)
            {
                case nameof(Report): return "the report";
                case nameof(ReportElement): kind = "element"; break;
                case nameof(ReportRestriction): kind = "restriction"; break;
                case nameof(ReportModel): kind = "model"; break;
                case nameof(ReportView): kind = "view"; break;
                case nameof(ReportTask): kind = "task"; break;
                case nameof(ReportOutput): kind = "output"; break;
                case nameof(ReportSource): kind = "source"; break;
                case nameof(MetaEnum): kind = "enumerated list"; break;
                case nameof(MetaTable): kind = "table"; break;
                case nameof(MetaColumn): kind = "column"; break;
                case nameof(MetaJoin): kind = "join"; break;
                case nameof(MetaConnection): kind = "connection"; break;
                case nameof(Parameter): kind = "parameter"; break;
                case nameof(OutputParameter): kind = "parameter"; break;
                default: kind = type?.Name ?? element.Name.LocalName; break;
            }
            //Elements and restrictions: the display name or the column name, the element GUID only if there is none
            var name = new[] { "DisplayNameEl", "Name", "DisplayName", "Alias" }.Select(i => (string)element.Element(i)).FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
            if (!string.IsNullOrEmpty(name)) return $"{kind} '{name.Trim()}'";
            var guid = (string)element.Element("GUID");
            var article = "aeiou".Contains(char.ToLowerInvariant(kind[0])) ? "an" : "a";
            return string.IsNullOrWhiteSpace(guid) ? $"{article} {kind}" : $"{article} {kind} (GUID {guid.Trim()})";
        }

        /// <summary>
        /// Canonical text of an element (formatting ignored) used to compare a class marked by <see cref="SealCodeAttribute"/>.
        /// </summary>
        static void AppendCanonical(StringBuilder sb, XElement element)
        {
            sb.Append('<').Append(element.Name.LocalName).Append('>');
            if (element.HasElements) foreach (var child in element.Elements()) AppendCanonical(sb, child);
            else sb.Append(Normalize(element.Value));
            sb.Append("</").Append(element.Name.LocalName).Append('>');
        }

        static IEnumerable<CodeItem> GetItems(XElement root)
        {
            var items = new List<CodeItem>();
            if (root != null) Visit(root, ResolveType(root, null), items, ReportCodeLevel.Metadata);
            return items;
        }

        /// <param name="inheritedLevel">Level required by every text of the element (e.g. the parameters of a table template)</param>
        static void Visit(XElement element, Type type, List<CodeItem> items, ReportCodeLevel inheritedLevel)
        {
            var label = GetLabel(element, type);
            if (type != null)
            {
                var classAttribute = GetAttribute(type);
                if (classAttribute != null && classAttribute.Level > ReportCodeLevel.Metadata)
                {
                    var sb = new StringBuilder();
                    AppendCanonical(sb, element);
                    items.Add(new CodeItem() { Key = type.Name + "|\n" + sb.ToString(), Level = classAttribute.Level, Message = $"the {label} requires the {classAttribute.Level} level" });
                }
                if (typeof(ReportView).IsAssignableFrom(type)) VisitViewParameters(element, label, items, (string)element.Element("TemplateName"), "Parameters");
                else if (typeof(ReportOutput).IsAssignableFrom(type)) VisitViewParameters(element, label, items, null, "ViewParameters");
            }

            foreach (var child in element.Elements())
            {
                var name = child.Name.LocalName;
                var property = type != null ? FindProperty(type, name) : null;
                if (!child.HasElements)
                {
                    VisitText(child, type, property, label, items, inheritedLevel);
                    continue;
                }

                //A level set on a list or object property applies to all its texts
                var childLevel = inheritedLevel;
                var attribute = property != null ? GetAttribute(property, type) : null;
                if (attribute != null && attribute.Level > childLevel) childLevel = attribute.Level;

                var itemType = GetItemType(property?.PropertyType);
                if (itemType != null || property == null)
                {
                    //List: each child element is an item
                    foreach (var subChild in child.Elements())
                    {
                        if (subChild.HasElements) Visit(subChild, ResolveType(subChild, itemType), items, childLevel);
                        else VisitText(subChild, null, null, label, items, childLevel);
                    }
                }
                else Visit(child, ResolveType(child, property.PropertyType), items, childLevel);
            }
        }

        /// <summary>
        /// Parameters of a view (or the view parameters of an output) rendered without encoding by the view template: a value must be a value of the template list or a plain value, otherwise it is code (Scripts level).
        /// </summary>
        static void VisitViewParameters(XElement element, string label, List<CodeItem> items, string templateName, string listName)
        {
            var list = element.Element(listName);
            if (list == null) return;

            //For an output, the view is not known here: the parameters rendered as code by any view template
            var templates = new List<ReportViewTemplate>();
            if (!string.IsNullOrEmpty(templateName))
            {
                var template = ViewTemplateResolver?.Invoke(templateName);
                if (template != null) templates.Add(template);
            }
            else
            {
                try { templates.AddRange(RepositoryServer.ViewTemplates); } catch { }
            }
            if (templates.Count == 0) return;

            foreach (var parameter in list.Elements())
            {
                var name = (string)parameter.Element("Name");
                var value = Normalize((string)parameter.Element("Value"));
                if (string.IsNullOrEmpty(name) || IsPlainValue(value)) continue;
                var definitions = templates.Where(i => i.RawParameterNames.Contains(name)).Select(i => i.Parameters.FirstOrDefault(j => j.Name == name)).ToList();
                if (definitions.Count == 0) continue;
                //A value of the list proposed by the template is safe
                if (definitions.All(i => i != null && i.Enums != null && i.EnumValues.Contains(value))) continue;
                items.Add(new CodeItem()
                {
                    Key = "Parameter|" + name + "\n" + value,
                    Level = ReportCodeLevel.Scripts,
                    Message = $"the value of the parameter '{name}' of {label} is rendered as code (JavaScript, CSS or HTML) and requires the Scripts level: use a value of its list or a plain value (no quote, parenthesis, brace, angle bracket, equal sign, ampersand or backslash)"
                });
            }
        }

        static void VisitText(XElement element, Type type, PropertyInfo property, string label, List<CodeItem> items, ReportCodeLevel inheritedLevel)
        {
            var text = Normalize(element.Value);
            if (string.IsNullOrEmpty(text)) return;

            var className = type?.Name ?? "";
            var propertyName = property?.Name ?? element.Name.LocalName;
            var level = ReportCodeLevel.Metadata;
            string message = null;

            //The name of a parameter only identifies it, its value is the input
            if (inheritedLevel > level && !(type != null && typeof(Parameter).IsAssignableFrom(type) && propertyName == "Name"))
            {
                level = inheritedLevel;
                message = $"the {propertyName} of {label} requires the {level} level (input of a script executed on the server)";
            }

            var attribute = property != null ? GetAttribute(property, type) : null;
            if (attribute != null && attribute.Level > level)
            {
                level = attribute.Level;
                message = $"the {propertyName} of {label} requires the {level} level";
            }

            //HTML rendered as is: active content (script, frame, event handler...) is code
            if (attribute != null && attribute.Html && level < ReportCodeLevel.Scripts && HasActiveHtml(text))
            {
                level = ReportCodeLevel.Scripts;
                message = $"the {propertyName} of {label} contains active HTML (script, frame, embedded object, event handler or script URL) and requires the Scripts level";
            }

            if (type == typeof(ReportModel) && (propertyName == "Restriction" || propertyName == "AggregateRestriction"))
            {
                //Restriction expression: only [GUID], AND, OR, NOT and parentheses at the Metadata level; GUIDs are ignored for the comparison (they may be regenerated)
                if (!_restrictionMetadataRegex.IsMatch(_restrictionTokenRegex.Replace(text, " ")) && level < ReportCodeLevel.Sql)
                {
                    level = ReportCodeLevel.Sql;
                    message = $"the {propertyName} expression of {label} requires the Sql level (only restriction references [GUID], AND, OR, NOT and parentheses are allowed at the Metadata level)";
                }
                text = _restrictionTokenRegex.Replace(text, "[]");
            }

            //A text starting with '@' is compiled as a Razor script
            if (text.StartsWith("@") && level < ReportCodeLevel.Scripts)
            {
                level = ReportCodeLevel.Scripts;
                message = $"the {propertyName} of {label} starts with '@' (Razor script) and requires the Scripts level";
            }

            if (level == ReportCodeLevel.Metadata) return;
            items.Add(new CodeItem() { Key = className + "|" + propertyName + "\n" + text, Level = level, Message = message });
        }
    }
}
