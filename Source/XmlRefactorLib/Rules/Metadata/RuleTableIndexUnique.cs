using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace XmlRefactor
{
    class RuleTableIndexUnique : Rule
    {
        public override string RuleName()
        {
            return "Table index unique";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "Metadata";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("AxTableIndex", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("AxTableIndex");
        }
        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        public string Run(string input, int startAt = 0)
        {
            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                string xml = match.Value;

                if (!xml.Contains("<AllowDuplicates>"))
                {
                    string indexName = MetaData.extractFromXML(xml, "//AxTableIndex/Name");
                    Debug.WriteLine(string.Format("{0}.{1}", Scanner.FILENAME, indexName));
                    Hits++;
                }
                return this.Run(input, match.Index + match.Length);
            }

            return input;
        }
    }
}
