using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace XmlRefactor
{
    class RuleCollectFinTagCandidateTables : Rule
    {

        public override string RuleName()
        {
            return "FinTag candidates";
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
            xpoMatch.AddXMLStart("RelatedTable", false);
            xpoMatch.AddLiteral("DimensionAttributeValueSet");
            xpoMatch.AddXMLEnd("RelatedTable");
        }

        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        private string TableName(string xml)
        {
            string name = MetaData.extractFromXML(xml, "//AxTable/Name");
            return name;
        }

        private string TableGroup(string xml)
        {
            string name = MetaData.extractFromXML(xml, "//AxTable/TableGroup");
            if (name == string.Empty)
                name = "Miscellaneous";
            return name;
        }

        private string ConfigKey(string xml)
        {
            string name = MetaData.extractFromXML(xml, "//AxTable/ConfigurationKey");
            return name;
        }

        private string TableType(string xml)
        {
            string name = MetaData.extractFromXML(xml, "//AxTable/TableType");
            if (name == string.Empty)
                name = "Regular";
            return name;
        }        

        public string Run(string _input, int startAt = 0)
        {
            Match match = xpoMatch.Match(_input, startAt);

            if (match.Success)
            {
                string xml = _input;                
                Hits++;
                using (StreamWriter sw = File.AppendText(@"c:\temp\FinTagCandidates.txt"))
                {
                    sw.WriteLine(string.Format("{0};{1};{2};{3}", TableName(xml), TableGroup(xml), ConfigKey(xml), TableType(xml)));
                }

                _input = this.Run(_input.Remove(match.Index, match.Length), match.Index + 1);
            }

            return _input;
        }
    }
}
