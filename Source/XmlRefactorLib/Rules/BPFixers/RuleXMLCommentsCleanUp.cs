using System;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleXMLCommentsCleanUp : Rule
    {
        const string deleteMarker = "SomethingRareThatDoesn'tExistInSourceCodeAlready";

        public override string RuleName()
        {
            return "XMLCommentsCleanUp()";
        }
        override public string Grouping()
        {
            return "XML Comments Clean Up";
        }
        public override bool Enabled()
        {
            return false;
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("Source", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Source");
        }

        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        private int countTags(string input, string tag)
        {
            int counter = 0;
            int startPos = 0;
            do
            {
                startPos = input.IndexOf("<" + tag + ">", startPos + 1);
                if (startPos > 0)
                    counter++;
            }
            while (startPos > 0);
            return counter;
        }

        private bool isValid(string input)
        {
            if (this.countTags(input, "summary") > 1 ||
                this.countTags(input, "/summary") > 1 ||
                this.countTags(input, "returns") > 1 ||
                this.countTags(input, "/returns") > 1 ||
                this.countTags(input, "remarks") > 1 ||
                this.countTags(input, "/remarks") > 1)
            {
                return false;
            }

            return true;
        }

        public string Run(string _input, int startAt = 0)
        {
            Match match = xpoMatch.Match(_input, startAt);

            if (match.Success)
            {
                string xml = _input;
                var stringToUpdate = match.Value;
                _input = this.processSource(_input, stringToUpdate);

                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }


        private string replace(string input, string tag)
        {
            string startTag = "<" + tag + ">";
            string endTag = "</" + tag + ">";

            input = input.Replace("<" + tag.Substring(0, 1).ToUpperInvariant() + tag.Substring(1) + ">", startTag);
            input = input.Replace("</" + tag.Substring(0, 1).ToUpperInvariant() + tag.Substring(1) + ">", endTag);

            input = input.Replace("<" + tag.Substring(0, tag.Length - 1) + ">", startTag);
            input = input.Replace("</" + tag.Substring(0, tag.Length - 1) + ">", endTag);

            input = input.Replace("<" + tag + "/>", "");
            input = input.Replace("<" + tag + " />", "");

            input = input.Replace("<." + tag + ">", startTag);
            input = input.Replace("< " + tag + ">", startTag);
            input = input.Replace("<" + tag + "    >", startTag);
            input = input.Replace("<" + tag + "   >", startTag);
            input = input.Replace("<" + tag + "  >", startTag);
            input = input.Replace("<" + tag + " >", startTag);
            input = input.Replace("<" + tag + " ", startTag);
            input = input.Replace("<" + tag + Environment.NewLine, startTag + Environment.NewLine);

            input = input.Replace("<./" + tag + ">", endTag);
            input = input.Replace("</ " + tag + ">", endTag);
            input = input.Replace("</" + tag + "/>", endTag);
            input = input.Replace("</" + tag + "    >", endTag);
            input = input.Replace("</" + tag + "   >", endTag);
            input = input.Replace("</" + tag + "  >", endTag);
            input = input.Replace("</" + tag + " >", endTag);
            input = input.Replace("</" + tag + " ", endTag);
            input = input.Replace("</" + tag + Environment.NewLine, endTag + Environment.NewLine);

            if (!input.Contains(startTag))
            {
                input = input.Replace(tag + " >", startTag);
            }
            if (!input.Contains(startTag))
            {
                input = input.Replace("<" + tag, startTag);
            }

            if (!input.Contains(endTag))
            {
                input = input.Replace(tag + " >", endTag);
            }
            if (!input.Contains(endTag))
            {
                input = input.Replace("/" + tag + ">", endTag);
            }
            if (!input.Contains(endTag))
            {
                input = input.Replace("</" + tag, endTag);
            }


            return input;
        }

        private string fixPairs(string input, string tag)
        {
            string startTag = "<" + tag + ">";
            string endTag = "</" + tag + ">";

            int posFirstStartTag = input.IndexOf(startTag);
            int posLastStartTag = input.IndexOf(startTag, posFirstStartTag + 1);

            if (posLastStartTag > 0)
            {
                input = input.Remove(posLastStartTag, startTag.Length).Insert(posLastStartTag, endTag);
            }

            int posFirstEndTag = input.IndexOf(endTag);
            int posLastEndTag = input.IndexOf(endTag, posFirstEndTag + 1);

            if (posLastEndTag > 0)
            {
                input = input.Remove(posFirstEndTag, endTag.Length).Insert(posFirstEndTag, startTag);
            }

            return input;
        }

        private string fixDanglingParam(string input)
        {
            Boolean hasParam = input.IndexOf("<param") > 0;


            if (!hasParam)
            {
                const string deleteMarker = "SomethingRareThatDoesn'tExistInSourceCodeAlready";
                string[] lines = input.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                for (int i = 1; i < lines.Length; i++)  //Skip first line
                {
                    string line = lines[i];

                    if (!line.TrimStart().StartsWith(@"///"))
                    {
                        break;
                    }
                    if (line.Replace(" ", "") == @"///</param>")
                    {
                        lines[i] = deleteMarker;
                    }
                }

                input = string.Join(Environment.NewLine, lines);
                input = input.Replace(deleteMarker + Environment.NewLine, "");

                input = input.Replace("</param>", "");
            }
            return input;
        }


        private string ensureStartTag(string input, string tag)
        {
            string startTagLine = String.Empty;
            Boolean missingStartTag = input.IndexOf("<" + tag + ">") < 0;
            Boolean hasEndTag = input.IndexOf("</" + tag + ">") > 0;

            if (missingStartTag && hasEndTag)
            {
                string[] lines = input.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                int endTagLine = 0;

                for (int i = 1; i < lines.Length; i++)  //Skip first line
                {
                    string line = lines[i];

                    if (!line.TrimStart().StartsWith(@"///"))
                    {
                        break;
                    }
                    if (line.Replace(" ", "") == "///</" + tag + ">")
                    {
                        startTagLine = line.Replace("</", "<");
                        endTagLine = i;
                        break;
                    }
                }

                if (startTagLine != string.Empty)
                {
                    for (int i = endTagLine - 1; i > 0; i--)
                    {
                        string line = lines[i].Replace("<c", "cc").Replace("</c", "cc");

                        if (line.Contains("<"))
                        {
                            if (i == endTagLine - 1)
                            {
                                lines[endTagLine] = deleteMarker;
                                missingStartTag = false;
                            }
                            else
                            {
                                lines[i] = lines[i] + Environment.NewLine + startTagLine;
                                missingStartTag = false;
                            }
                            break;
                        }
                    }
                    if (missingStartTag)
                    {
                        lines[1] = startTagLine + Environment.NewLine + lines[1];
                    }
                    input = string.Join(Environment.NewLine, lines);
                    input = input.Replace(deleteMarker + Environment.NewLine, "");
                }
            }
            return input;
        }


        private string ensureEndTag(string input, string tag)
        {
            string endTagLine = String.Empty;
            Boolean hasStartTag = input.IndexOf("<" + tag + ">") > 0;
            Boolean missingEndTag = input.IndexOf("</" + tag + ">") < 0;

            if (hasStartTag && missingEndTag)
            {
                string[] lines = input.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                int fullLine = 0;
                for (int i = 1; i < lines.Length; i++)  //Skip first line
                {
                    string line = lines[i].Replace("<c", "cc").Replace("</c", "cc");

                    if (!line.TrimStart().StartsWith(@"///"))
                    {
                        if (missingEndTag &&
                            endTagLine != String.Empty)
                        {
                            lines[i] = endTagLine + Environment.NewLine + lines[i];
                            missingEndTag = false;
                        }
                        break;
                    }
                    if (line.Replace(" ", "") == "///<" + tag + ">")
                    {
                        endTagLine = lines[i].Replace("<", "</");
                    }
                    else if (line.Contains("<" + tag + ">"))
                    {
                        fullLine = i;
                    }
                    else
                    {
                        if (missingEndTag &&
                            endTagLine != String.Empty)
                        {
                            if (line.Contains("<"))
                            {
                                lines[i] = endTagLine + Environment.NewLine + lines[i];
                                missingEndTag = false;
                            }
                        }
                    }
                }
                if (missingEndTag && fullLine > 0)
                {
                    lines[fullLine] += "</" + tag + ">";
                    missingEndTag = false;
                }
                input = string.Join(Environment.NewLine, lines);
                input = input.Replace(deleteMarker + Environment.NewLine, "");

            }
            return input;
        }


        private string ensureTags(string input, string tag)
        {
            input = this.ensureStartTag(input, tag);
            input = this.ensureEndTag(input, tag);
            return input;
        }

        private string processSource(string _input, string _methodString)
        {
            if (_methodString.Contains("returns type="))
                return _input;
            //      if (Scanner.FILENAME.ToLowerInvariant().Contains("axpurchline_in") && _methodString.Contains("parmMaximumRetailPrice_IN"))
            //      {
            //          int i = 0;
            //      }
            string originalMethodString = _methodString;
            _methodString = _methodString.Replace("<Summary/>", "<summary/>");
            _methodString = _methodString.Replace("<sunnary>", "<summary>");
            _methodString = _methodString.Replace("</sunnary>", "</summary>");

            _methodString = this.replace(_methodString, "summary");

            Boolean isXmlDoc = _methodString.IndexOf("</summary>") > 0 ||
                               _methodString.IndexOf("<summary>") > 0;
            if (isXmlDoc)
            {
                _methodString = _methodString.Replace("<returns>c>", "<returns><c>");
                _methodString = _methodString.Replace("/// ///", "///");
                _methodString = this.replace(_methodString, "returns");
                _methodString = this.replace(_methodString, "remarks");
                _methodString = _methodString.Replace("<reamarks>", "<remarks>");
                _methodString = _methodString.Replace("</reamarks>", "</remarks>");
                _methodString = _methodString.Replace("<c/>", "</c>");
                _methodString = _methodString.Replace("<c />", "</c>");

                Boolean summaryComplete = false;
                Boolean returnsComplete = false;
                Boolean remarksComplete = false;
                Boolean remarksMultiLine = false;
                Boolean returnsMultiLine = false;
                int tagStart = 0;
                int emptyLines = 0;
                string[] lines = _methodString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                if (lines[0].Contains("///"))
                    return _input;

                for (int i = 1; i < lines.Length; i++)  //Skip first line
                {
                    string line = lines[i];

                    if (!line.TrimStart().StartsWith(@"///"))
                    {
                        break;
                    }
                    if (i == 1 && line.Contains("</summary>"))
                    {
                        lines[i] = line.Replace("</summary> ", "<summary>");
                        line = lines[i];
                    }
                    else if (i == 1 && !line.Contains("<"))
                    {
                        int pos = line.IndexOf("///") + 3;
                        lines[i] = line.Substring(0, pos) + " <summary>" + Environment.NewLine + line;
                        line = lines[i];
                    }
                    if (line.Contains("<c></c>"))
                    {
                        int pos = line.IndexOf("</c>");
                        int pos2 = line.LastIndexOf("</c>");
                        if (pos == pos2)
                        {
                            lines[i] = line.Replace("<c></c>", "");
                        }
                        else
                        {
                            lines[i] = line.Replace("<c></c>", "<c>");
                        }
                        line = lines[i];
                    }
                    else if (line.Contains("<c><"))
                    {
                        lines[i] = line.Replace("<c><", "<c>");
                        line = lines[i];
                    }
                    if (line.Contains("</>") && line.Contains("<c>") && !line.Contains("</c>"))
                    {
                        lines[i] = line.Replace("</>", "</c>");
                        line = lines[i];
                    }
                    if (!line.Contains("</c>") && line.Contains("/c>") && line.Replace(" ", "").Length > 7)
                    {
                        lines[i] = line.Replace("/c>", "</c>");
                        line = lines[i];
                    }
                    if (!line.Contains("<c>") &&
                        line.Contains("c>") &&
                        line.Replace(" ", "").Length > 7 &&
                        !line.Contains("Go to") &&
                        !line.ToLower().Contains("adic>") &&
                        !line.ToLower().Contains("desc>")
                        )
                    {
                        int pos = line.IndexOf("c>");
                        int pos2 = line.IndexOf("</c>");
                        if (pos != pos2 + 2)
                        {
                            lines[i] = line.Replace("c>", "<c>");
                            line = lines[i];
                        }
                    }
                    if (line.Contains("<c>") && !line.Contains("</c>") && !lines[i + 1].Contains("</c>") && line.Replace(" ", "").Length > 7)
                    {
                        int pos = line.IndexOf("<c>");
                        int pos2 = line.LastIndexOf("<c>");
                        if (pos2 > pos)
                        {
                            lines[i] = line.Substring(0, pos2) + "</c>" + line.Substring(pos2 + 3);
                        }
                        else
                        {
                            lines[i] = line.Replace(" <c> ", " ");
                            lines[i] = line.Replace(" <c>", " ");
                            lines[i] = line.Replace("<c> ", " ");
                            lines[i] = line.Replace("<c>", " ");
                        }
                        line = lines[i];
                    }
                    if (line.Contains("</c>") && !lines[i - 1].Contains("<c>") && !line.Contains("<c>") && line.Replace(" ", "").Length > 8)
                    {
                        int pos = line.IndexOf("</c>");
                        int pos2 = line.LastIndexOf("</c>");
                        if (pos2 > pos)
                        {
                            lines[i] = line.Substring(0, pos) + "<c>" + line.Substring(pos + 4);
                        }
                        else
                        {
                            lines[i] = line.Replace(" </c> ", " ");
                            lines[i] = line.Replace(" </c>", " ");
                            lines[i] = line.Replace("</c> ", " ");
                            lines[i] = line.Replace("</c>", " ");
                        }
                        line = lines[i];
                    }
                    if (line.Contains("<summary>"))
                    {
                        tagStart = i;
                    }
                    else if (line.Contains("<remarks>"))
                    {
                        if (line.Replace(" ", "").Length > 14)
                        {
                            remarksMultiLine = true;
                        }
                        tagStart = i;
                        summaryComplete = true;
                    }
                    else if (line.Contains("<returns>"))
                    {
                        if (line.Replace(" ", "").Length > 14)
                        {
                            returnsMultiLine = true;
                        }
                        tagStart = i;
                        summaryComplete = true;
                    }
                    else if (line.Trim() == @"///")
                    {
                        if (tagStart == i - 1 - emptyLines)
                        {
                            lines[i] = deleteMarker;
                            emptyLines++;
                        }
                    }

                    if (line.Replace(" ", "").StartsWith("///</param") && line.TrimEnd().EndsWith("</param>"))
                    {
                        int pos = line.IndexOf("/param");
                        int pos2 = line.LastIndexOf("/param");
                        if (pos < pos2)
                            lines[i] = line.Remove(pos, 1);
                    }

                    if (line.Contains("</summary>"))
                    {
                        if (summaryComplete)
                        {
                            lines[i] = deleteMarker;
                        }
                        else
                        {
                            if (tagStart > 0 &&
                                i == tagStart + 1 + emptyLines &&
                                !lines[i + 1].Contains("///")) // empty and only tag
                            {
                                for (int j = tagStart; j <= i; j++)
                                {
                                    lines[j] = deleteMarker;
                                }
                            }
                            else if (tagStart == 0 && i == 1)
                            {
                                lines[i] = deleteMarker;
                            }
                        }
                        summaryComplete = true;
                        emptyLines = 0;
                    }
                    else if (line.Contains("</remarks>") && !remarksMultiLine)
                    {
                        if (remarksComplete)
                        {
                            lines[i] = deleteMarker;
                        }
                        else
                        {
                            if (tagStart > 0 && i == tagStart + 1) // empty tag
                            {
                                for (int j = tagStart; j <= i; j++)
                                {
                                    lines[j] = deleteMarker;
                                }
                            }
                        }
                        remarksComplete = true;
                        emptyLines = 0;
                    }
                    else if (line.Contains("</returns>") && !returnsMultiLine)
                    {
                        if (returnsComplete)
                        {
                            lines[i] = deleteMarker;
                        }
                        returnsComplete = true;
                        emptyLines = 0;
                    }
                }
                _methodString = string.Join(Environment.NewLine, lines);
                _methodString = this.fixPairs(_methodString, "summary");
                _methodString = this.fixPairs(_methodString, "returns");
                _methodString = this.fixPairs(_methodString, "remarks");
                _methodString = this.fixDanglingParam(_methodString);
                _methodString = this.ensureTags(_methodString, "summary");
                _methodString = this.ensureTags(_methodString, "returns");
                _methodString = this.ensureTags(_methodString, "remarks");
            }
            else
            {
                string[] lines = _methodString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                for (int i = 1; i < lines.Length; i++)  //Skip first line
                {
                    string line = lines[i];

                    if (!line.TrimStart().StartsWith(@"///"))
                    {
                        if (line.Trim() == String.Empty)
                        {
                            //      lines[i] = deleteMarker;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (line.Trim() == @"///")
                    {
                        lines[i] = deleteMarker;
                    }
                    else
                    {
                        lines[i] = line.Replace(@"//////", @"//");
                        lines[i] = line.Replace(@"/////", @"//");
                        lines[i] = line.Replace(@"////", @"//");
                        lines[i] = line.Replace(@"///", @"//");
                    }
                }

                _methodString = string.Join(Environment.NewLine, lines);
            }
            _methodString = _methodString.Replace(deleteMarker + Environment.NewLine, "");

            if (_methodString != originalMethodString && this.isValid(_methodString))
            {
                Hits++;
                _input = _input.Replace(originalMethodString, _methodString);
            }
            return _input;
        }
    }
}
