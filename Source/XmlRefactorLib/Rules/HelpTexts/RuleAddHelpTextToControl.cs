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
    class RuleAddHelpTextToControl : Rule
    {
        HashSet<string> labelsToAdd = new HashSet<string>();

        public RuleAddHelpTextToControl()
        {
            this.initLabels(@"../../../XmlRefactorLib/RulesInput/HelpTextsToAdd.txt");
        }

        public override string RuleName()
        {
            return "Add help text to controls";
        }

        public override bool Enabled()
        {
            return false;
        }
        private void initLabels(string file)
        {
            var stringArray = File.ReadAllLines(file);
            foreach (string item in stringArray)
            {
                var formName = new string(item.TakeWhile((c) => c != ';').ToArray());
                var controlName = new string(item.Remove(0, formName.Length + 1).TakeWhile((c) => c != ';').ToArray());

                labelsToAdd.Add(string.Format("{1}_{0}", controlName, formName));
            }
        }
        public override string Grouping()
        {
            return "Help texts";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("AxFormControl");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("AxFormControl");
        }
        public override string Run(string input)
        {

            return this.Run(input, 0);
        }

        private int insertAt(string input)
        {
            string[] previousAttributes = { "HeightMode", "Height", "FilterField", "FilterExpression", "FilterDataSource", "ExtendedStyle",
                "Enabled","EnableFormRef","DragDrop","CountryRegionCodes","ConfigurationKey","AutoDeclaration","AllowEdit","Name" };

            foreach (var attr in previousAttributes)
            {
                int pos = input.IndexOf("<"+attr+">");

                if (pos > 0)
                {
                    return input.IndexOf("\n", pos);
                }
            }           

            return 0;
        }

        public string Run(string input, int startAt = 0)
        {
            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                string xml = match.Value;
                
                if (xml.Substring(5).Contains("<AxFormControl"))
                {
                    // It contains sub controls, skip
                    return this.Run(input, match.Index + 1);
                }

                string controlName = MetaData.extractNextXMLElement("Name", 0, xml);
                string formName = MetaData.extractNextXMLElement("Name", 0, input);
                string labelId = string.Format("{1}_{0}", controlName, formName);

                if (labelsToAdd.Contains(labelId))
                {
                    int pos = insertAt(xml);

                    if (pos > 0)
                    {
                        int endPos = xml.IndexOf("<Name>");
                        int startPos = xml.Substring(0, endPos).LastIndexOf(">");
                        string whiteSpace = xml.Substring(startPos+1, endPos - startPos-1);

                        string toInsert = whiteSpace + "<HelpText>@FieldDescriptions_WHS:" + labelId + "</HelpText>";
                        string updatedXml = xml.Insert(pos, toInsert);
                        string updatedText = input.Replace(xml, updatedXml);
                        Debug.WriteLine(labelId);
                        Hits++;
                        return this.Run(updatedText, match.Index + 1);
                    }
                }
                else
                {
                    return this.Run(input, match.Index + 1);
                }                
            }

            return input;
        }
    }
}
