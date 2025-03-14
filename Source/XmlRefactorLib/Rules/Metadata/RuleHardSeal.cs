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
    class RuleEnumHardSeal : Rule
    {
        public override string RuleName()
        {
            return "Hard seal";
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
            xpoMatch.AddXMLStart("AxModelInfo");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("AxModelInfo");
        }
        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        private int insertCustomizationAt(string input)
        {
            int pos = input.IndexOf("<Description>");
            if (pos > 0)
            {
                return pos;
            }
            
            pos = input.IndexOf("<DisplayName>");
            if (pos > 0)
            {
                return pos;
            }

            return 0;
        }

        public string Run(string input, int startAt = 0)
        {
            if (!Scanner.FILENAME.Contains("Descriptor"))
                return input;

            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                string xml = input;
                string customization = MetaData.extractFromXML(xml, "//AxModelInfo/Customization");
                string locked = MetaData.extractFromXML(xml, "//AxModelInfo/Locked");
                string updatedText = "";

                if (customization == "")
                {
                    int pos = this.insertCustomizationAt(input);
                    updatedText = input.Insert(pos, "<Customization>DoNotAllow</Customization>\r\n  ");
                    input = updatedText;
                }
                else if (customization != "DoNotAllow")
                {
                    string originalText = input;
                    int startPos = originalText.IndexOf("<Customization>");
                    int endPos = originalText.IndexOf("</Customization>") + "</Customization>".Length;

                    updatedText = originalText.Remove(startPos, endPos - startPos);
                    updatedText = updatedText.Insert(startPos, "<Customization>DoNotAllow</Customization>");

                    input = updatedText;
                }

                if (locked != "")
                {
                    string originalText = input;
                    int startPos = originalText.IndexOf("<Locked>");
                    int endPos = originalText.IndexOf("</Locked>") + "</Locked>".Length;
                    startPos = originalText.LastIndexOf("\n", startPos);
                    endPos = originalText.IndexOf("\n", endPos);

                    updatedText = originalText.Remove(startPos, endPos - startPos);
                }

                if (updatedText != "")
                {
                    Hits++;
                    return updatedText;
                }

            }

            return input;
        }
    }
}
