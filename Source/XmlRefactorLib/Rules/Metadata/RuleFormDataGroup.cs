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
    class RuleFormDataGroup : Rule
    {
        public override string RuleName()
        {
            return "Form control not bound to data group";
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
            xpoMatch.AddXMLStart("DataField", false);
            xpoMatch.AddLiteral("InventColorId");
            xpoMatch.AddXMLEnd("DataField");
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
                int designPos = input.IndexOf("<Design>");
                if (designPos < match.Index)
                {
                    string dataGroup = MetaData.extractNextXMLElement("DataGroup", match.Index, input);

                    if (dataGroup != "InventoryDimensions" &&
                        dataGroup != "ProductDimensions"
                        )
                    {
                        Debug.WriteLine(string.Format("{0} Result={1}  DataGroup = {2}", Scanner.FILENAME, match.Value, dataGroup));
                        Hits++;
                    }

                }
                return this.Run(input, match.Index + match.Length);
            }

            return input;
        }
    }
}
