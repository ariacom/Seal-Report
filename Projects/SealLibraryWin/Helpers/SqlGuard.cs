//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the MIT License; see the LICENSE file at https://github.com/ariacom/Seal-Report.
//
using System.Text;
using System.Text.RegularExpressions;

namespace Seal.Helpers
{
    /// <summary>
    /// Checks a SQL text written by a user or an AI Agent: one read statement only. It is a simple analysis of the text, called by the script set in <see cref="Seal.Model.SecurityUser.CheckSql"/>:
    /// it refuses what it cannot read for sure (a backslash in a string, a dollar quote), and the rights of the database account stay the real protection.
    /// </summary>
    public static class SqlGuard
    {
        static readonly Regex _firstWordRegex = new Regex(@"^[\s(]*(SELECT|WITH)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        //Allowed by the syntax of a read statement: a data-modifying common table expression, SELECT INTO
        static readonly Regex _writeWordRegex = new Regex(@"\b(INSERT|UPDATE|DELETE|MERGE|INTO)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex _dollarQuoteRegex = new Regex(@"\$\w*\$", RegexOptions.Compiled);

        /// <summary>
        /// Checks a SQL text.
        /// </summary>
        /// <param name="sql">The SQL text</param>
        /// <param name="isStatement">True for a complete statement (it must start with SELECT or WITH), false for a part of a statement (a clause, an expression)</param>
        /// <returns>Null if the text is accepted, otherwise the reason of the refusal, in English</returns>
        public static string Check(string sql, bool isStatement = true)
        {
            var code = GetCode(sql, out string error);
            if (error != null) return error;

            var trimmed = code.TrimEnd();
            if (isStatement && trimmed.EndsWith(";")) trimmed = trimmed.Substring(0, trimmed.Length - 1);
            if (trimmed.Contains(';')) return "only one SQL statement is allowed";
            if (isStatement && !_firstWordRegex.IsMatch(trimmed)) return "only a SELECT or WITH statement is allowed";
            if (_dollarQuoteRegex.IsMatch(trimmed)) return "a dollar quote is not allowed";

            var match = _writeWordRegex.Match(trimmed);
            if (match.Success) return $"the keyword {match.Value.ToUpperInvariant()} is not allowed";
            return null;
        }

        /// <summary>
        /// Returns the SQL text without its comments, with its strings and quoted identifiers emptied.
        /// </summary>
        static string GetCode(string sql, out string error)
        {
            error = null;
            var result = new StringBuilder();
            sql = sql ?? "";
            int i = 0;
            while (i < sql.Length)
            {
                var c = sql[i];
                if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
                {
                    while (i < sql.Length && sql[i] != '\n') i++;
                    result.Append(' ');
                }
                else if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
                {
                    //A comment executed by MySQL
                    if (i + 2 < sql.Length && sql[i + 2] == '!') { error = "this comment is not allowed"; return null; }
                    //Comments are nested in PostgreSQL
                    int depth = 1;
                    i += 2;
                    while (i < sql.Length && depth > 0)
                    {
                        if (sql[i] == '/' && i + 1 < sql.Length && sql[i + 1] == '*') { depth++; i += 2; }
                        else if (sql[i] == '*' && i + 1 < sql.Length && sql[i + 1] == '/') { depth--; i += 2; }
                        else i++;
                    }
                    if (depth > 0) { error = "a comment is not closed"; return null; }
                    result.Append(' ');
                }
                else if (c == '\'' || c == '"' || c == '`')
                {
                    i++;
                    bool closed = false;
                    while (i < sql.Length)
                    {
                        //The meaning of a backslash depends on the database and on the string: E'\'' is one quote in PostgreSQL, '\' is one backslash
                        if (sql[i] == '\\' && c == '\'') { error = "a backslash in a string is not allowed"; return null; }
                        if (sql[i] == c)
                        {
                            if (i + 1 < sql.Length && sql[i + 1] == c) { i += 2; continue; }
                            closed = true;
                            i++;
                            break;
                        }
                        i++;
                    }
                    if (!closed) { error = "a string or a quoted name is not closed"; return null; }
                    result.Append(c).Append(c);
                }
                else
                {
                    result.Append(c);
                    i++;
                }
            }
            return result.ToString();
        }
    }
}
