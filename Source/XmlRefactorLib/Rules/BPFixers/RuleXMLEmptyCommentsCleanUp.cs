using System;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleXMLEmptyCommentsCleanUp : Rule
    {
        const string deleteMarker = "SomethingRareThatDoesn'tExistInSourceCodeAlready";

        public override string RuleName()
        {
            return "Remove empty XML doc";
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


        private string processSource(string _input, string _methodString)
        {
            if (_methodString.Contains("returns type="))
                return _input;
            //      if (Scanner.FILENAME.ToLowerInvariant().Contains("axpurchline_in") && _methodString.Contains("parmMaximumRetailPrice_IN"))
            //      {
            //          int i = 0;
            //      }
            string originalMethodString = _methodString;

            Boolean isXmlDoc = _methodString.IndexOf("</summary>") > 0 ||
                               _methodString.IndexOf("<summary>") > 0;
            if (isXmlDoc)
            {
                string[] lines = _methodString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                if (lines[0].Contains("///"))
                    return _input;

                for (int i = 1; i < lines.Length; i++)  //Skip first line
                {
                    string line = lines[i];

                    bool isTrippleComment = line.TrimStart().StartsWith(@"///");
                    
                    if (!isTrippleComment)
                    {
                        if (line.TrimStart().StartsWith(@"//"))
                            return _input;
                        if (line.Trim() == String.Empty)
                            return _input;
                        if (line.TrimStart().StartsWith(@"/*"))
                            return _input;
                        break;
                    }
                    string lineTrimmed = line.Replace(" ", "").
                        Replace("<summary>", "").
                        Replace("</summary>", "").
                        Replace("<remarks>", "").
                        Replace("</remarks>", "").
                        Replace("<returns>", "").
                        Replace("</returns>", "").
                        Replace("///", "");

                    if (lineTrimmed == string.Empty)
                    {
                        lines[i] = deleteMarker;
                    }
                    else if (lineTrimmed.StartsWith("<param"))
                    {
                        int pos = lineTrimmed.IndexOf(">");
                        if (pos < 1)
                            return _input;
                        lineTrimmed = lineTrimmed.Substring(pos+1);
                        if (lineTrimmed == "</param>")
                        {
                            lines[i] = deleteMarker;
                        }
                        else
                        {
                            return _input;
                        }
                    }
                    else
                    {
                        return _input;
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
