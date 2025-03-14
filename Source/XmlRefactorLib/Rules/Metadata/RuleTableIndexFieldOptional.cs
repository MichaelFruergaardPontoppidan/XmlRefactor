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
    class RuleTableIndexFieldOptional : Rule
    {
        HashSet<string> fieldsToMakeOptional = new HashSet<string>();

        public RuleTableIndexFieldOptional()
        {
            this.initInputs(@"../../../XmlRefactorLib/RulesInput/IndexFieldsToMakeOptional.txt");
        }
        private void initInputs(string file)
        {
            var stringArray = File.ReadAllLines(file);
            foreach (var item in stringArray)
            {
                var item2 = item.Replace("/", "\\").ToLowerInvariant();
                if (!fieldsToMakeOptional.Contains(item2))
                {
                    fieldsToMakeOptional.Add(item2);
                }
            }
        }
        public override string RuleName()
        {
            return "Table index field optional";
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
            xpoMatch.AddXMLStart("AxTableIndexField", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("AxTableIndexField");
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

                if (!xml.Contains("<Optional>"))
                {
                    string fieldName = MetaData.extractFromXML(xml, "//AxTableIndexField/DataField");
                    string indexName = MetaData.extractPreviousXMLElement("Name", match.Index, input);
                    string tableName = MetaData.extractNextXMLElement("Name", 1, input);
                    string token = tableName + "\\" + indexName + "\\" + fieldName;

                    if (fieldsToMakeOptional.Contains(token.ToLowerInvariant()))
                    {
                        if (xml.Contains("<Tags>"))
                            throw new NotSupportedException();
                        if (xml.Contains("<IncludedColumn>"))
                            throw new NotSupportedException();

                        int endpos = xml.IndexOf("</DataField>") + "</DataField>".Length;
                        int startPos = xml.LastIndexOf(Environment.NewLine, endpos);
                        int xmlPos = xml.IndexOf("<DataField>");

                        string updatedInput = input.Insert(match.Index + endpos, xml.Substring(startPos, xmlPos - startPos) + "<Optional>Yes</Optional>");

                        string indexToken = "<Name>" + indexName + "</Name>";
                        int indexPos = updatedInput.IndexOf(indexToken);

                        int endpos2 = updatedInput.IndexOf("</IsSystemGenerated>", indexPos) + "</IsSystemGenerated>".Length;
                        if (endpos2 > indexPos)
                        {
                            int startPos2 = updatedInput.LastIndexOf(Environment.NewLine, endpos2);
                            int xmlPos2 = updatedInput.IndexOf("<IsSystemGenerated>", startPos2);

                            if (!updatedInput.Substring(indexPos, endpos2 - indexPos).Contains("IsManuallyUpdated"))
                            {
                                updatedInput = updatedInput.Insert(xmlPos2, "<IsManuallyUpdated>Yes</IsManuallyUpdated>" + updatedInput.Substring(startPos2, xmlPos2 - startPos2));
                            }
                        }
                        Hits++;
                        return this.Run(updatedInput, match.Index + match.Length);
                    }
                    return this.Run(input, match.Index + match.Length);
                }
            }

            return input;
        }
    }
}
