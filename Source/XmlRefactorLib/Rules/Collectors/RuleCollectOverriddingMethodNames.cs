using System;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleCollectOverriddingMethodNames : Rule
    {

        public override string RuleName()
        {
            return "Overridding Method names";
        }
        override public string Grouping()
        {
            return "Collectors";
        }
        public override bool Enabled()
        {
            return false;
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("Method", false);
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddXMLStart("Name", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Name");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Method");
        }

        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        public bool include(string _input, string methodName)
        {
            if (Scanner.FILENAME.Contains("AxForm"))
                return false;
            if (Scanner.FILENAME.Contains("AxQuery"))
                return false;
            if (Scanner.FILENAME.Contains("_Extension"))
                return false;

            _input = _input.ToLowerInvariant();
            if (_input.Contains("[subscribesto"))
                return false;
            if (_input.Contains("[form"))
                return false;
            if (_input.Contains("[prehandlerfor"))
                return false;
            if (_input.Contains("[posthandlerfor"))
                return false;

            int pos = _input.IndexOf("CData");

            string source = _input.Substring(pos + 6);
            string[] lines = source.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            string firstLine = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == String.Empty ||
                    lines[i].Trim().StartsWith("//"))
                {
                    continue;
                }

                if (!line.Contains("("))
                {
                    continue;
                }

                firstLine = line;
                break;
            }

            if (firstLine.Contains("private ") ||
                firstLine.Contains("static ") ||
                firstLine.Contains("internal "))
            {
                return false;
            }

            if (source.Contains("super("))
                return true;

            if (MetaData.isMethodOverridding(Path.GetFileNameWithoutExtension(Scanner.FILENAME), methodName))
                return true;

            return false;
        }

        public string Run(string _input, int startAt = 0)
        {
            Match match = xpoMatch.Match(_input, startAt);

            if (match.Success)
            {
                string xml = _input;
                string name = match.Groups[1].Value.Trim();
                if (this.include(match.Value, name))
                {

                    if (name.ToLowerInvariant() != "classdeclaration")
                    {
                        string AOTPath = MetaData.AOTPath(name);
                        using (StreamWriter sw = File.AppendText(this.logFileName()))
                        {
                            sw.WriteLine(AOTPath);
                            Hits++;
                        }
                    }
                    //Debug.WriteLine(string.Format("{0} has unknown table range {1} with value {2} on field {3}", Scanner.FILENAME, rangeName, value, fieldName));
                }

                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }
    }
}
