//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Azure.Identity;
using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Data.SqlClient;
using MimeKit;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using RazorEngine.Templating;
using Seal.Model;

namespace Seal.Helpers
{
    /// <summary>
    /// Helper Objects
    /// </summary>
    internal class NamespaceDoc
    {
    }

    /// <summary>
    /// Static helper methods (strings, HTML, database connections, types, reflection, logging) that can be used in Razor scripts
    /// </summary>
    public partial class Helper
    {
        /// <summary>
        /// Returns the Description attribute text of an enum value, or the value name if no description is defined
        /// </summary>
        public static string GetEnumDescription(Type type, Object value)
        {
            FieldInfo fi = type.GetField(Enum.GetName(type, value));
            DescriptionAttribute dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
            if (dna != null) return dna.Description;
            else return value.ToString();
        }

        /// <summary>
        /// Returns the Description attribute text of an enum value, or the value name if no description is defined
        /// </summary>
        public static string GetEnumDescription(object value)
        {
            var type = value.GetType();
            FieldInfo fi = type.GetField(Enum.GetName(type, value));
            DescriptionAttribute dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
            if (dna != null) return dna.Description;
            else return value.ToString();
        }

        /// <summary>
        /// Returns the enum value having the given Description attribute text (or value name), or the default value if not found
        /// </summary>
        public static T GetEnumFromDescription<T>(string description, T defaultValue) where T : Enum
        {
            var type = typeof(T);
            var fields = type.GetFields();

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<DescriptionAttribute>();
                if (attribute != null && attribute.Description == description)
                {
                    return (T)field.GetValue(null);
                }
            }

            // If no matching description is found, try parse the enum value directly
            if (Enum.TryParse(typeof(T), description, true, out object result))
            {
                return (T)result;
            }
            return defaultValue;
        }


        /// <summary>
        /// Copy the public property values from a source object to a destination object of the same type. Properties with XmlIgnore or listed in skipNames are skipped.
        /// </summary>
        static public void CopyProperties(object src, object dest, string[] skipNames = null)
        {
            var xmlIgnore = new XmlIgnoreAttribute();
            foreach (PropertyDescriptor item in TypeDescriptor.GetProperties(src))
            {
                if (skipNames != null && skipNames.Contains(item.Name)) continue;
                if (item.Attributes.Contains(xmlIgnore)) continue;
                item.SetValue(dest, item.GetValue(src));
            }
        }

        /// <summary>
        /// Returns true if all writable property values of the two objects are identical (compared as strings)
        /// </summary>
        static public bool ArePropertiesIdentical(object obj1, object obj2, string skipEmptySuffix = "")
        {
            bool result = true;
            foreach (PropertyDescriptor item in TypeDescriptor.GetProperties(obj1))
            {
                if (item.IsReadOnly) continue;
                if (!string.IsNullOrEmpty(skipEmptySuffix) && string.IsNullOrEmpty(item.GetValue(obj1).ToString()) && item.Name.EndsWith(skipEmptySuffix)) continue;

                if (item.GetValue(obj1).ToString() != item.GetValue(obj2).ToString())
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Copy the properties having the same name from a source object to a destination object of a different type
        /// </summary>
        static public void CopyPropertiesDifferentObjects(object src, object dest)
        {
            var propSource = TypeDescriptor.GetProperties(src);
            var propDest = TypeDescriptor.GetProperties(dest);
            foreach (PropertyDescriptor itemDest in propDest)
            {
                var itemSource = propSource.Find(itemDest.Name, true);
                if (itemSource != null)
                {
                    try
                    {
                        itemDest.SetValue(dest, itemSource.GetValue(src));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(itemDest.Name + " " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Copy the property values of a reference object to a destination object for the properties still having their default value
        /// </summary>
        static public void CopyPropertiesFromReference(object defaultObject, object referenceObject, object destObject)
        {
            foreach (PropertyDescriptor item in TypeDescriptor.GetProperties(defaultObject))
            {
                if (item.GetValue(destObject) == item.GetValue(defaultObject)) item.SetValue(destObject, item.GetValue(referenceObject));
            }
        }


        /// <summary>
        /// Set a property value on an object by name from a string (enum, boolean, integer and string properties are supported)
        /// </summary>
        static public void SetPropertyValue(object item, string propertyName, string propertyValue)
        {
            PropertyInfo prop = item.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (null != prop && prop.CanWrite)
            {
                if (prop.PropertyType.IsEnum)
                {
                    var propertyVals = propertyValue.Split(' ');
                    prop.SetValue(item, Enum.Parse(prop.PropertyType, propertyVals[0]));
                }
                else if (prop.PropertyType.Name == "Boolean" && !string.IsNullOrWhiteSpace(propertyValue))
                {
                    prop.SetValue(item, bool.Parse(propertyValue));
                }
                else if (prop.PropertyType.Name == "Int32")
                {
                    prop.SetValue(item, int.Parse(propertyValue));
                }
                else
                {
                    prop.SetValue(item, propertyValue);
                }
            }
        }

        /// <summary>
        /// Returns the value of a public instance property of an object by name, or null if the property does not exist
        /// </summary>
        static public object GetPropertyValue(object item, string propertyName)
        {
            PropertyInfo prop = item.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (null != prop) return prop.GetValue(item, null);
            return null;
        }

        /// <summary>
        /// Returns the value of a public static property of a type by name, or null if the property does not exist
        /// </summary>
        static public object GetStaticPropertyValue(Type type, string propertyName)
        {
            PropertyInfo prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (null != prop) return prop.GetValue(null, null);
            return null;
        }

        /// <summary>
        /// Returns a new GUID as a string without dashes
        /// </summary>
        static public string NewGUID()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        /// <summary>
        /// Returns the input string enclosed in double quotes, doubling any embedded double quote
        /// </summary>
        static public string QuoteDouble(string input)
        {
            if (input == null) input = "";
            return string.Format("\"{0}\"", input.Replace("\"", "\"\""));
        }

        /// <summary>
        /// Returns the input string enclosed in single quotes, doubling any embedded single quote
        /// </summary>
        static public string QuoteSingle(string input)
        {
            if (input == null) input = "";
            return string.Format("'{0}'", input.Replace("'", "''"));
        }

        /// <summary>
        /// Removes all white-space characters from the input string
        /// </summary>
        static public string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        /// <summary>
        /// Returns the first string that is not null or empty, or an empty string if none
        /// </summary>
        static public string FirstNotEmpty(string str1, string str2 = null, string str3 = null, string str4 = null, string str5 = null)
        {
            if (!string.IsNullOrEmpty(str1)) return str1;
            if (!string.IsNullOrEmpty(str2)) return str2;
            if (!string.IsNullOrEmpty(str3)) return str3;
            if (!string.IsNullOrEmpty(str4)) return str4;
            if (!string.IsNullOrEmpty(str5)) return str5;
            return "";
        }

        /// <summary>
        /// Returns an HTML attribute string (name='value') if the value is not empty, otherwise an empty string
        /// </summary>
        static public string AddAttribute(string name, string value)
        {
            name = name.Replace("\r\n", "_").Replace("\r", "_").Replace("\n", "_");
            value = value.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Replace("'", "\"");
            if (!string.IsNullOrWhiteSpace(value)) return $"{name}='{value}'";
            return "";
        }

        /// <summary>
        /// Returns prefix + input + suffix if the input object is not null or empty, otherwise an empty string
        /// </summary>
        static public string AddIfNotNull(string prefix, object input, string suffix)
        {
            if (input != null && input.ToString() != "") return prefix + input.ToString() + suffix;
            return "";
        }


        /// <summary>
        /// Returns prefix + input + suffix if the input string is not empty, otherwise an empty string
        /// </summary>
        static public string AddIfNotEmpty(string prefix, string input, string suffix)
        {
            if (!string.IsNullOrEmpty(input)) return prefix + input + suffix;
            return "";
        }

        /// <summary>
        /// Returns separator + input if the input string is not empty, otherwise an empty string
        /// </summary>
        static public string AddNotEmpty(string separator, string input)
        {
            if (!string.IsNullOrEmpty(input)) return separator + input;
            return "";
        }

        /// <summary>
        /// Returns input + separator if the input string is not empty, otherwise an empty string
        /// </summary>
        static public string AddNotEmpty2(string input, string separator)
        {
            if (!string.IsNullOrEmpty(input)) return input + separator;
            return "";
        }

        /// <summary>
        /// Append a value to a string, adding the separator first if the string is not empty
        /// </summary>
        static public void AddValue(ref string input, string separator, string value)
        {
            if (!string.IsNullOrEmpty(input)) input += separator + value;
            else input = value;
        }

        /// <summary>
        /// Append a value to a StringBuilder, adding the separator first if the builder is not empty
        /// </summary>
        static public void AddValue(ref StringBuilder input, string separator, string value)
        {
            if (input.Length > 0) input.Append(separator + value);
            else input = new StringBuilder(value);
        }

        /// <summary>
        /// Compare two strings ignoring leading and trailing spaces (null and empty strings are considered equal)
        /// </summary>
        static public bool CompareTrim(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return true;
            if (s1 != null && s2 != null) return s1.Trim() == s2.Trim();
            return false;
        }

        /// <summary>
        /// Returns the list of non-empty lines contained in a string
        /// </summary>
        static public List<string> GetStringList(string listInput)
        {
            var result = new List<string>();
            if (!string.IsNullOrEmpty(listInput))
            {
                foreach (var input in listInput.Replace("\r\n", "§").Replace("\r", "§").Replace("\n", "§").Split('§'))
                {
                    if (!string.IsNullOrEmpty(input)) result.Add(input);
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if the input matches the pattern, where '*' is a wildcard
        /// </summary>
        static public bool IsMatchWildcard(string input, string pattern)
        {
            var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*") + "$";
            return Regex.IsMatch(input, regexPattern);
        }

        /// <summary>
        /// Array of string from a value having CR/LF
        /// </summary>
        static public string[] GetVals(string value, bool anyWords = false)
        {
            if (string.IsNullOrEmpty(value)) return new string[0];
            value = value.Trim();
            if (anyWords) value = value.Replace(" ", "\n");
            if (value.Contains("\n")) return value.Replace("\r", "").Split('\n');
            else return new string[] { value };
        }

        /// <summary>
        /// Try to parse a string as a double, swapping ',' and '.' decimal separators if the first parse fails
        /// </summary>
        static public bool ValidateNumeric(string value, out Double d)
        {
            bool result = false;
            if (Double.TryParse(value, out d))
            {
                result = true;
            }
            else
            {
                if (value.Contains(".")) value = value.Replace(".", ",");
                else if (value.Contains(",")) value = value.Replace(",", ".");
                if (Double.TryParse(value, out d)) result = true;
            }
            return result;
        }

        /// <summary>
        /// Concatenate the display values of result cells using the given separator
        /// </summary>
        static public string ConcatCellValues(ResultCell[] cells, string separator)
        {
            string result = "";
            foreach (var cell in cells) Helper.AddValue(ref result, separator, cell.DisplayValue);
            return result;
        }

        /// <summary>
        /// Returns the default value if the input string is null or empty, otherwise the input
        /// </summary>
        static public string IfNullOrEmpty(string value, string defaultValue)
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return value;
        }

        /// <summary>
        /// HTML-encode a string, converting new lines to br tags
        /// </summary>
        static public string ToHtml(string value)
        {
            if (value != null) return HttpUtility.HtmlEncode(value).Replace("\r\n", "<br>").Replace("\n", "<br>");
            return "";
        }

        //Like ToHtml but for single-line contexts (e.g. a <select> restriction option label): newlines are
        //collapsed to a space instead of <br>. A <br> injected inside an <option> breaks bootstrap-select's
        //menu build and blanks the whole dropdown, so a stray CR/LF in an enum value must never reach it.
        /// <summary>
        /// HTML-encode a string for single-line contexts: new lines are converted to spaces instead of br tags
        /// </summary>
        static public string ToHtmlNoBr(string value)
        {
            if (value != null) return HttpUtility.HtmlEncode(value).Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
            return "";
        }

        /// <summary>
        /// Convert a boolean to its JavaScript literal ('true' or 'false')
        /// </summary>
        static public string ToJS(bool value)
        {
            return value.ToString().ToLower();
        }

        /// <summary>
        /// Encode a string to be used in JavaScript
        /// </summary>
        static public string ToJS(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return HttpUtility.JavaScriptStringEncode(value);
        }

        /*
        static public string ToMomentJSFormat(CultureInfo culture, string datetimeFormat)
        {
            string format = datetimeFormat;
            if (datetimeFormat == "d") format = culture.DateTimeFormat.ShortDatePattern;
            else if (datetimeFormat == "D") format = culture.DateTimeFormat.LongDatePattern;
            else if (datetimeFormat == "t") format = culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "T") format = culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "g") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "G") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "f") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "F") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;

            return format.Replace("y", "Y").Replace("d", "D").Replace("tt", "A").Replace("z", "Z").Replace("/", culture.DateTimeFormat.DateSeparator);
        }

        static public string ToDateFnsFormat(CultureInfo culture, string datetimeFormat)
        {
            string format = datetimeFormat;
            if (datetimeFormat == "d") format = culture.DateTimeFormat.ShortDatePattern;
            else if (datetimeFormat == "D") format = culture.DateTimeFormat.LongDatePattern;
            else if (datetimeFormat == "t") format = culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "T") format = culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "g") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "G") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "f") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "F") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;

            return format.Replace("y", "Y").Replace("d", "D").Replace("tt", "A").Replace("z", "Z").Replace("/", culture.DateTimeFormat.DateSeparator);
        }
        */

        /// <summary>
        /// Converts a .NET date/time format pattern to a Moment.js format string
        /// </summary>
        public static string ToMomentJSFormat(CultureInfo culture, string datetimeFormat)
        {
            var tokenMap = new (string CSharp, string Moment)[]
            {
        ("dddd", "dddd"),   // full weekday name — same token
        ("ddd",  "ddd"),    // short weekday name — same token
        ("dd",   "DD"),     // day 2 digits
        ("d",    "D"),      // day 1-2 digits
        ("yyyy", "YYYY"),   // year 4 digits
        ("yyy",  "YYYY"),   // year 4 digits (fallback)
        ("yy",   "YY"),     // year 2 digits
        ("y",    "YYYY"),   // year minimal -> 4 digits
        ("fff",  "SSS"),    // milliseconds 3 digits
        ("ff",   "SS"),     // milliseconds 2 digits
        ("f",    "S"),      // milliseconds 1 digit
        ("tt",   "A"),      // AM/PM
        ("t",    "A"),      // A/P -> AM/PM
        ("zzz",  "Z"),      // timezone +01:00
        ("zz",   "ZZ"),     // timezone +0100
        ("z",    "ZZ"),     // timezone offset
        ("K",    "Z"),      // timezone info
        ("g",    ""),       // era — no Moment equivalent
            };

            return ConvertFormat(culture, datetimeFormat, tokenMap);
        }

        /// <summary>
        /// Converts a .NET date/time format pattern to a Flatpickr format string.
        /// Flatpickr uses its own single-letter tokens (e.g. minutes = 'i', month = 'm'/'n', AM/PM = 'K'),
        /// so every token is mapped explicitly. Milliseconds/timezone/era have no Flatpickr token and are dropped.
        /// </summary>
        public static string ToFlatpickrFormat(CultureInfo culture, string datetimeFormat)
        {
            var tokenMap = new (string CSharp, string Flatpickr)[]
            {
        ("dddd", "l"),      // full weekday name
        ("ddd",  "D"),      // short weekday name
        ("dd",   "d"),      // day 2 digits
        ("d",    "j"),      // day 1-2 digits
        ("yyyy", "Y"),      // year 4 digits
        ("yyy",  "Y"),      // year 4 digits (fallback)
        ("yy",   "y"),      // year 2 digits
        ("y",    "Y"),      // year minimal -> 4 digits
        ("MMMM", "F"),      // full month name
        ("MMM",  "M"),      // short month name
        ("MM",   "m"),      // month 2 digits
        ("M",    "n"),      // month 1-2 digits
        ("HH",   "H"),      // hour 24
        ("H",    "H"),      // hour 24 (Flatpickr has no no-leading-zero token)
        ("hh",   "h"),      // hour 12
        ("h",    "h"),      // hour 12
        ("mm",   "i"),      // minutes
        ("m",    "i"),      // minutes
        ("ss",   "S"),      // seconds 2 digits
        ("s",    "s"),      // seconds
        ("fff",  ""),       // milliseconds — no Flatpickr token
        ("ff",   ""),       // milliseconds — no Flatpickr token
        ("f",    ""),       // milliseconds — no Flatpickr token
        ("tt",   "K"),      // AM/PM
        ("t",    "K"),      // AM/PM
        ("zzz",  ""),       // timezone — no Flatpickr token
        ("zz",   ""),       // timezone — no Flatpickr token
        ("z",    ""),       // timezone — no Flatpickr token
        ("K",    ""),       // timezone — no Flatpickr token
        ("g",    ""),       // era — no Flatpickr token
            };

            return ConvertFormat(culture, datetimeFormat, tokenMap);
        }

        /// <summary>
        /// Converts a .NET date/time format pattern to a date-fns format string
        /// </summary>
        public static string ToDateFnsFormat(CultureInfo culture, string datetimeFormat)
        {
            string format = datetimeFormat;
            if (datetimeFormat == "d") format = culture.DateTimeFormat.ShortDatePattern;
            else if (datetimeFormat == "D") format = culture.DateTimeFormat.LongDatePattern;
            else if (datetimeFormat == "t") format = culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "T") format = culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "g") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "G") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "f") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "F") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;

            var tokenMap = new (string CSharp, string DateFns)[]
            {
        ("dddd", "EEEE"),   // full weekday name
        ("ddd",  "EEE"),    // short weekday name
        ("yyyy", "yyyy"),   // needed to block single 'y' match
        ("yyy",  "yyyy"),   // year 4 digits (fallback)
        ("fff",  "SSS"),    // milliseconds 3 digits
        ("ff",   "SS"),     // milliseconds 2 digits
        ("f",    "S"),      // milliseconds 1 digit
        ("tt",   "aa"),     // AM/PM
        ("t",    "aa"),     // A/P -> AM/PM
        ("zzz",  "xxx"),    // timezone +01:00
        ("zz",   "xx"),     // timezone +0100
        ("z",    "xx"),     // timezone offset
        ("K",    "xxx"),    // timezone info
        ("y",    "yyyy"),   // year minimal -> 4 digits
        ("g",    ""),       // era — no date-fns equivalent
            };

            return ConvertFormat(culture, datetimeFormat, tokenMap);
        }

        private static string ConvertFormat(CultureInfo culture, string datetimeFormat, (string From, string To)[] tokenMap)
        {
            string format = datetimeFormat;
            if (datetimeFormat == "d") format = culture.DateTimeFormat.ShortDatePattern;
            else if (datetimeFormat == "D") format = culture.DateTimeFormat.LongDatePattern;
            else if (datetimeFormat == "t") format = culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "T") format = culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "g") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "G") format = culture.DateTimeFormat.ShortDatePattern + " " + culture.DateTimeFormat.LongTimePattern;
            else if (datetimeFormat == "f") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.ShortTimePattern;
            else if (datetimeFormat == "F") format = culture.DateTimeFormat.LongDatePattern + " " + culture.DateTimeFormat.LongTimePattern;

            var result = new StringBuilder();
            int i = 0;

            while (i < format.Length)
            {
                // Quoted literal strings: 'text' passes through as-is
                if (format[i] == '\'')
                {
                    i++;
                    while (i < format.Length && format[i] != '\'')
                    {
                        result.Append(format[i]);
                        i++;
                    }
                    if (i < format.Length) i++; // consume closing '
                    continue;
                }

                // Date separator
                if (format[i] == '/')
                {
                    result.Append(culture.DateTimeFormat.DateSeparator);
                    i++;
                    continue;
                }

                // Time separator
                if (format[i] == ':')
                {
                    result.Append(culture.DateTimeFormat.TimeSeparator);
                    i++;
                    continue;
                }

                bool matched = false;
                foreach (var (from, to) in tokenMap)
                {
                    if (format.AsSpan(i).StartsWith(from.AsSpan()))
                    {
                        result.Append(to);
                        i += from.Length;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    result.Append(format[i]);
                    i++;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns true if the two dates differ by less than one second
        /// </summary>
        static public bool AreEqualToSecond(DateTime dt1, DateTime dt2)
        {
            return Math.Abs((dt1 - dt2).TotalSeconds) < 1;
        }


        /// <summary>
        /// Removes HTML tags and non-breaking space entities from a string
        /// </summary>
        static public string RemoveHTMLTags(string value)
        {
            return Regex.Replace(value, @"<[^>]+>|&nbsp;", "").Trim();
        }

        /// <summary>
        /// Returns a name not present in the given list, appending a number to the name if necessary
        /// </summary>
        static public string GetUniqueName(string name, List<string> entities)
        {
            string result;
            int i = 1;
            while (true)
            {
                result = name;
                if (i != 1) result += i.ToString();
                if (!entities.Contains(result)) break;
                i++;
            }
            return result;
        }
        /// <summary>
        /// Returns a name not present in the given list (case insensitive comparison), appending a number to the name if necessary
        /// </summary>
        static public string GetUniqueNameCaseInsensitive(string name, List<string> entities)
        {
            string result;
            int i = 1;
            while (true)
            {
                result = name;
                if (i != 1) result += i.ToString();
                if (!entities.Exists(i => i.ToLower() == result.ToLower())) break;
                i++;
            }
            return result;
        }


        /// <summary>
        /// Convert a database object name to a display name (split camel case, replace underscores with spaces, capitalize the first letter)
        /// </summary>
        static public string DBNameToDisplayName(string name)
        {
            string result = name;
            result = string.Join(" ", Regex.Split(result, @"([A-Z][a-z]+)"));
            result = result.Replace('_', ' ').Trim();
            result = result.Replace("  ", " ");
            result = result.Replace("  ", " ");
            result = result.Replace("  ", " ");
            if (result.Length > 0) result = result.Substring(0, 1).ToUpper() + result.Substring(1);
            return result.Trim();
        }


        /// <summary>
        /// Convert a database column type value (OLE DB type number or type name) to a ColumnType
        /// </summary>
        static public ColumnType DatabaseToNetTypeConverter(object dbValue)
        {
            ColumnType result = ColumnType.Text;
            try
            {
                int intValue;
                if (Int32.TryParse(dbValue.ToString(), out intValue))
                {
                    OleDbType columnType = (OleDbType)intValue;
                    result = Helper.NetTypeConverter(Helper.OleDbToNetTypeConverter(columnType));
                    if (columnType == OleDbType.WChar || columnType == OleDbType.VarWChar || columnType == OleDbType.LongVarWChar) result = ColumnType.UnicodeText;
                }
                else
                {
                    var dbValueString = dbValue.ToString().ToLower();
                    if (dbValueString.Contains("number") || dbValueString.Contains("double") || dbValueString.Contains("numeric")) result = ColumnType.Numeric;
                    else if (dbValueString.Contains("date")) result = ColumnType.DateTime;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Convert an ODBC type name to a ColumnType, returning Text on error
        /// </summary>
        static public ColumnType ODBCToNetTypeConverter(string odbcType)
        {
            ColumnType result = ColumnType.Text;
            try
            {
                result = Helper.OdbcTypeConverter(odbcType);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Convert an ODBC type name (e.g. 'varchar', 'datetime') to a ColumnType
        /// </summary>
        static public ColumnType OdbcTypeConverter(string dbType)
        {
            string t = dbType.ToLower();

            if (t == "char" || t == "varchar" || t == "varchar2" || t == "text" || t == "uniqueidentifier") return ColumnType.Text;
            if (t == "nchar" || t == "ntext" || t == "nvarchar") return ColumnType.UnicodeText;
            if (t == "date" || t == "datetime" || t == "datetime2" || t == "smalldatetime" || t == "timestamp") return ColumnType.DateTime;
            return ColumnType.Numeric;
        }

        /// <summary>
        /// Convert an OleDbType to the corresponding .NET Type
        /// </summary>
        static public Type OleDbToNetTypeConverter(OleDbType oleDbTypeNumber)
        {
            switch ((int)oleDbTypeNumber)
            {
                case 0: return typeof(Nullable);
                case 2: return typeof(Int16);
                case 3: return typeof(Int32);
                case 4: return typeof(Single);
                case 5: return typeof(Double);
                case 6: return typeof(Decimal);
                case 7: return typeof(DateTime);
                case 8: return typeof(String);
                case 9: return typeof(Object);
                case 10: return typeof(Exception);
                case 11: return typeof(Boolean);
                case 12: return typeof(Object);
                case 13: return typeof(Object);
                case 14: return typeof(Decimal);
                case 16: return typeof(SByte);
                case 17: return typeof(Byte);
                case 18: return typeof(UInt16);
                case 19: return typeof(UInt32);
                case 20: return typeof(Int64);
                case 21: return typeof(UInt64);
                case 64: return typeof(DateTime);
                case 72: return typeof(Guid);
                case 128: return typeof(Byte[]);
                case 129: return typeof(String);
                case 130: return typeof(String);
                case 131: return typeof(Decimal);
                case 133: return typeof(DateTime);
                case 134: return typeof(TimeSpan);
                case 135: return typeof(DateTime);
                case 138: return typeof(Object);
                case 139: return typeof(Decimal);
                case 200: return typeof(String);
                case 201: return typeof(String);
                case 202: return typeof(String);
                case 203: return typeof(String);
                case 204: return typeof(Byte[]);
                case 205: return typeof(Byte[]);
            }
            throw (new Exception("DataType Not Supported"));
        }

        /// <summary>
        /// Convert a .NET type to a ColumnType (Text, DateTime or Numeric)
        /// </summary>
        static public ColumnType NetTypeConverter(Type netType)
        {
            if (netType == typeof(string) || netType == typeof(Guid)) return ColumnType.Text;
            if (netType == typeof(DateTime)) return ColumnType.DateTime;
            return ColumnType.Numeric;
        }

        /// <summary>
        /// Returns the list of ODBC driver names installed on the machine (read from the registry)
        /// </summary>
        public static List<String> GetSystemDriverList()
        {
            List<string> names = new List<string>();
            // get system dsn's
            Microsoft.Win32.RegistryKey reg = (Microsoft.Win32.Registry.LocalMachine).OpenSubKey("Software");
            if (reg != null)
            {
                reg = reg.OpenSubKey("ODBC");
                if (reg != null)
                {
                    reg = reg.OpenSubKey("ODBCINST.INI");
                    if (reg != null)
                    {

                        reg = reg.OpenSubKey("ODBC Drivers");
                        if (reg != null)
                        {
                            // Get all DSN entries defined in DSN_LOC_IN_REGISTRY.
                            foreach (string sName in reg.GetValueNames())
                            {
                                names.Add(sName);
                            }
                        }
                        try
                        {
                            reg.Close();
                        }
                        catch { /* ignore this exception if we couldn't close */ }
                    }
                }
            }

            return names;
        }
        /// <summary>
        /// Convert a NumericStandardFormat to its .NET format string (e.g. 'N2')
        /// </summary>
        static public string ConvertNumericStandardFormat(NumericStandardFormat format)
        {
            if (format == NumericStandardFormat.Numeric0) return "N0";
            else if (format == NumericStandardFormat.Numeric1) return "N1";
            else if (format == NumericStandardFormat.Numeric2) return "N2";
            else if (format == NumericStandardFormat.Numeric3) return "N3";
            else if (format == NumericStandardFormat.Numeric4) return "N4";
            else if (format == NumericStandardFormat.Percentage0) return "P0";
            else if (format == NumericStandardFormat.Percentage1) return "P1";
            else if (format == NumericStandardFormat.Percentage2) return "P2";
            else if (format == NumericStandardFormat.Currency0) return "C0";
            else if (format == NumericStandardFormat.Currency2) return "C2";
            else if (format == NumericStandardFormat.Decimal) return "D";
            else if (format == NumericStandardFormat.Decimal0) return "D0";
            else if (format == NumericStandardFormat.Decimal1) return "D1";
            else if (format == NumericStandardFormat.Decimal2) return "D2";
            else if (format == NumericStandardFormat.Decimal3) return "D3";
            else if (format == NumericStandardFormat.Decimal4) return "D4";
            else if (format == NumericStandardFormat.Exponential) return "E";
            else if (format == NumericStandardFormat.Exponential2) return "E2";
            else if (format == NumericStandardFormat.Fixedpoint) return "F";
            else if (format == NumericStandardFormat.Fixedpoint0) return "F0";
            else if (format == NumericStandardFormat.Fixedpoint2) return "F2";
            else if (format == NumericStandardFormat.General) return "G";
            else if (format == NumericStandardFormat.General2) return "G2";
            else if (format == NumericStandardFormat.General5) return "G5";
            else if (format == NumericStandardFormat.Hexadecimal) return "H";
            else if (format == NumericStandardFormat.Hexadecimal8) return "H8";

            return "0";
        }

        /// <summary>
        /// Convert a DateTimeStandardFormat to its .NET format string (e.g. 'd')
        /// </summary>
        static public string ConvertDateTimeStandardFormat(DateTimeStandardFormat format)
        {
            if (format == DateTimeStandardFormat.ShortDate) return "d";
            else if (format == DateTimeStandardFormat.LongDate) return "D";
            else if (format == DateTimeStandardFormat.ShortTime) return "t";
            else if (format == DateTimeStandardFormat.LongTime) return "T";
            else if (format == DateTimeStandardFormat.ShortDateTime) return "g";
            else if (format == DateTimeStandardFormat.LongDateTime) return "G";
            else if (format == DateTimeStandardFormat.FullShortDateTime) return "f";
            else if (format == DateTimeStandardFormat.FullLongDateTime) return "F";
            return "0";
        }

        /// <summary>
        /// Build a readable error message from a Razor template compilation exception
        /// </summary>
        static public string GetExceptionMessage(TemplateCompilationException ex)
        {
            var result = new StringBuilder("");
            var firstError = "";
            foreach (var err in ex.CompilerErrors)
            {
                if (string.IsNullOrEmpty(firstError) && err.Line > 0) firstError = err.ErrorText + "\r\n\r\n";
                result.AppendFormat("{0}\r\nLine {1} Column {2} Error Number {3}\r\n", err.ErrorText, err.Line, err.Column, err.ErrorNumber);
            }
            return firstError + result.ToString();
        }

        /// <summary>
        /// Parse a SQL statement through the Razor engine and execute it on the connection (used for Pre and Post SQL statements)
        /// </summary>
        static public void ExecutePrePostSQL(DbConnection connection, string sql, object model, bool ignoreErrors)
        {
            try
            {
                if (!string.IsNullOrEmpty(sql))
                {
                    string finalSql = RazorHelper.CompileExecute(sql, model);
                    if (!string.IsNullOrEmpty(sql))
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = finalSql;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                if (!ignoreErrors) throw;
            }
        }


        /// <summary>
        /// Prefix a message with the current date and time
        /// </summary>
        public static string FormatMessage(string message)
        {
            return string.Format("[{0} {1}] {2} ", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), message);
        }

        /// <summary>
        /// Remove invalid file name and path characters from a string
        /// </summary>
        public static string CleanFileName(string s)
        {
            foreach (char character in Path.GetInvalidFileNameChars())
            {
                s = s.Replace(character.ToString(), string.Empty);
            }

            foreach (char character in Path.GetInvalidPathChars())
            {
                s = s.Replace(character.ToString(), string.Empty);
            }

            return s;
        }

        static DateTime NextDailyPuge = DateTime.MinValue;
        const string TaskSchedulerEntry = "Seal Task Scheduler";
        /// <summary>
        /// Prefix of the daily log files for report executions
        /// </summary>
        public const string DailyLogExecutions = "executions";
        /// <summary>
        /// Prefix of the daily log files for events
        /// </summary>
        public const string DailyLogEvents = "events";
        /// <summary>
        /// Prefix of the daily log files for schedules
        /// </summary>
        public const string DailyLogSchedules = "schedules";
        /// <summary>
        /// Append a message to a daily log file in the logs folder, purging log files older than logDays days
        /// </summary>
        public static void WriteDailyLog(string prefix, string logsFolder, int logDays, string message)
        {
            try
            {
                if (logDays <= 0) return;

                if (!Directory.Exists(logsFolder)) Directory.CreateDirectory(logsFolder);

                lock (TaskSchedulerEntry)
                {

                    string logFileName = Path.Combine(logsFolder, string.Format("{0}_{1:yyyy_MM_dd}.txt", prefix, DateTime.Now));
                    File.AppendAllText(logFileName, message);

                    if (NextDailyPuge < DateTime.Now)
                    {
                        NextDailyPuge = DateTime.Now.AddDays(1);
                        foreach (var file in Directory.GetFiles(logsFolder, "*"))
                        {
                            //purge old files...
                            if (File.GetLastWriteTime(file).AddDays(logDays) < DateTime.Now)
                            {
                                try
                                {
                                    File.Delete(file);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(message + "\r\n" + ex.Message);
                                }
                                ;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(message + "\r\n" + ex.Message);
            }
        }

        /// <summary>
        /// Log an exception (message and stack trace) to the daily events log file and to the Windows Event Log
        /// </summary>
        public static void WriteLogException(string context, Exception ex)
        {
            try
            {
                var msg = $"Exception got in {context}\r\n{ex.Message}\r\n{ex.StackTrace}";
                var fullMessage = string.Format("**********\r\n{0} Exception\r\n{1}\r\n\r\n", DateTime.Now, msg);
                if (Repository.IsInstanceCreated)
                {
                    WriteDailyLog(DailyLogEvents, Repository.Instance.LogsFolder, Repository.Instance.Configuration.LogDays, fullMessage);
                    if (ex.InnerException != null) WriteDailyLog(DailyLogEvents, Repository.Instance.LogsFolder, Repository.Instance.Configuration.LogDays, $"Inner Exception:\r\n{ex.InnerException.Message}\r\n{ex.InnerException.StackTrace}");
                }
                else
                {
                    var path = Repository.FindRepository();
                    var pathLogs = Path.Combine(path, "Logs");

                    if (Directory.Exists(pathLogs)) WriteDailyLog(DailyLogEvents, pathLogs, 30, fullMessage);
                    else if (Directory.Exists(path)) WriteDailyLog(DailyLogEvents, path, 30, fullMessage);
                    else WriteDailyLog(DailyLogEvents, FileHelper.TempApplicationDirectory, 30, fullMessage);
                }

                if (ex.Message.Length > 32000) msg = ex.Message.Substring(32000);
                else msg = ex.Message;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) EventLog.WriteEntry("Seal Report", $"{context}\r\n{msg}", EventLogEntryType.Error);
            }
            catch { }

            Console.WriteLine(ex.Message);
        }

        /// <summary>
        /// Write a message to the daily log file and to the Windows Event Log
        /// </summary>
        public static void WriteLogEntry(string source, EventLogEntryType type, string message)
        {
            string msg = message;
            try
            {
                Console.WriteLine(msg);

                var fullMessage = string.Format("**********\r\n{0} {1}\r\n{2}\r\n\r\n", DateTime.Now, type.ToString(), msg);
                WriteDailyLog(source == TaskSchedulerEntry ? DailyLogSchedules : DailyLogEvents, Repository.Instance.LogsFolder, Repository.Instance.Configuration.LogDays, fullMessage);

                if (msg.Length > 25000)
                {
                    msg = msg.Substring(0, 25000) + "\r\n...\r\nMessage truncated, check the event log files in the Logs Repository sub-folder.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) EventLog.WriteEntry(source, ex.Message, EventLogEntryType.Error);
                }
                catch { }
            }
            finally
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) EventLog.WriteEntry(source, msg, type);
                }
                catch { }
            }
        }

        /// <summary>
        /// Write a message to the scheduler daily log file and to the Windows Event Log
        /// </summary>
        public static void WriteLogEntryScheduler(EventLogEntryType type, string message)
        {
            WriteLogEntry(TaskSchedulerEntry, type, message);
        }
        /// <summary>
        /// Write an exception message to the Windows Event Log
        /// </summary>
        public static void WriteEventLogEntry(string source, Exception ex)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) EventLog.WriteEntry(source, ex.Message, EventLogEntryType.Error);
            }
            catch { }
        }

        /// <summary>
        /// Returns true if the current Windows user is an administrator of the machine
        /// </summary>
        public static bool IsMachineAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);

                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Couldn't determine user account status: " + ex.Message);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Returns true if the operating system major version is 6 or greater (Windows Vista/Server 2008 or later)
        /// </summary>
        public static bool IsValidOS()
        {
            int major = Environment.OSVersion.Version.Major;
            return (major >= 6);
        }


        /// <summary>
        /// Write the content of a DataTable to the debug output
        /// </summary>
        public static void DisplayDataTable(DataTable table)
        {
            foreach (System.Data.DataRow row in table.Rows)
            {
                foreach (System.Data.DataColumn col in table.Columns)
                {
                    Debug.WriteLine("{0} = {1}", col.ColumnName, row[col]);
                }
                Debug.WriteLine("============================");
            }
        }


        /// <summary>
        /// Load a DataTable from a SQL statement executed on a database connection
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0038:Use pattern matching", Justification = "<Pending>")]
        public static DataTable GetDataTable(DbConnection connection, string sql)
        {
            DataTable result = new DataTable();
            DbDataAdapter adapter = null;
            if (connection is OdbcConnection)
            {
                OdbcCommand command = ((OdbcConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new OdbcDataAdapter(command);
            }
            else if (connection is SqlConnection)
            {
                SqlCommand command = ((SqlConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new SqlDataAdapter(command);
            }
            else if (connection is MySqlConnector.MySqlConnection)
            {
                MySqlConnector.MySqlCommand command = ((MySqlConnector.MySqlConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new MySqlConnector.MySqlDataAdapter(command);
            }
            else if (connection is OracleConnection)
            {
                OracleCommand command = ((OracleConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new OracleDataAdapter(command);
            }
            else if (connection is NpgsqlConnection)
            {
                NpgsqlCommand command = ((NpgsqlConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new NpgsqlDataAdapter(command);
            }
            else if (connection is SQLiteConnection)
            {
                SQLiteCommand command = ((SQLiteConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new SQLiteDataAdapter(command);
            }
            else
            {
                OleDbCommand command = ((OleDbConnection)connection).CreateCommand();
                command.CommandTimeout = 0;
                command.CommandText = sql;
                adapter = new OleDbDataAdapter(command);
            }
            adapter.Fill(result);
            return result;
        }

        /// <summary>
        /// If the source contains the pattern at the given index, append the replacement to the result, advance the index and return true
        /// </summary>
        public static bool FindReplacePattern(string source, ref int index, string pattern, string replace, StringBuilder result)
        {
            if (index + pattern.Length <= source.Length && source.Substring(index, pattern.Length) == pattern)
            {
                result.Append(replace);
                index += pattern.Length - 1;
                return true;
            }
            return false;

        }

        /// <summary>
        /// Clone an object using XML serialization
        /// </summary>
        public static object Clone(Object source)
        {
            XmlSerializer serializer = new XmlSerializer(source.GetType());
            MemoryStream ms = new MemoryStream();
            serializer.Serialize(ms, source);
            ms.Position = 0;
            var result = serializer.Deserialize(ms);
            ms.Close();
            return result;
        }

        /// <summary>
        /// Serialize an object to an XML file
        /// </summary>
        public static void Serialize(string path, object obj, XmlSerializer serializer = null)
        {
            if (serializer == null) serializer = new XmlSerializer(obj.GetType());

            using (var tw = new FileStream(path, FileMode.Create))
            {
                using (var writer = new XmlTextWriter(tw, null) { Formatting = Formatting.Indented, Indentation = 2 })
                {
                    serializer.Serialize(writer, obj);
                    writer.Close();
                }
                tw.Close();
            }
        }

        /// <summary>
        /// Create a DbConnection from a connection type and a connection string
        /// </summary>
        public static DbConnection DbConnectionFromConnectionString(ConnectionType connectionType, string connectionString)
        {
            DbConnection connection;
            if (connectionType == ConnectionType.MSSQLServer || connectionType == ConnectionType.MSSQLServerMicrosoft)
            {
                connection = new SqlConnection(connectionString);
            }
            else if (connectionType == ConnectionType.Odbc)
            {
                connection = new OdbcConnection(connectionString);
            }
            else if (connectionType == ConnectionType.MySQL)
            {
                connection = new MySqlConnector.MySqlConnection(connectionString);
            }
            else if (connectionType == ConnectionType.Oracle)
            {
                connection = new OracleConnection(connectionString);
            }
            else if (connectionType == ConnectionType.PostgreSQL)
            {
                connection = new NpgsqlConnection(connectionString);
            }
            else if (connectionType == ConnectionType.SQLite)
            {
                connection = new SQLiteConnection(connectionString);
            }
            else
            {
                OleDbConnectionStringBuilder builder = new OleDbConnectionStringBuilder(connectionString);
                string provider = builder["Provider"].ToString();
                if (provider.StartsWith("MSDASQL"))
                {
                    //Provider=MSDASQL.1;Persist Security Info=False;Extended Properties="DSN=mysql2;SERVER=localhost;UID=root;DATABASE=sakila;PORT=3306";Initial Catalog=sakila
                    //Provider=MSDASQL.1;Persist Security Info=True;Data Source=mysql;Initial Catalog=sakila
                    //Provider=MSDASQL.1;Persist Security Info=False;Extended Properties="DSN=brCRM;DBQ=C:\tem\adb.mdb;DriverId=25;FIL=MS Access;MaxBufferSize=2048;PageTimeout=5;UID=admin;"

                    //Extract the real ODBC connection string...to be able to use the OdbcConnection
                    string odbcConnectionString = "";
                    if (builder.ContainsKey("Extended Properties")) odbcConnectionString = builder["Extended Properties"].ToString();
                    else if (builder.ContainsKey("Data Source") && !string.IsNullOrEmpty(builder["Data Source"].ToString())) odbcConnectionString = "DSN=" + builder["Data Source"].ToString();
                    if (odbcConnectionString != "" && builder.ContainsKey("Initial Catalog")) odbcConnectionString += ";DATABASE=" + builder["Initial Catalog"].ToString();
                    if (odbcConnectionString != "" && builder.ContainsKey("User ID")) odbcConnectionString += ";UID=" + builder["User ID"].ToString();
                    if (odbcConnectionString != "" && builder.ContainsKey("Password")) odbcConnectionString += ";PWD=" + builder["Password"].ToString();

                    connection = new OdbcConnection(odbcConnectionString);
                }
                else
                {
                    connection = new OleDbConnection(connectionString);
                }
            }

            return connection;
        }

        /// <summary>
        /// Determine the DatabaseType from a connection string
        /// </summary>
        static public DatabaseType GetDatabaseType(string connectionString)
        {
            DatabaseType result = DatabaseType.Standard;
            if (connectionString.ToLower().Contains("oracle"))
            {
                result = DatabaseType.Oracle;
            }
            else if (connectionString.ToLower().Contains(".mdb") || connectionString.ToLower().Contains(".accdb"))
            {
                result = DatabaseType.MSAccess;
            }
            else if (connectionString.ToLower().Contains(".xls") || connectionString.ToLower().Contains("excel driver"))
            {
                result = DatabaseType.MSExcel;
            }
            else if (connectionString.ToLower().Contains("sqlncli"))
            {
                result = DatabaseType.MSSQLServer;
            }
            else if (connectionString.ToLower().Contains("postgres"))
            {
                result = DatabaseType.PostgreSQL;
            }
            else if (connectionString.ToLower().Contains("sqlite"))
            {
                result = DatabaseType.SQLite;
            }

            return result;
        }

        /// <summary>
        /// Add 'User ID' and 'Password' to an OLE DB connection string if they are not already set
        /// </summary>
        static public string GetOleDbConnectionString(string input, string userName, string password)
        {
            string result = input;
            if (input != null && !input.Contains("User ID=") && !string.IsNullOrEmpty(userName)) result += string.Format(";User ID={0}", userName);
            if (input != null && !input.Contains("Password=") && !string.IsNullOrEmpty(password)) result += string.Format(";Password={0}", password);
            return result;
        }

        /// <summary>
        /// Add 'UID' and 'PWD' to an ODBC connection string if they are not already set
        /// </summary>
        static public string GetOdbcConnectionString(string input, string userName, string password)
        {
            string result = input;
            if (input != null && !input.Contains("UID=") && !string.IsNullOrEmpty(userName)) result += string.Format(";UID={0}", userName);
            if (input != null && !input.Contains("PWD=") && !string.IsNullOrEmpty(password)) result += string.Format(";PWD={0}", password);
            return result;
        }

        /// <summary>
        /// Replace the %USER% and %PASSWORD% keywords in a Mongo DB connection string
        /// </summary>
        static public string GetMongoConnectionString(string input, string userName, string password)
        {
            string result = input;
            try
            {
                if (result != null && result.Contains("%USER%")) result = string.Format(result.Replace("%USER%", "{0}"), userName);
                if (result != null && result.Contains("%PASSWORD%")) result = string.Format(result.Replace("%PASSWORD%", "{0}"), password);
            }
            catch { }
            return result;
        }
        /// <summary>
        /// Add 'UID' and 'PWD' to a MySQL connection string if they are not already set
        /// </summary>
        static public string GetMySQLConnectionString(string input, string userName, string password)
        {
            string result = input;
            if (input != null && !input.Contains("UID=") && !string.IsNullOrEmpty(userName)) result += string.Format(";UID={0}", userName);
            if (input != null && !input.Contains("PWD=") && !string.IsNullOrEmpty(password)) result += string.Format(";PWD={0}", password);
            return result;
        }

        /// <summary>
        /// Returns the content of an image file as a base64 data URI to be used in an HTML img src attribute
        /// </summary>
        public static string HtmlMakeImageSrcData(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] filebytes = new byte[fs.Length];
            _ = fs.Read(filebytes, 0, Convert.ToInt32(fs.Length));
            var ext = Path.GetExtension(path);
            string type;
            if (ext == ".ico") type = "x-icon";
            else type = ext.Replace(".", "");
            if (type.ToLower() == "svg") type += "+xml";
            return "data:image/" + type + ";base64," + Convert.ToBase64String(filebytes, Base64FormattingOptions.None);
        }

        /// <summary>
        /// Returns a file path as an HTML encoded 'file:///' URL
        /// </summary>
        public static string HtmlGetFilePath(string path)
        {
            return "file:///" + HttpUtility.HtmlEncode(path.Replace(Path.DirectorySeparatorChar.ToString(), "/"));
        }

        /// <summary>
        /// Returns true if the date/time format contains a time component
        /// </summary>
        static public bool HasTimeFormat(DateTimeStandardFormat formatType, string format)
        {
            if (formatType.ToString().Contains("Time")) return true;
            return ((formatType == DateTimeStandardFormat.Custom || formatType == DateTimeStandardFormat.Default)
                && (format.ToLower().Contains("t") || format.Contains("H") || format.Contains("m") || format.Contains("s")));
        }

        /// <summary>
        /// Add email address to a MailAddressCollection
        /// </summary>
        static public void AddEmailAddresses(MailAddressCollection collection, string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                var addresses = GetEmailAddresses(input);
                foreach (string address in addresses)
                {
                    collection.Add(address);
                }
            }
        }

        /// <summary>
        /// Add email address to a InternetAddressList
        /// </summary>
        public static void AddEmailAddresses(InternetAddressList list, string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                var addresses = GetEmailAddresses(input);
                foreach (string address in addresses)
                {
                    list.Add(MailboxAddress.Parse(address.Trim()));
                }
            }
        }

        /// <summary>
        /// Add email address to a MailAddressCollection
        /// </summary>
        static public string[] GetEmailAddresses(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                return input.Replace(";", "\r\n").Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').ToList().Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
            }
            return new string[0];

        }


        //SQL Keywords management
        /// <summary>
        /// Replace all Seal keywords (enum filters, enum values, common restrictions and values) in a SQL statement by neutral values (e.g. common restrictions by '1=1')
        /// </summary>
        public static string ClearAllSQLKeywords(string sql, ReportModel model = null)
        {
            sql = ClearSQLKeywords(sql, Repository.EnumFilterKeyword, "filter");
            sql = ClearSQLKeywords(sql, Repository.EnumValuesKeyword, "NULL");
            if (model != null) sql = model.ParseCommonRestrictions(sql);
            sql = ClearSQLKeywords(sql, Repository.CommonRestrictionKeyword, "1=1");
            sql = ClearSQLKeywords(sql, Repository.CommonValueKeyword, "NULL");
            return sql;
        }

        //SQL Keywords management
        /// <summary>
        /// Replace all Seal enum keywords (enum filters and values) in a LINQ script by 'null'
        /// </summary>
        public static string ClearAllLINQKeywords(string script)
        {
            script = ClearSQLKeywords(script, Repository.EnumFilterKeyword, "null");
            script = ClearSQLKeywords(script, Repository.EnumValuesKeyword, "null");
            return script;
        }

        /// <summary>
        /// Replace all occurrences of a keyword expression (keyword followed by a name and a closing '}') in a SQL statement by a given value
        /// </summary>
        public static string ClearSQLKeywords(string sql, string keyword, string replacedBy)
        {
            if (string.IsNullOrEmpty(sql)) return "";

            //Replace keyword by 1=1
            int index = 0;
            do
            {
                index = sql.IndexOf(keyword, index);
                if (index > 0)
                {
                    index += keyword.Length;
                    for (int i = index; i < sql.Length; i++)
                    {
                        if (sql[i] == '}')
                        {
                            sql = sql.Replace(keyword + sql.Substring(index, i - index) + "}", replacedBy);
                            index -= keyword.Length;
                            break;
                        }
                    }
                }
            }
            while (index > 0 && index < sql.Length);
            return sql;
        }

        /// <summary>
        /// Merge a CTE (Common Table Expression 'WITH' clause) into an existing CTE clause
        /// </summary>
        public static string AddCTE(string current, string CTE)
        {
            var result = current;
            if (!string.IsNullOrEmpty(result))
            {
                if (CTE != null && CTE.Length > 5 && CTE.ToLower().Trim().StartsWith("with"))
                {
                    var startIndex = CTE.ToLower().IndexOf("with");
                    if (startIndex >= 0) result += "," + CTE.Substring(startIndex + 5);
                }
            }
            else result = CTE;

            return result;
        }

        /// <summary>
        /// Returns the names used with a given keyword in a SQL statement (text between the keyword and the closing '}')
        /// </summary>
        public static List<string> GetSQLKeywordNames(string sql, string keyword)
        {
            var result = new List<string>();
            //Get keywords
            int index = 0;
            do
            {
                index = sql.IndexOf(keyword, index);
                if (index >= 0)
                {
                    index += keyword.Length;
                    string restrictionName = "";
                    for (int i = index; i < sql.Length; i++)
                    {
                        if (sql[i] == '}')
                        {
                            restrictionName = sql.Substring(index, i - index); ;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(restrictionName)) result.Add(restrictionName);
                }

            }
            while (index > 0);
            return result;
        }


        /// <summary>
        /// Returns the directory of the current application executable
        /// </summary>
        public static string GetApplicationDirectory()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly == null) assembly = Assembly.GetCallingAssembly();
            if (assembly == null) assembly = new StackTrace().GetFrames().Last().GetMethod().Module.Assembly;
            return Path.GetDirectoryName(assembly.Location);
        }

        /// <summary>
        /// Convert a TimeSpan to a translated display string (e.g. '2 hours'), or a default value if empty
        /// </summary>
        public static string FromTimeSpan(TimeSpan val, string def, Repository repository)
        {
            string result = "";
            string secStr = repository == null ? "second" : repository.TranslateWebJS("second").ToLower();
            string minStr = repository == null ? "minute" : repository.TranslateWebJS("minute").ToLower();
            string hourStr = repository == null ? "hour" : repository.TranslateWebJS("hour").ToLower();
            string dayStr = repository == null ? "day" : repository.TranslateWebJS("day").ToLower();
            string secStr2 = repository == null ? "seconds" : repository.TranslateWebJS("seconds").ToLower();
            string minStr2 = repository == null ? "minutes" : repository.TranslateWebJS("minutes").ToLower();
            string hourStr2 = repository == null ? "hours" : repository.TranslateWebJS("hours").ToLower();
            string dayStr2 = repository == null ? "days" : repository.TranslateWebJS("days").ToLower();

            if (val.Seconds > 0) result = val.Seconds.ToString() + " " + (val.Seconds > 1 ? secStr2 : secStr);
            else if (val.Minutes > 0) result = val.Minutes.ToString() + " " + (val.Minutes > 1 ? minStr2 : minStr);
            else if (val.Hours > 0) result = val.Hours.ToString() + " " + (val.Hours > 1 ? hourStr2 : hourStr);
            else if (val.Days > 0) result = val.Days.ToString() + " " + (val.Days > 1 ? dayStr2 : dayStr);
            else result = def;
            return result;
        }

        /// <summary>
        /// Parse a translated display string (e.g. '2 hours') into a TimeSpan
        /// </summary>
        public static TimeSpan ToTimeSpan(string str, Repository repository)
        {
            string secStr = repository == null ? "second" : repository.TranslateWebJS("second").ToLower();
            string minStr = repository == null ? "minute" : repository.TranslateWebJS("minute").ToLower();
            string hourStr = repository == null ? "hour" : repository.TranslateWebJS("hour").ToLower();
            string dayStr = repository == null ? "day" : repository.TranslateWebJS("day").ToLower();
            TimeSpan result = new TimeSpan(0, 0, 0);
            if (!string.IsNullOrEmpty(str))
            {
                int val;
                str = str.ToLower();
                if (str.Contains(secStr))
                {
                    if (int.TryParse(str.Replace(" ", "").Replace(secStr, "").Replace("s", "").Replace("e", ""), out val)) result = new TimeSpan(0, 0, val);
                    else throw new Exception("Invalid interval");
                }
                else if (str.Contains(minStr))
                {
                    if (int.TryParse(str.Replace(" ", "").Replace(minStr, "").Replace("s", "").Replace("e", ""), out val)) result = new TimeSpan(0, val, 0);
                    else throw new Exception("Invalid interval");
                }
                else if (str.Contains(hourStr))
                {
                    if (int.TryParse(str.Replace(" ", "").Replace(hourStr, "").Replace("s", "").Replace("e", ""), out val)) result = new TimeSpan(val, 0, 0);
                    else throw new Exception("Invalid interval");
                }
                else if (str.Contains(dayStr))
                {
                    if (int.TryParse(str.Replace(" ", "").Replace(dayStr, "").Replace("s", ""), out val)) result = new TimeSpan(val, 0, 0, 0);
                    else throw new Exception("Invalid interval");
                }
            }
            return result;
        }

        /// <summary>
        /// Convert an objet to a string, handling DBNull value
        /// </summary>
        static public string ToString(object obj)
        {
            if (obj != null && obj == DBNull.Value) obj = null;
            return Convert.ToString(obj);
        }

        /// <summary>
        /// Convert an objet to a datetime, handling DBNull value
        /// </summary>
        static public DateTime? ToDateTime(object obj)
        {
            if (obj != null && obj == DBNull.Value) obj = null;
            return obj == null ? (DateTime?)null : Convert.ToDateTime(obj, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert an objet to a double, handling DBNull value
        /// </summary>
        static public double? ToDouble(object obj)
        {
            if (obj != null && obj == DBNull.Value) obj = null;
            return obj == null ? (double?)null : Convert.ToDouble(obj, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Check if a password is complex enough (at least 8 characters, one uppercase letter, one digit, and one special character)   
        /// </summary>
        public static bool IsPasswordComplex(string password, int len = 8)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < len) return false;

            bool hasUpperCase = Regex.IsMatch(password, "[A-Z]");
            bool hasDigit = Regex.IsMatch(password, "[0-9]");
            bool hasSpecialChar = Regex.IsMatch(password, "[^a-zA-Z0-9]");

            return hasUpperCase && hasDigit && hasSpecialChar;
        }

        private static string FormatMaskedPhone(string maskedDigits, string originalPhone)
        {
            // Try to preserve the original formatting structure
            StringBuilder result = new StringBuilder();
            int maskedIndex = 0;

            foreach (char c in originalPhone)
            {
                if (char.IsDigit(c))
                {
                    // Replace with masked digit if available
                    if (maskedIndex < maskedDigits.Length)
                    {
                        result.Append(maskedDigits[maskedIndex]);
                        maskedIndex++;
                    }
                }
                else
                {
                    // Keep formatting characters like (), -, +, spaces
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Mask a phone number to show only the last 4 digits, replacing the rest with asterisks.
        /// </summary>
        public static string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            // Remove all non-digit characters for processing
            string digitsOnly = Regex.Replace(phoneNumber, @"\D", "");

            if (digitsOnly.Length < 4)
                return new string('*', phoneNumber.Length);

            // Show last 4 digits
            string masked = new string('*', digitsOnly.Length - 4) + digitsOnly.Substring(digitsOnly.Length - 4);

            // If original had formatting, try to preserve some structure
            if (phoneNumber.Contains("-") || phoneNumber.Contains(" ") || phoneNumber.Contains("("))
            {
                return FormatMaskedPhone(masked, phoneNumber);
            }

            return masked;
        }

        /// <summary>
        /// Masks an email address using standard masking
        /// Shows first 1-2 and last 1 character of local part
        /// Example: john.doe@example.com → jo****e@example.com
        /// </summary>
        public static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            if (parts.Length != 2)
                return email;

            string localPart = parts[0];
            string domain = parts[1];

            // Handle very short local parts
            if (localPart.Length <= 3)
            {
                return new string('*', localPart.Length) + "@" + domain;
            }

            // Determine how many characters to show at start
            int showStart = localPart.Length <= 6 ? 1 : 2;
            int showEnd = 1;
            int maskLength = localPart.Length - showStart - showEnd;

            string maskedLocal = localPart.Substring(0, showStart) +
                               new string('*', maskLength) +
                               localPart.Substring(localPart.Length - showEnd);

            return maskedLocal + "@" + domain;
        }

        #region License
        /// <summary>
        /// Decrypt a license text and extract its generation date, version, serial number, name and type
        /// </summary>
        public static void GetLicense(string text, out DateTime generationDate, out int version, out string serial, out string name, out string type)
        {
            string[] texts = CryptoHelper.DecryptAES(text, CryptoHelper.AESLicenseKey).Split('\r');
            if (texts.Length < 6) throw new Exception("Invalid license1:" + texts.Length.ToString());
            if (CryptoHelper.RSAVerifySignature(CryptoHelper.RSALicensePublicKey, texts[1] + "\r" + texts[2] + "\r" + texts[3] + "\r" + texts[4] + "\r" + texts[5], texts[0])) throw new Exception("Invalid license2:" + texts.Length.ToString());
            if (texts[1].Length != 14) throw new Exception("Invalid license3:" + texts[1]);
            generationDate = new DateTime(int.Parse(texts[1].Substring(0, 4)), int.Parse(texts[1].Substring(4, 2)), int.Parse(texts[1].Substring(6, 2)), int.Parse(texts[1].Substring(8, 2)), int.Parse(texts[1].Substring(10, 2)), int.Parse(texts[1].Substring(12, 2)));
            version = int.Parse(texts[2]);
            serial = texts[3];
            name = texts[4];
            type = texts[5];
        }

        /// <summary>
        /// Read and check a license file, returning a text describing the license (empty if no file or invalid file)
        /// </summary>
        public static string GetLicenseText(string licenseFilePath, out bool invalid)
        {
            var result = "";
            invalid = false;
            try
            {
                if (File.Exists(licenseFilePath))
                {
                    GetLicense(File.ReadAllText(licenseFilePath, Encoding.UTF8), out DateTime generationDate, out int version, out string serial, out string name, out string type);
                    var buildDate = SealServerConfiguration.GetBuildDate();
                    if (generationDate > buildDate.AddYears(1))
                    {
                        invalid = true;
                        result = $"Warning: The license date {generationDate.ToShortDateString()} is invalid for the build date {buildDate.ToShortDateString()}.\r\n";
                        result += "Please consider to buy a new license to use this version.\r\nThank you.\r\n\r\n";
                    }
                    result += $"License type: {type}\r\nSerial number: {serial}\r\nLicense name: {name}\r\nGeneration date: {generationDate.ToShortDateString()}\r\n";
                }
            }
            catch
            {
                result = "";
            }
            return result;
        }

        #endregion

    }
}
