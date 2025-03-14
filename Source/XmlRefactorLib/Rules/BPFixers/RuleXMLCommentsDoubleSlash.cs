using System;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleXMLCommentsDoubleSlash : Rule
    {
        const string deleteMarker = "SomethingRareThatDoesn'tExistInSourceCodeAlready";

        public override string RuleName()
        {
            return "XMLCommentsCleanUp()";
        }
        override public string Grouping()
        {
            return "XML Comments // -> ///";
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

        
        private string processSource(string _input, string _methodString)
        {
            //      if (Scanner.FILENAME.ToLowerInvariant().Contains("axpurchline_in") && _methodString.Contains("parmMaximumRetailPrice_IN"))
            //      {
            //          int i = 0;
            //      }
            string originalMethodString = _methodString;

            Boolean isXmlDoc = _methodString.IndexOf("<summary>") > 0;
            if (isXmlDoc)
            {
                string[] lines = _methodString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                if (lines[0].Contains("///"))
                    return _input;

                Boolean firstReplacement = true;
                Boolean insideTags = false;

                for (int i = 1; i < lines.Length; i++)  //Skip first line
                {
                    string line = lines[i];

                    if (line.TrimStart().StartsWith(@"///"))
                    {
                        break;
                    }
                    if (line.TrimStart().StartsWith(@"//"))
                    {
                        if (firstReplacement)
                        {
                            if (!line.Contains("<summary>"))
                                return _input;
                            firstReplacement = false;
                        }

                        if (insideTags == false && !line.Contains("<"))
                        {
                            break;
                        }

                        lines[i] = line.Replace("    //", "    ///");
                        if (i < lines.Length)
                        {
                            string nextLine = lines[i + 1];
                            int padding = nextLine.Length - nextLine.TrimStart().Length;
                            lines[1] = " ".PadLeft(padding)+lines[1].TrimStart();
                        }

                        insideTags = !line.Contains("</");                             
                    }
                    else if (line.Trim() != String.Empty)
                    {
                        break;
                    }
                }
                _methodString = string.Join(Environment.NewLine, lines);
            }

            if (_methodString != originalMethodString && this.isValid(_methodString))
            {
                Hits++;
                _input = _input.Replace(originalMethodString, _methodString);
            }
            return _input;
        }
    }
}
