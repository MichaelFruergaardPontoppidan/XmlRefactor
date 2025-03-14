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
    class RuleAddLink : Rule
    {
        string methodName;

        public override string RuleName()
        {
            return "AddLink() parameters in wrong sequence";
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
            xpoMatch.AddSymbol("."); 
            xpoMatch.AddLiteral("addLink");
            xpoMatch.AddStartParenthesis();
               xpoMatch.AddLiteral("fieldnum");
               xpoMatch.AddStartParenthesis();
               xpoMatch.AddCapture();
               xpoMatch.AddComma();
               xpoMatch.AddCapture();
               xpoMatch.AddEndParenthesis();
               xpoMatch.AddComma();
               xpoMatch.AddWhiteSpace();
               xpoMatch.AddLiteral("fieldnum");
               xpoMatch.AddStartParenthesis();
               xpoMatch.AddCapture();
               xpoMatch.AddComma();
               xpoMatch.AddCapture();
               xpoMatch.AddEndParenthesis();
        }
        public override string Run(string input)
        {
            return this.Run(input, 0);
        }
        public string getMenuItemsAsString(List<string> list)
        {
            string result = string.Empty;
            foreach (var s in list)
            {
                if (result == string.Empty)
                    result = s;
                else
                    result += "," + s;
            }
            return result;
        }

        private void log(string message, string severity = "info")
        {
            using (StreamWriter sw = File.AppendText(this.logFileName())) 
            {
                sw.WriteLine(string.Format("{0};{1};{2};{3}", severity, Scanner.FILENAME, methodName, message));
            }
        }

        public string Run(string input, int startAt = 0)
        {
            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                string xml = match.Value;
                string table1Name = match.Groups[1].Value.Trim();
                string field1Name = match.Groups[2].Value.Trim();
                string table2Name = match.Groups[3].Value.Trim();
                string field2Name = match.Groups[4].Value.Trim();

                methodName = MetaData.extractPreviousXMLElement("Name", match.Index, input);

                if (methodName.ToLowerInvariant() == "init")
                {
                    string dsxml = MetaData.extractPreviousXMLElement("DataSource", match.Index, input);
                    if (dsxml != string.Empty) // Not a datasource method
                    {
                        string dsName = MetaData.extractNextXMLElement("Name", 1, dsxml);
                        if (dsName == table1Name && dsName != table2Name)
                        {
                            string methodxml = MetaData.extractPreviousXMLElement("Source", match.Index, input).ToLowerInvariant();

                            if (!methodxml.Contains(".adddatasource"))
                            {
                                this.log("Possibly wrong order of parameters. " + xml);
                            }
                        }
                    }
                }
                
                return this.Run(input, match.Index + match.Length);

            }

            return input;
        }
    }
}
