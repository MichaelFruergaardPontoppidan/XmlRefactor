using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace XmlRefactor
{
    class RuleFieldAndFieldGroupsSizeToContent : Rule
    {
        public override string RuleName()
        {
            return "FieldAndFieldGroups -> WidthMode/HeightMode = SizeToContent";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "FormPattern";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddLiteral("FieldsFieldGroups");
        }
        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        private void handleScope(XmlNode scopeControl, string xPath, string name)
        {
            XmlNode child = scopeControl.SelectSingleNode(xPath);

            if (child == null)
                return;
            

            if (child.ParentNode == null)
                return;

            XmlNode parentControl = child.ParentNode.ParentNode;

            
            
            if (parentControl != null &&
                parentControl.SelectSingleNode("Type[.=\"ReferenceGroup\"]") != null)
            {
                return;
            }
            
            XmlNode toDelete;
            toDelete = child.SelectSingleNode(name);
            child.RemoveChild(toDelete);
            Hits++;
            
        }

        public string Run(string input, int startAt = 0)
        {
            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                try
                {
                    doc.LoadXml(input);

                    XmlNodeList parentControls = doc.SelectNodes("//AxFormControl[Pattern = \"FieldsFieldGroups\"]");

                    if (parentControls != null)
                    {
                        foreach (XmlNode scopeControl in parentControls)
                        {
                            int h = Hits;
                            this.handleScope(scopeControl, ".//AxFormControl[WidthMode]", "WidthMode");
                            this.handleScope(scopeControl, ".//AxFormControl[HeightMode]", "HeightMode");

                            if (h != Hits)
                            {
                                string updatedInput = doc.InnerXml;

                                return this.Run(updatedInput);
                            }
                        }
                    }
                }
                catch (Exception e)
                {

                }


            }

            return input;
        }
    }
}
