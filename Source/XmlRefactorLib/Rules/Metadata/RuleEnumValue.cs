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
    class RuleEnumValue2Name : Rule
    {
        public override string RuleName()
        {
            return "EnumValue -> EnumName";
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
            xpoMatch.AddXMLStart("AxTableRelationConstraint");
            //xpoMatch.AddXMLStartTag();
            //xpoMatch.AddLiteral("AxTableRelationConstraint");
            //xpoMatch.AddCaptureAnything();
            //xpoMatch.AddLiteral("AxTableRelationConstraintFixed");
            //xpoMatch.AddCaptureAnything();
            //xpoMatch.AddXMLEndTag();

            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("AxTableRelationConstraint");
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

                if (!xml.Contains("<ValueStr>") &&
                    (xml.Contains("AxTableRelationConstraintFixed") ||
                     xml.Contains("AxTableRelationConstraintRelatedFixed")))
                {

                    //            <AxTableRelationConstraint xmlns=""
                    //					i:type="AxTableRelationConstraintFixed">
                    //					<Name>InventAccountType</Name>
                    //					<Field>InventAccountType</Field>
                    //					<Value>21</Value>
                    //					<ValueStr>InventAccountType::SalesIssue</ValueStr>
                    //				</AxTableRelationConstraint>

                    int startPos = xml.IndexOf("xmlns")-1;
                    int endPos = xml.IndexOf(">");
                    xml = xml.Remove(startPos, endPos-startPos);  //Remove the i:type...
                    
                    string fieldName = MetaData.extractFromXML(xml, "//AxTableRelationConstraint/Field");

                    string value = MetaData.extractFromXML(xml, "//AxTableRelationConstraint/Value");
                    if (value == String.Empty)
                    {
                        //default
                        value = "0";
                    }
                    string enumName = "";
                    string token = "Field";
                    if (fieldName != "")
                    {
                        enumName = MetaData.extractFromXML(input, "//AxTable/Fields/AxTableField[Name=\"" + fieldName + "\"]/EnumType");
                    }
                    else
                    {
                        string relatedFieldName = MetaData.extractFromXML(xml, "//AxTableRelationConstraint/RelatedField");
                        string sub = input.Substring(1, match.Index);
                        int pos = sub.LastIndexOf("<RelatedTable>") + "<RelatedTable>".Length;
                        int pos2 = sub.IndexOf("</RelatedTable>", pos);
                        string relatedTableName = sub.Substring(pos, pos2-pos);
                        fieldName = relatedTableName+"."+relatedFieldName;
                        enumName = MetaData.getEnumFromTable(relatedTableName, relatedFieldName);
                        token = "RelatedField";
                    }
                    if (enumName != "")
                    {
                        string symbol = MetaData.getSymbolFromEnum(enumName, value);
                        if (symbol != "")
                        {
                            string originalText = match.Value;
                            startPos = originalText.IndexOf("<Value>");
                            endPos = originalText.IndexOf("</Value>") + "</Value>".Length;
                            string updatedText;
                            if (startPos != -1)
                            {
                                updatedText = originalText.Remove(startPos, endPos - startPos);
                            }
                            else
                            {
                                startPos = originalText.IndexOf("</Name>") + "</Name>".Length;
                                endPos = originalText.IndexOf("<"+token+">");
                                string whiteSpace = originalText.Substring(startPos, endPos - startPos);
                                int iPos = whiteSpace.IndexOf("<");
                                if (iPos != -1)
                                {
                                    whiteSpace = whiteSpace.Substring(1, iPos - 1);
                                }
                                startPos = originalText.IndexOf("</"+token+">") + ("</"+token+">").Length;
                                updatedText = originalText.Insert(startPos, whiteSpace);
                                startPos += whiteSpace.Length;
                            }
                            updatedText = updatedText.Insert(startPos, "<ValueStr>" + enumName + "::" + symbol + "</ValueStr>");

                            string updatedInput = input.Remove(match.Index, match.Length);
                            updatedInput = updatedInput.Insert(match.Index, updatedText);
                            Hits++;
                            return this.Run(updatedInput, match.Index + match.Length);
                        }
                        else
                        {
                            if (enumName != "NoYes" &&
                                enumName != "Boolean")
                            {
                                Debug.WriteLine(string.Format("{0} references non existing value {1} in enum {2} via constraint on field {3}", Scanner.FILENAME, value, enumName, fieldName));
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
