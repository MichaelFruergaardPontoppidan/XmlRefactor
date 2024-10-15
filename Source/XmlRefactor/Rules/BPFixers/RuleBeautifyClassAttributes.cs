using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleBeautifyClassAttributes : Rule
    {
        public RuleBeautifyClassAttributes()
        {
        }

        public override string RuleName()
        {
            return "Beautify layout of class level attributes";
        }

        public override bool Enabled()
        {
            return true;
        }
        override public string Grouping()
        {
            return "Layout";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("Declaration", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Declaration");
        }

        public override string Run(string _input)
        {
            return this.Run(_input, 0);
        }

        private bool isTargeted(string methodName)
        {
            return true;
        }

        private int findPosOfFirst(string findMe, string inMe)
        {
            int pos = 0;
            string line = String.Empty;
            do
            {
                pos = inMe.ToLowerInvariant().IndexOf(findMe, pos) + findMe.Length;
                line = this.getLineAtPos(inMe, pos);
            }
            while (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("using"));

            return pos - findMe.Length;
        }
        private string getStartAttributes(string newSource)
        {
            int posOfClass = this.findPosOfFirst("class ", newSource);
            int posOfFirstAttr = this.findPosOfFirst("[", newSource);
            if (posOfFirstAttr >= 0 && posOfClass>=0)
            {
                int posOfLastAttr = newSource.LastIndexOf("]", posOfClass);
                if (posOfLastAttr >= 0)
                {
                    return newSource.Substring(posOfFirstAttr, posOfLastAttr - posOfFirstAttr + 1);
                }
            }
            return String.Empty;
        }

        private string getRearrangedAttributes(string newSource)
        {
            string attribs = this.getStartAttributes(newSource);
            string newLayout = attribs;
            if (attribs.Length > 0 && !attribs.Contains("//"))
            {
                newLayout = "[\r\n    ";
                int parms = 0;
                int attributesFound = 1;
                foreach (char c in attribs.Substring(0, attribs.Length - 1))
                {
                    switch (c)
                    {
                        case '\n':
                        case '\r':
                        case '\t': 
                            break;
                        case '[':
                        case ' ':
                            if (parms > 0)
                                newLayout += c;
                            break;
                        case ']':
                            if (parms > 0)
                            {
                                newLayout += c;
                            }
                            else
                            {
                                newLayout += ",\r\n    ";
                                attributesFound++;
                            }
                            break;
                        case '(':
                            parms++;
                            newLayout += c;
                            break;
                        case ')':
                            parms--;
                            newLayout += c;
                            break;
                        case ',':
                            newLayout += c;
                            if (parms == 0)
                            {
                                newLayout += "\r\n    ";
                                attributesFound++;
                            }
                            break;
                        default:
                            if (!Char.IsControl(c))
                            {
                                newLayout += c;
                            }
                            break;
                    }

                }
                newLayout += "\r\n]\r\n";
                newLayout = newLayout.Replace("Attribute(", "(");
                newLayout = newLayout.Replace("Attribute\r\n]", "\r\n]");
                newLayout = newLayout.Replace("Attribute,", ",");
                newLayout = newLayout.Replace("        ", "    ");

                if (attributesFound == 1)
                {
                    // Make single line
                    newLayout = newLayout.Replace("\r\n", "");
                    newLayout = newLayout.Replace("    ", "");
                    newLayout += "\r\n";
                }
            }
            return newLayout;
        }

        public string Run(string _input, int _startAt = 0)
        {
            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success)
            {
                string source = match.Groups[1].Value;

                Boolean isBody = false;
                Boolean isInMultilineComment = false;
                string usingLines = String.Empty;
                string usingNetLines = String.Empty;
                string macroLines = string.Empty;
                string xmlDocLines = string.Empty;
                string theRest = string.Empty;
                string multilineComment = string.Empty;
                HashSet<string> usingStatements = new HashSet<string>();
                foreach (var line in source.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    if (isInMultilineComment)
                    {
                        multilineComment += line + Environment.NewLine;
                        if (line.TrimEnd().EndsWith("*/"))
                        {
                            isInMultilineComment = false;
                        }
                        else if (line.TrimEnd().Contains("*/"))
                        {
                            throw new Exception("Not supported");
                        }
                    }
                    else if (line.TrimStart().StartsWith("using") || 
                        line.Contains("CDATA"))
                    {
                        if (line.EndsWith(";"))
                        {
                            usingStatements.Add(line.Substring(0, line.Length-1).TrimStart());
                        }
                        else
                        {
                            usingStatements.Add(line.TrimStart());
                        }
                    }
                    else if (line.StartsWith("#"))
                    {
                        macroLines += line + Environment.NewLine;
                    }
                    else if (line.TrimStart().StartsWith("///"))
                    {
                        xmlDocLines += line.TrimStart() + Environment.NewLine;
                    }
                    else if (line.TrimStart().StartsWith("/*"))
                    {
                        multilineComment += line + Environment.NewLine;
                        isInMultilineComment = true;
                    }
                    else if (line.Length > 0 || isBody)
                    {
                        if (line.StartsWith("{"))
                        {
                            isBody = true;
                        }
                        theRest += line + Environment.NewLine;
                    }
                }
                foreach (var line in usingStatements.OrderBy(s => s))
                {
                    if (line.Contains("CDATA"))
                        usingLines += line + Environment.NewLine;
                    else
                        usingLines += line + ";" + Environment.NewLine;
                }
                if (usingLines.ToLowerInvariant().Contains("using"))
                {
                    usingLines += Environment.NewLine;
                }

                string attribs = this.getStartAttributes(theRest);
                string rearrangedAttribs = string.Empty;
                if (attribs.Length > 0)
                {
                    rearrangedAttribs = this.getRearrangedAttributes(theRest);

                    if (rearrangedAttribs == attribs)
                    {
                        // Nothing to do
                        rearrangedAttribs = string.Empty;
                    }
                    else
                    {
                        theRest = theRest.Replace(attribs, "");
                    }
                }

                string newSource = usingLines.TrimStart() + macroLines.TrimStart() + multilineComment.TrimStart()+ xmlDocLines.TrimStart() + rearrangedAttribs + theRest.Trim();
                _input = _input.Replace(source, newSource);
                Hits++;
                
                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }

    
        private string getLineAtPos(string source, int pos)
        {
            int pos2 = source.LastIndexOf(Environment.NewLine, pos) + 1;
            return source.Substring(pos2, pos - pos2);
        }

    }
}
