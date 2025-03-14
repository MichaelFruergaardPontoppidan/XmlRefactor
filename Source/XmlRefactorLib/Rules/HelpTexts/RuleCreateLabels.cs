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
    class RuleCreateLabels : Rule
    {
        public override string RuleName()
        {
            return "Create labels - must scan TXT files to work";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "Help texts";
        }
        protected override void buildXpoMatch()
        {            
            xpoMatch.AddCaptureAnything();         
        }
        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        public string Run(string input, int startAt = 0)
        {
            if (!Scanner.FILENAME.Contains("FieldDescriptions_WHS.en-US.label.txt"))
                return input;

            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                LabelFile labelFile = new LabelFile(input);

                ReadLabelsToAdd(labelFile);

                //Description text for the VendTransDebit_Voucher control on the AgreementVendTrans_RU form

                string updatedText = labelFile.export();

                if (updatedText != "")
                {
                    Hits++;
                    return updatedText;
                }
            }

            return input;
        }

        private static void ReadLabelsToAdd(LabelFile labelFile)
        {
            var stringArray = File.ReadAllLines(@"../../../XmlRefactorLib/RulesInput/HelpTextsToAdd.txt");
            foreach (string item in stringArray)
            {
                var formName = new string(item.TakeWhile((c) => c != ';').ToArray());
                var controlName = new string(item.Remove(0, formName.Length + 1).TakeWhile((c) => c != ';').ToArray());
                var label = item.Substring(formName.Length + controlName.Length + 2);

                labelFile.addLabel(new Label()
                {
                    ID = string.Format("{1}_{0}", controlName, formName),
                    Text = label,
                    Description = string.Format("Description text for the {0} control on the {1} form", controlName, formName)
                }
                );

            }
        }

    }
}
