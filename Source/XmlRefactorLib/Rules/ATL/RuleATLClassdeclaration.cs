using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleATLClassdeclaration : Rule
    {
        HashSet<string> classMethodHashSet = new HashSet<string>();
        public RuleATLClassdeclaration()
        {
        }

        public override string RuleName()
        {
            return "Update class declarations";
        }

        public override bool Enabled()
        {
            return false;
        }
        override public string Grouping()
        {
            return "ATL";
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


        protected string doAtlData(string stringToUpdate)
        {
            var updatedString = stringToUpdate;
            if (Scanner.FILENAME.ToLowerInvariant().Contains("atldata"))
            {
                if (Scanner.FILENAME.ToLowerInvariant().Contains("atldatawhs.xml") ||
                    Scanner.FILENAME.ToLowerInvariant().Contains("atldataassetjournals.xml"))
                {
                    updatedString = this.makePublic(stringToUpdate);
                }
                else
                {
                    updatedString = this.makePublicFinal(stringToUpdate);
                }
                updatedString = this.makeConstsPrivate(updatedString);
                updatedString = this.makeMembersPrivate(updatedString);
            }                

            return updatedString;
        }


        protected string doAtlCommandAndCreator(string stringToUpdate, string _input)
        {
            var updatedString = stringToUpdate;
            if (Scanner.FILENAME.ToLowerInvariant().Contains("atlcommand") ||
                Scanner.FILENAME.ToLowerInvariant().Contains("atlcreator") )
            {
                updatedString = this.makePublicFinal(stringToUpdate, true);
                updatedString = this.makeConstsPrivate(updatedString);
                updatedString = this.makeMembersPrivate(updatedString);                
            }

            return updatedString;
        }

            protected string doAtlSpec(string stringToUpdate, string _input)
        {
            var updatedString = stringToUpdate;
            if (Scanner.FILENAME.ToLowerInvariant().Contains("atlspec"))
            {
                updatedString = this.makePublicFinal(stringToUpdate, false);
                updatedString = this.makeConstsPrivate(updatedString);
                updatedString = this.makeMembersPrivate(updatedString);
                if (!updatedString.Contains("AtlGenerateSpecTable"))
                {
                    Regex reg = new Regex(@"return\W*tablenum\(([\w]+?)\)", RegexOptions.IgnoreCase);
                    Match match = reg.Match(_input);
                    if (match.Success)
                    {
                        string tableName = match.Groups[1].Value;
                        updatedString = this.appendAttribute(updatedString, "AtlGenerateSpecTable(tablestr(" + tableName + "))", true);
                    }
                }
            }

            return updatedString;
        }
        protected string doAtlQuery(string stringToUpdate, string _input)
        {
            var updatedString = stringToUpdate;

            if (Scanner.FILENAME.ToLowerInvariant().Contains("atlquery"))
            {
                updatedString = this.makePublicFinal(stringToUpdate);
                updatedString = this.makeConstsPrivate(updatedString);
                updatedString = this.makeMembersPrivate(updatedString);

                if (!updatedString.Contains("AtlGenerateQueryTable"))
                {
                    Regex reg = new Regex(@"return\W*tablenum\(([\w]+?)\)", RegexOptions.IgnoreCase);
                    Match match = reg.Match(_input);
                    if (match.Success)
                    {
                        string tableName = match.Groups[1].Value;
                        updatedString = this.appendAttribute(updatedString, "AtlGenerateQueryTable(tablestr(" + tableName + "))", true);
                    }
                }
            }

            return updatedString;
        }
        protected string doAtlEntity(string stringToUpdate, string _input)
        {
            var updatedString = stringToUpdate;

            if (Scanner.FILENAME.ToLowerInvariant().Contains("atlentity") &&
                !Scanner.FILENAME.ToLowerInvariant().Contains("atlentityloadline"))
            {
                updatedString = this.fixNewLineAfterClass(stringToUpdate);
                updatedString = this.makePublicFinal(updatedString);
                updatedString = this.makeConstsPrivate(updatedString);
                updatedString = this.makeMembersPrivate(updatedString);

                if (!updatedString.Contains("AtlGenerateEntityTable"))
                {
                    Regex reg = new Regex(@"return\W*tablenum\(([\w]+?)\)", RegexOptions.IgnoreCase);
                    Match match = reg.Match(_input);
                    if (match.Success)
                    {
                        string tableName = match.Groups[1].Value;
                        updatedString = this.appendAttribute(updatedString, "AtlGenerateEntityTable(tablestr(" + tableName + "))", true);
                    }
                }
            }

            return updatedString;
        }
        public string Run(string _input, int _startAt = 0)
        {
            //if (!_input.Contains("class "))
            //    return _input;

            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success)
            {

                string xml = _input;
                var stringToUpdate = match.Value;
                var updatedString = stringToUpdate;
                if (!Scanner.FILENAME.ToLowerInvariant().Contains("_extension"))
                {
                    updatedString = this.doAtlData(updatedString);
                    updatedString = this.doAtlSpec(updatedString, _input);
                    updatedString = this.doAtlQuery(updatedString, _input);
                    updatedString = this.doAtlEntity(updatedString, _input);
                    updatedString = this.doAtlCommandAndCreator(updatedString, _input);
                }
                /*
                if (Scanner.FILENAME.ToLowerInvariant().Contains("atlentity") &&
                    Scanner.FILENAME.ToLowerInvariant().Contains("_extension"))
                {
                    updatedString = this.makePublicFinal(stringToUpdate);
                    updatedString = this.makeConstsPrivate(updatedString);
                    updatedString = this.makeMembersPrivate(updatedString);
                    if (!updatedString.Contains("ExtensionOf"))
                    {
                        Regex reg = new Regex(@"class\W*([\w]+?)\W*_Extension", RegexOptions.IgnoreCase);
                        match = reg.Match(updatedString);
                        if (match.Success)
                        {
                            string entityName = match.Groups[1].Value;
                            updatedString = this.appendAttribute(updatedString, "ExtensionOf(dataentityviewstr("+entityName+"))", false);
                        }
                    }
                }
                */
                if (updatedString != stringToUpdate)
                {
                    Hits++;
                    _input = _input.Replace(stringToUpdate, updatedString);
                }
            }

            return _input;
        }

        private string makeMembersPrivate(string stringToUpdate)
        {
            string[] lines = stringToUpdate.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            Boolean passedSignature = false;

            for (int i = 1; i < lines.Length; i++)  //Skip first line
            {
                string line = lines[i];

                if (line.Trim() == "{")
                {
                    passedSignature = true;
                }
                if (line.Trim() == "}")
                {
                    break;
                }

                if (passedSignature &&
                    line.Trim().Length > 10 &&
                    !line.TrimStart().Contains("private ") &&
                    !line.TrimStart().Contains("protected ") &&
                    !line.TrimStart().Contains("public ") &&
                    !line.TrimStart().StartsWith("//") &&
                    !line.TrimStart().StartsWith("#") &&
                    !line.TrimStart().StartsWith("/*"))
                {
                    int pos = line.Length - line.TrimStart().Length;
                    lines[i] = line.Insert(pos, "private ");
                }
            }

            stringToUpdate = string.Join(Environment.NewLine, lines);

            return stringToUpdate;
        }

        private string makeConstsPrivate(string stringToUpdate)
        {
            stringToUpdate = stringToUpdate.Replace("const ", "private const ");
            stringToUpdate = stringToUpdate.Replace("private private const", "private const");
            stringToUpdate = stringToUpdate.Replace("protected private const", "protected const");
            stringToUpdate = stringToUpdate.Replace("public private const", "public const");
            return stringToUpdate;
        }
        private string makePublic(string stringToUpdate)
        {
            Regex reg = new Regex(@"class\W*([\w]+?)\W*extends\W*Atl", RegexOptions.IgnoreCase);
            Match match = reg.Match(stringToUpdate);
            if (match.Success)
            {
                int lineStart = stringToUpdate.LastIndexOf(Environment.NewLine, match.Index);
                string line = stringToUpdate.Substring(lineStart, match.Index - lineStart);

                if (!line.Contains("public"))
                    stringToUpdate = stringToUpdate.Insert(match.Index, "public ");
            }
            return stringToUpdate;
        }

        private string makePublicFinal(string stringToUpdate, bool mustExtend = true)
        {
            string r = @"class\W*([\w]+?)\W*extends\W*Atl";
            if (!mustExtend)
                r = @"class\W*([\w]+?)\W*";

            Regex reg = new Regex(r, RegexOptions.IgnoreCase);
            Match match = reg.Match(stringToUpdate);
            if (match.Success)
            {
                int lineStart = stringToUpdate.LastIndexOf(Environment.NewLine, match.Index);
                string line = stringToUpdate.Substring(lineStart, match.Index - lineStart);

                if (!line.Contains("final") && !line.Contains("abstract"))
                    stringToUpdate = stringToUpdate.Insert(match.Index, "final ");

                if (!line.Contains("public"))
                    stringToUpdate = stringToUpdate.Insert(match.Index, "public ");
            }
            return stringToUpdate;
        }

        private string fixNewLineAfterClass(string stringToUpdate)
        {
            string className = Path.GetFileNameWithoutExtension(Scanner.FILENAME);
            string r = @"class\W*?\n\W*?" + className;

            Regex reg = new Regex(r, RegexOptions.IgnoreCase);
            Match match = reg.Match(stringToUpdate);
            if (match.Success)
            {
                string line = match.Groups[0].Value;
                line = line.Replace("\n", " ");
                line = line.Replace("\r", "");
                stringToUpdate = stringToUpdate.Replace(match.Groups[0].Value, line);
/*                int lineStart = stringToUpdate.LastIndexOf(Environment.NewLine, match.Index);
                string line = stringToUpdate.Substring(lineStart, match.Index - lineStart);

                if (!line.Contains("final") && !line.Contains("abstract"))
                    stringToUpdate = stringToUpdate.Insert(match.Index, "final ");

                if (!line.Contains("public"))
                    stringToUpdate = stringToUpdate.Insert(match.Index, "public ");
  */
            }
            return stringToUpdate;
        }


        private string appendAttribute(string source, string _attribute, bool mustExtend = true)
        {
            if (source.Contains(_attribute))
            {
                return source;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, mustExtend) + 1;
            if (signatureLinePosInSource > 0)
            {
                string theline = source.Substring(signatureLinePosInSource);
                int attributeEndPosInSource = this.attributeEndPos(source, signatureLinePosInSource);

                if (attributeEndPosInSource > 0)
                {
                    source = source.Insert(attributeEndPosInSource, ", " + _attribute);
                }
                else
                {
                    if (theline.StartsWith("\n"))
                        signatureLinePosInSource++;
                    theline = theline.Replace("\n", "").Replace("\t", "    ");
                    int spaces = theline.Length - theline.TrimStart().Length;
                    string spaceStr = spaces > 0 ? " ".PadLeft(spaces) : "";
                    source = source.Insert(signatureLinePosInSource, spaceStr + "[" + _attribute + "]" + Environment.NewLine);
                }


            }
            return source;
        }
        private string getLineAtPos(string source, int pos)
        {
            int pos2 = source.LastIndexOf(Environment.NewLine, pos) + 1;
            return source.Substring(pos2, pos - pos2);
        }
        private int attributeEndPos(string source, int posOfSignatureLine)
        {
            int startPos = posOfSignatureLine;
            string potentialLine = string.Empty;
            int pos = 0;
            do
            {
                pos = source.LastIndexOf("]", startPos);
                if (pos > 0)
                {
                    potentialLine = this.getLineAtPos(source, pos).TrimStart();
                    startPos = pos - 1;
                }
            }
            while (pos > 0 && potentialLine.StartsWith("//"));

            return pos;
        }
        private int signatureLineStartPos(string source, bool mustExtend)
        {
            Regex reg;
            
            if (mustExtend)
                reg = new Regex(@"class\W*([\w]+?)\W*extends\W*Atl", RegexOptions.IgnoreCase);
            else
                reg = new Regex(@"class\W*([\w]+?)\W*_Extension", RegexOptions.IgnoreCase);

            Match match = reg.Match(source);
            int lineStart = source.LastIndexOf(Environment.NewLine, match.Index);

            return lineStart;
        }

     
    }
}
