//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using ScintillaNET;
using System;
using System.Drawing;
using ScintillaNET_FindReplaceDialog;
using System.Windows.Forms;

namespace Seal.Helpers
{
    public class ScintillaHelper
    {
        public static void Init(Scintilla scintilla, string lang)
        {
            if (lang == "cs") Init(scintilla, Lexer.Cpp);
            else if (lang == "css") Init(scintilla, Lexer.Css);
            else if (lang == "mssql") Init(scintilla, Lexer.Sql);
            else if (lang == "html") Init(scintilla, Lexer.Html);
        }
 
        public static void Init(Scintilla scintilla, Lexer lex, bool showLineNumber = true)
        {
            scintilla.Lexer = lex;
            // we have common to every lexer style saves time.
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = "Consolas";
            scintilla.Styles[Style.Default].Size = 10;
            scintilla.StyleClearAll();

            if (showLineNumber)
            {
                // Show line numbers
                scintilla.Margins[0].Width = 20;
                scintilla.Styles[Style.LineNumber].ForeColor = Color.FromArgb(255, 128, 128, 128);  //Dark Gray
                scintilla.Styles[Style.LineNumber].BackColor = Color.FromArgb(255, 228, 228, 228);  //Light Gray
            }

            if (lex == Lexer.Cpp)
            {
                // Configure the CPP (C#) lexer styles
                scintilla.Styles[Style.Cpp.Default].ForeColor = Color.Silver;
                scintilla.Styles[Style.Cpp.Comment].ForeColor = Color.FromArgb(0, 128, 0); // Green
                scintilla.Styles[Style.Cpp.CommentLine].ForeColor = Color.FromArgb(0, 128, 0); // Green
                scintilla.Styles[Style.Cpp.CommentLineDoc].ForeColor = Color.FromArgb(128, 128, 128); // Gray
                scintilla.Styles[Style.Cpp.Number].ForeColor = Color.Olive;
                scintilla.Styles[Style.Cpp.Word].ForeColor = Color.Blue;
                scintilla.Styles[Style.Cpp.Word2].ForeColor = Color.Blue;
                scintilla.Styles[Style.Cpp.String].ForeColor = Color.FromArgb(163, 21, 21); // Red
                scintilla.Styles[Style.Cpp.Character].ForeColor = Color.FromArgb(163, 21, 21); // Red
                scintilla.Styles[Style.Cpp.Verbatim].ForeColor = Color.FromArgb(163, 21, 21); // Red
                scintilla.Styles[Style.Cpp.StringEol].BackColor = Color.Pink;
                scintilla.Styles[Style.Cpp.Operator].ForeColor = Color.Purple;
                scintilla.Styles[Style.Cpp.Preprocessor].ForeColor = Color.Maroon;

                scintilla.Lexer = Lexer.Cpp;

                // Set the keywords
                scintilla.SetKeywords(0, "abstract as base break case catch checked continue default delegate do else event explicit extern false finally fixed for foreach goto if implicit in interface internal is lock namespace new null object operator out override params private protected public readonly ref return sealed sizeof stackalloc switch this throw true try typeof unchecked unsafe using virtual while");
                scintilla.SetKeywords(1, "bool byte char class const decimal double enum float int long sbyte short static string struct uint ulong ushort void");

                //Folding
                // Instruct the lexer to calculate folding
                scintilla.SetProperty("fold", "1");
                scintilla.SetProperty("fold.compact", "1");

                // Configure a margin to display folding symbols
                scintilla.Margins[2].Type = MarginType.Symbol;
                scintilla.Margins[2].Mask = Marker.MaskFolders;
                scintilla.Margins[2].Sensitive = true;
                scintilla.Margins[2].Width = 20;

                // Set colors for all folding markers
                for (int i = 25; i <= 31; i++)
                {
                    scintilla.Markers[i].SetForeColor(SystemColors.ControlLightLight);
                    scintilla.Markers[i].SetBackColor(SystemColors.ControlDark);
                }

                // Configure folding markers with respective symbols
                scintilla.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
                scintilla.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
                scintilla.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
                scintilla.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
                scintilla.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
                scintilla.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
                scintilla.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

                // Enable automatic folding
                scintilla.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);
            }
            else if (lex == Lexer.Sql)
            {
                // Reset the styles
                scintilla.StyleResetDefault();
                scintilla.Styles[Style.Default].Font = "Courier New";
                scintilla.Styles[Style.Default].Size = 10;
                scintilla.StyleClearAll();

                // Set the SQL Lexer
                scintilla.Lexer = Lexer.Sql;

                // Set the Styles
                scintilla.Styles[Style.Sql.Comment].ForeColor = Color.Green;
                scintilla.Styles[Style.Sql.CommentLine].ForeColor = Color.Green;
                scintilla.Styles[Style.Sql.CommentLineDoc].ForeColor = Color.Green;
                scintilla.Styles[Style.Sql.Number].ForeColor = Color.Maroon;
                scintilla.Styles[Style.Sql.Word].ForeColor = Color.Blue;
                scintilla.Styles[Style.Sql.Word2].ForeColor = Color.Fuchsia;
                scintilla.Styles[Style.Sql.User1].ForeColor = Color.Gray;
                scintilla.Styles[Style.Sql.User2].ForeColor = Color.FromArgb(255, 00, 128, 192);    //Medium Blue-Green
                scintilla.Styles[Style.Sql.String].ForeColor = Color.Red;
                scintilla.Styles[Style.Sql.Character].ForeColor = Color.Red;
                scintilla.Styles[Style.Sql.Operator].ForeColor = Color.Black;

                // Set keyword lists
                // Word = 0
                scintilla.SetKeywords(0, @"add alter as authorization backup begin bigint binary bit break browse bulk by cascade case catch check checkpoint close clustered column commit compute constraint containstable continue create current cursor cursor database date datetime datetime2 datetimeoffset dbcc deallocate decimal declare default delete deny desc disk distinct distributed double drop dump else end errlvl escape except exec execute exit external fetch file fillfactor float for foreign freetext freetexttable from full function goto grant group having hierarchyid holdlock identity identity_insert identitycol if image index insert int intersect into key kill lineno load merge money national nchar nocheck nocount nolock nonclustered ntext numeric nvarchar of off offsets on open opendatasource openquery openrowset openxml option order over percent plan precision primary print proc procedure public raiserror read readtext real reconfigure references replication restore restrict return revert revoke rollback rowcount rowguidcol rule save schema securityaudit select set setuser shutdown smalldatetime smallint smallmoney sql_variant statistics table table tablesample text textsize then time timestamp tinyint to top tran transaction trigger truncate try union unique uniqueidentifier update updatetext use user values varbinary varchar varying view waitfor when where while with writetext xml go ");
                // Word2 = 1
                scintilla.SetKeywords(1, @"ascii cast char charindex ceiling coalesce collate contains convert current_date current_time current_timestamp current_user floor isnull max min nullif object_id session_user substring system_user tsequal ");
                // User1 = 4
                scintilla.SetKeywords(4, @"all and any between cross exists in inner is join left like not null or outer pivot right some unpivot ( ) * ");
                // User2 = 5
                scintilla.SetKeywords(5, @"sys objects sysobjects ");
            }
            else if (lex == Lexer.Container)
            {
                // Reset the styles
                scintilla.StyleResetDefault();
                scintilla.Styles[Style.Default].Font = "Courier New";
                scintilla.Styles[Style.Default].Size = 10;
                scintilla.StyleClearAll();

                // Set the SQL Lexer
                scintilla.Styles[ScintillaRestrictionLexer.StyleDefault].ForeColor = Color.Black;
                scintilla.Styles[ScintillaRestrictionLexer.StyleKeyword].ForeColor = Color.Olive;
                scintilla.Styles[ScintillaRestrictionLexer.StyleIdentifier].ForeColor = Color.Black;
                scintilla.Styles[ScintillaRestrictionLexer.StyleNumber].ForeColor = Color.Maroon;
                scintilla.Styles[ScintillaRestrictionLexer.StyleString].ForeColor = Color.Red;
                scintilla.Styles[ScintillaRestrictionLexer.StyleRestriction].ForeColor = Color.Teal;

                if (scintilla.Tag == null)
                {
                    ScintillaRestrictionLexer sealLexer = new ScintillaRestrictionLexer(@"all and any between cross exists in inner is join left like not null or outer pivot right some unpivot add alter as authorization backup begin bigint binary bit break browse bulk by cascade case catch check checkpoint close clustered column commit compute constraint containstable continue create current cursor cursor database date datetime datetime2 datetimeoffset dbcc deallocate decimal declare default delete deny desc disk distinct distributed double drop dump else end errlvl escape except exec execute exit external fetch file fillfactor float for foreign freetext freetexttable from full function goto grant group having hierarchyid holdlock identity identity_insert identitycol if image index insert int intersect into key kill lineno load merge money national nchar nocheck nocount nolock nonclustered ntext numeric nvarchar of off offsets on open opendatasource openquery openrowset openxml option order over percent plan precision primary print proc procedure public raiserror read readtext real reconfigure references replication restore restrict return revert revoke rollback rowcount rowguidcol rule save schema securityaudit select set setuser shutdown smalldatetime smallint smallmoney sql_variant statistics table table tablesample text textsize then time timestamp tinyint to top tran transaction trigger truncate try union unique uniqueidentifier update updatetext use user values varbinary varchar varying view waitfor when where while with writetext xml go");
                    scintilla.StyleNeeded += new EventHandler<StyleNeededEventArgs>(delegate (object sender, StyleNeededEventArgs e)
                    {
                        var startPos = scintilla.GetEndStyled();
                        var endPos = e.Position;

                        sealLexer.Style(scintilla, startPos, endPos);
                    });
                }
            }

            //First initialization
            if (scintilla.Tag == null)
            {
                //Find replace dialog
                FindReplace replaceDlg = new FindReplace();
                replaceDlg.Scintilla = scintilla;
                scintilla.Tag = replaceDlg;

                scintilla.KeyDown += Scintilla_KeyDown;
                if (showLineNumber) scintilla.TextChanged += Scintilla_TextChanged;
                scintilla.CharAdded += Scintilla_CharAdded;
            }
        }

        private static void Scintilla_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (sender is Scintilla)
            {
                var replaceDlg = (FindReplace)((Scintilla)sender).Tag;

                if (e.Control && e.KeyCode == Keys.F)
                {
                    replaceDlg.ShowFind();
                    e.SuppressKeyPress = true;
                }
                else if (e.Shift && e.KeyCode == Keys.F3)
                {
                    replaceDlg.Window.FindPrevious();
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.F3)
                {
                    replaceDlg.Window.FindNext();
                    e.SuppressKeyPress = true;
                }
                else if (e.Control && e.KeyCode == Keys.H)
                {
                    replaceDlg.ShowReplace();
                    e.SuppressKeyPress = true;
                }
                else if (e.Control && e.KeyCode == Keys.I)
                {
                    replaceDlg.ShowIncrementalSearch();
                    e.SuppressKeyPress = true;
                }
                else if (e.Control && e.KeyCode == Keys.G)
                {
                    GoTo MyGoTo = new GoTo((Scintilla)sender);
                    MyGoTo.ShowGoToDialog();
                    e.SuppressKeyPress = true;
                }
            }
        }

        private static void Scintilla_CharAdded(object sender, CharAddedEventArgs e)
        {
            if (sender is Scintilla)
            {
                var scintilla = (Scintilla)sender;
                if (e.Char == '\n')
                {
                    var currentPos = scintilla.CurrentPosition;
                    if (scintilla.CurrentLine > 0)
                    {
                        scintilla.Lines[scintilla.CurrentLine].Indentation = scintilla.Lines[scintilla.CurrentLine - 1].Indentation;
                        scintilla.CurrentPosition += scintilla.Lines[scintilla.CurrentLine].Indentation;
                        scintilla.SelectionStart = scintilla.CurrentPosition;
                    }
                }
            }
        }

        static int MaxLineNumberCharLength = 0;
        private static void Scintilla_TextChanged(object sender, EventArgs e)
        {
            Scintilla scintilla = (Scintilla)sender;
            // Did the number of characters in the line number display change?
            // i.e. nnn VS nn, or nnnn VS nn, etc...
            var maxLineNumberCharLength = scintilla.Lines.Count.ToString().Length;
            if (maxLineNumberCharLength == MaxLineNumberCharLength)
                return;

            // Calculate the width required to display the last line number
            // and include some padding for good measure.
            const int padding = 2;
            scintilla.Margins[0].Width = scintilla.TextWidth(Style.LineNumber, new string('9', maxLineNumberCharLength + 1)) + padding;
            MaxLineNumberCharLength = maxLineNumberCharLength;
        }
    }
}
