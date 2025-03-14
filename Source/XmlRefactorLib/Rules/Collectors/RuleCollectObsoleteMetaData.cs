using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace XmlRefactor
{
    class RuleCollectObsoleteMetaData : Rule
    {

        public override string RuleName()
        {
            return "Obsolete metadata";
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
            xpoMatch.AddXMLStart("IsObsolete", false);
            xpoMatch.AddLiteral("Yes");
            xpoMatch.AddXMLEnd("IsObsolete");
        }

        public override string Run(string input)
        {
            return this.Run(input, 0);
        }


        private string AOTPath(string xml, int pos)
        {
            string name = MetaData.extractPreviousXMLElement("Name", pos, xml);
            Stack<string> paths = MetaData.XMLPathToFirstToken(xml, "IsObsolete");
            string path = string.Empty;
            var asArray = paths.ToArray();
            for (int i = asArray.Length - 2; i>=0; i--)
            {
                path += "\\" + asArray[i];
            }
            if (path != string.Empty)
                return path+"\\"+name;
            return string.Empty;
        }

        public string Run(string _input, int startAt = 0)
        {
            Match match = xpoMatch.Match(_input, startAt);

            if (match.Success)
            {
                string xml = _input;
                string AOTPath = MetaData.AOTPath("")+this.AOTPath(xml, match.Index);
                Hits++;
                using (StreamWriter sw = File.AppendText(@"c:\temp\ObsoleteMetaData-all.txt"))
                {
                    sw.WriteLine(AOTPath);
                }

                _input = this.Run(_input.Remove(match.Index, match.Length), match.Index + 1);
            }

            return _input;
        }
    }
}
