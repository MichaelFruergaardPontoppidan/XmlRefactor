using System;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleCollectObsoleteClasses : Rule
    {

        public override string RuleName()
        {
            return "Obsolete classes";
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
            xpoMatch.AddXMLStart("Declaration", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Declaration");
        }

        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        public bool include(string source)
        {
            if (source.Contains("[sysobsolete"))
                return true;
            if (source.Contains("[ sysobsolete"))
                return true;
            return false;
        }

        public string Run(string _input, int startAt = 0)
        {
            Match match = xpoMatch.Match(_input, startAt);
            //NOTE: Classes are also captured by the obsolete metadata rule
            if (match.Success)
            {
                string xml = _input;
                if (this.include(match.Value.ToLowerInvariant()))
                {
                    string AOTPath = MetaData.AOTPath("");
                    Hits++;
                    using (StreamWriter sw = File.AppendText(@"c:\temp\ObsoleteClasses-all.txt"))
                    {
                        sw.WriteLine(AOTPath);
                    }

                    if (!MetaData.isReferencedExternally(""))
                    {
                        using (StreamWriter sw = File.AppendText(@"c:\temp\ObsoleteClasses-unreferenced.txt"))
                        {
                            sw.WriteLine(AOTPath);
                        }
                    }
                    else
                    {
                        using (StreamWriter sw = File.AppendText(@"c:\temp\ObsoleteClasses-referenced.txt"))
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
