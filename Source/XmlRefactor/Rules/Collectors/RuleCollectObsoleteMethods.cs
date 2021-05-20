using System;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleCollectObsoleteMethods : Rule
    {

        public override string RuleName()
        {
            return "Obsolete methods";
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

        public bool include(string source)
        {
            bool errorsOnly = false;
            if (errorsOnly)
            {
                int pos = source.IndexOf("[sysobsolete");
                if (pos>0)
                {
                    int pos2 = source.IndexOf(Environment.NewLine, pos);
                    string line = source.Substring(pos, pos2 - pos).Trim();
                    if (line.Replace(" ", "").EndsWith("true)]"))
                        return true;
                }
            }
            else
            {
                if (source.Contains("[sysobsolete"))
                    return true;
                if (source.Contains("[ sysobsolete"))
                    return true;
            }
            return false;
        }

        public string Run(string _input, int startAt = 0)
        {
            Match match = xpoMatch.Match(_input, startAt);

            if (match.Success)
            {
                string xml = _input;
                if (this.include(match.Value.ToLowerInvariant()))
                {
                    string name = match.Groups[1].Value.Trim();

                    string AOTPath = MetaData.AOTPath(name);
                    Hits++;
                    using (StreamWriter sw = File.AppendText(@"c:\temp\ObsoleteErrorMethods-all.txt"))
                    {
                        sw.WriteLine(AOTPath);
                    }

                    if (!MetaData.isReferencedExternally(name))
                    {
                        using (StreamWriter sw = File.AppendText(@"c:\temp\ObsoleteErrorMethods-unreferenced.txt"))
                        {
                            sw.WriteLine(AOTPath);
                        }
                    }
                    else
                    {
                        using (StreamWriter sw = File.AppendText(@"c:\temp\ObsoleteErrorMethods-referenced.txt"))
                        {
                            sw.WriteLine(AOTPath);
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
