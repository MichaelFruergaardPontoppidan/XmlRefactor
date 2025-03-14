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
    class RuleEnumQueryRange : Rule
    {
        readonly char[] matchChars = new char[]{'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'/*, '<', '>', '.'*/};
        public override string RuleName()
        {
            return "Enum Query Range -> Enum Name";
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
            xpoMatch.AddXMLStart("AxQuerySimpleDataSourceRange");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("AxQuerySimpleDataSourceRange");
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

                if (xml.Contains("<Value>") &&
                    xml.Contains("<Field>"))
                {

                    //		<AxQuerySimpleDataSourceRange>
					//			<Name>NamedCategoryHierarchyRoleRetail</Name>
					//			<Field>NamedCategoryHierarchyRole</Field>
					//			<Value>4</Value>
					//		</AxQuerySimpleDataSourceRange>

                    string value = MetaData.extractFromXML(xml, "//AxQuerySimpleDataSourceRange/Value");

                    if (value.IndexOfAny(matchChars) != -1 &&
                        !value.Contains("(") &&
                        !value.Contains(">") &&
                        !value.Contains("<") &&
                        !value.Contains("..") &&
                        !value.Contains("!"))
                    {
                        string fieldName = MetaData.extractFromXML(xml, "//AxQuerySimpleDataSourceRange/Field");
                        string rangeName = MetaData.extractFromXML(xml, "//AxQuerySimpleDataSourceRange/Name");

                        string tableName = MetaData.getTableNameFromQueryRange(input, rangeName);

                        if (tableName == String.Empty)
                        {
                            Debug.WriteLine(string.Format("{0} has unknown table range {1} with value {2} on field {3}", Scanner.FILENAME, rangeName, value, fieldName));
                        }
                        string enumName = MetaData.getEnumFromTable(tableName, fieldName);

                        if (enumName != String.Empty &&
                            enumName.ToLowerInvariant() != "Boolean".ToLowerInvariant() &&
                            enumName.ToLowerInvariant() != "NoYes".ToLowerInvariant() &&
                            enumName.ToLowerInvariant() != "NoYesId".ToLowerInvariant())
                        {
                            string symbols = string.Empty;

                            foreach (var v in value.Split(','))
                            {
                                string symbol = MetaData.getSymbolFromEnum(enumName, v.Trim());

                                if (symbol == string.Empty)
                                {
//                                    Debug.WriteLine(string.Format("{0} is referencing unknown enum value. Enum={1} with value {2}", Scanner.FILENAME, enumName, v));
                                    break;
                                }
                                Debug.WriteLine(string.Format("{0};{1};{2}", enumName, v, symbol));

                                if (symbols != string.Empty)
                                    symbols += ", "+symbol;
                                else
                                    symbols = symbol;
                            }
                            if (symbols != "")
                            {
                                string originalText = match.Value;
                                int startPos = originalText.IndexOf("<Value>");
                                int endPos = originalText.IndexOf("</Value>") + "</Value>".Length;
                                string updatedText;
                                updatedText = originalText.Remove(startPos, endPos - startPos);
                                updatedText = updatedText.Insert(startPos, "<Value>" + symbols + "</Value>");

                                string updatedInput = input.Remove(match.Index, match.Length);
                                updatedInput = updatedInput.Insert(match.Index, updatedText);
                                Hits++;
                                return this.Run(updatedInput, match.Index + match.Length);
                            }
                            else
                            {
                                //Debug.WriteLine(string.Format("{0} has integer based range {1} on enum {2} on field {3}.{4}", Scanner.FILENAME, value, enumName, tableName, fieldName));
                              //  Debug.WriteLine(string.Format("{0};{1};{2};{3};{4}", Scanner.FILENAME, value, enumName, tableName, fieldName));
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
