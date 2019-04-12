//
// Copyright (c) Seal Report, Eric Pfirsch (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Seal.Helpers
{
    class ScintillaRestrictionLexer
    {
        public const int StyleDefault = 0;
        public const int StyleKeyword = 1;
        public const int StyleIdentifier = 2;
        public const int StyleNumber = 3;
        public const int StyleString = 4;
        public const int StyleRestriction = 5;

        private const int STATE_UNKNOWN = 0;
        private const int STATE_IDENTIFIER = 1;
        private const int STATE_NUMBER = 2;
        private const int STATE_STRING = 3;
        private const int STATE_RESTRICTION = 4;

        private HashSet<string> keywords;

        public void Style(Scintilla scintilla, int startPos, int endPos)
        {
            // Back up to the line start
            var line = scintilla.LineFromPosition(startPos);
            startPos = scintilla.Lines[line].Position;

            var length = 0;
            var state = STATE_UNKNOWN;

            // Start styling
            scintilla.StartStyling(startPos);
            while (startPos < endPos)
            {
                var c = (char)scintilla.GetCharAt(startPos);

                REPROCESS:
                switch (state)
                {
                    case STATE_UNKNOWN:
                        if (c == '[')
                        {
                            // Start of "Restriction"
                            scintilla.SetStyling(1, StyleRestriction);
                            state = STATE_RESTRICTION;
                        }
                        else if (c == '\'')
                        {
                            // Start of "string"
                            scintilla.SetStyling(1, StyleString);
                            state = STATE_STRING;
                        }
                        else if (Char.IsDigit(c))
                        {
                            state = STATE_NUMBER;
                            goto REPROCESS;
                        }
                        else if (Char.IsLetter(c))
                        {
                            state = STATE_IDENTIFIER;
                            goto REPROCESS;
                        }
                        else
                        {
                            // Everything else
                            scintilla.SetStyling(1, StyleDefault);
                        }
                        break;

                    case STATE_STRING:
                        if (c == '\'')
                        {
                            length++;
                            scintilla.SetStyling(length, StyleString);
                            length = 0;
                            state = STATE_UNKNOWN;
                        }
                        else
                        {
                            length++;
                        }
                        break;

                    case STATE_NUMBER:
                        if (Char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || c == 'x')
                        {
                            length++;
                        }
                        else
                        {
                            scintilla.SetStyling(length, StyleNumber);
                            length = 0;
                            state = STATE_UNKNOWN;
                            goto REPROCESS;
                        }
                        break;

                    case STATE_IDENTIFIER:
                        if (Char.IsLetterOrDigit(c))
                        {
                            length++;
                        }
                        else
                        {
                            var style = StyleIdentifier;
                            var identifier = scintilla.GetTextRange(startPos - length, length);
                            if (keywords.Contains(identifier.ToLower()))
                                style = StyleKeyword;

                            scintilla.SetStyling(length, style);
                            length = 0;
                            state = STATE_UNKNOWN;
                            goto REPROCESS;
                        }
                        break;

                    case STATE_RESTRICTION:
                        if (c == ']')
                        {
                            length++;
                            scintilla.SetStyling(length, StyleRestriction);
                            length = 0;
                            state = STATE_UNKNOWN;
                        }
                        else
                        {
                            length++;
                        }
                        break;

                }

                startPos++;
            }
        }

        public ScintillaRestrictionLexer(string keywords)
        {
            // Put keywords in a HashSet
            var list = Regex.Split(keywords ?? string.Empty, @"\s+").Where(l => !string.IsNullOrEmpty(l));
            this.keywords = new HashSet<string>(list);
        }
    }
}
