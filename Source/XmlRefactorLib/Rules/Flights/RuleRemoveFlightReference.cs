using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace XmlRefactor
{
    class RuleRemoveFlightReference : Rule
    {
        private string flightToRemove = String.Empty;

        public RuleRemoveFlightReference()
        {
        }

        public override string RuleName()
        {
            return "Remove flight references";
        }

        public override bool Enabled()
        {
            return true;
        }

        override public string Grouping()
        {
            return "Flights";
        }
        override public bool IsXppRule()
        {
            return false;
        }

        protected override void buildXpoMatch()
        {
            /*
            xpoMatch.AddXMLStart("Source", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Source");
            */
            xpoMatch.AddLiteral(flightToRemove);
        }

        public override string Run(string _input)
        {
            return this.Run(_input, 0);
        }

        public override string mustContain()
        {
            if (flightToRemove == string.Empty)
            {
                flightToRemove = this.getFlightToRemove();
            }

            return flightToRemove;
        }

        private string getFlightToRemove()
        { 
            if (Settings.RuleParameter != String.Empty)
                return Settings.RuleParameter;

            return "WHSUseReturnDetailConfigurationProviderFlight"; 
        }


        private string rewriteSource(string sourceCode)
        {
            string flightEnabledCall = flightToRemove + "::instance().isenabled()";
            if (sourceCode.IndexOf(flightEnabledCall, StringComparison.OrdinalIgnoreCase) > 0)
            {
                string sourceCodeToRefactor = Regex.Replace(sourceCode, flightEnabledCall.Replace("(", "\\(").Replace(")", "\\)"), "true", RegexOptions.IgnoreCase);

                string newCode = LLM.prompt("Remove unreachable code", sourceCodeToRefactor);
                if (newCode.Contains(" if"))
                {
                    string newCode2 = LLM.prompt("Without introducing new return statements, simplify conditional logic keeping all semantics intact; if not possible, return the code unchanged.", newCode);

                    if (hasSameCountOf("return", newCode, newCode2) &&
                        hasSameCountOf("ttsbegin", newCode, newCode2))
                    {
                        // LLM followed instructions, accept reply
                        newCode = newCode2;
                    }
                }

                newCode = LLM.prompt(@"If any lines contain just a variable declaration then move the type declaration to the first line where the variable is assigned." +
                    "For example: " +
                    "int myInt;" +
                    "myInt = 5;" +
                    "becomes" +
                    "int myInt = 5;" +
                    "Do not change any other variants of variable declarations or usage", newCode);

                newCode = newCode.Replace("\r", String.Empty); //Typically none
                newCode = newCode.Replace("\n", Environment.NewLine);

                if (this.CountLeadingSpaces(newCode) == 0)
                {
                    // Fix indentation
                    int indentation = this.CountLeadingSpaces(sourceCode);

                    newCode = newCode.Replace(Environment.NewLine, Environment.NewLine + new string(' ', indentation));
                    newCode = new string(' ', indentation) + newCode;
                }
                return newCode;
            }

            string updatedAttribs = this.rewriteTestAttributes(sourceCode);
            if (updatedAttribs != null)
            {
                return updatedAttribs;
            }

            return null;
        }

        private string rewriteTestAttributes(string sourceCode)
        {
            XmlMatch m = new XmlMatch();
            m.AddCommaOptional();
            m.AddWhiteSpace();
            m.AddOneOfLiterals("SysTestFeatureDependency", "SysTestCaseFlightDependency");
            m.AddStartParenthesis();
            m.AddLiteral("classStr");
            m.AddStartParenthesis();
            m.AddLiteral(flightToRemove);
            m.AddEndParenthesis();
            m.AddComma();
            m.AddWhiteSpace();
            m.AddCaptureWord();
            m.AddWhiteSpace();
            m.AddEndParenthesis();
            m.AddCommaOptional();
            m.AddWhiteSpace();
            Match match = m.Match(sourceCode);
            if (match.Success)
            {
                string onOff = match.Groups[1].Value;

                switch (onOff.ToLower())
                {
                    case "true":
                        string updatedSource = sourceCode.Remove(match.Index, match.Length);
                        return updatedSource;

                    case "false":
                        return String.Empty; //Delete the method

                    default:
                        throw new Exception($"Unknown test flight dependency: {onOff}");

                }
            }

            return null;
        }

        private string deleteMethod(string methodName, string sourceCode)
        {
            XmlMatch m = new XmlMatch();
            m.AddXMLStart("Method", false);
            m.AddWhiteSpace();
            m.AddXMLStart("Name", false);
            m.AddLiteral(methodName);
            m.AddXMLEnd("Name");
            m.AddWhiteSpace();
            m.AddXMLStart("Source", false);
            m.AddCaptureAnything();
            m.AddXMLEnd("Source");
            m.AddWhiteSpace();
            m.AddXMLEnd("Method");
            m.AddWhiteSpace();

            Match match = m.Match(sourceCode);
            if (match.Success)
            {               
                return sourceCode.Remove(match.Index, match.Length);
            }
            throw new Exception($"Unable to delete method: {methodName} from file {Scanner.FILENAME}");
        }

        public string Run(string _input, int _startAt = 0)
        {  
            if (Scanner.FILENAME.EndsWith(flightToRemove + ".xml", StringComparison.OrdinalIgnoreCase))
            {
                //Skip the flight class itself
                return _input;
            }

            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success)
            {
                string containingXMLElement = MetaData.XMLGetElementNameBeforeIndex(_input, match.Index);
                string updatedInput;

                switch (containingXMLElement)
                {
                    case "Source":
                    case "Declaration":
                        string sourceCode = MetaData.extractPreviousXMLElement(containingXMLElement, match.Index, _input);
                        sourceCode = sourceCode.Replace("<![CDATA[" + Environment.NewLine, "");
                        sourceCode = sourceCode.Replace(Environment.NewLine + Environment.NewLine + "]]>", "");

                        string updatedSource = this.rewriteSource(sourceCode);

                        if (updatedSource != null)
                        {
                            if (updatedSource == String.Empty)
                            {
                                string methodName = MetaData.extractPreviousXMLElement("Name", match.Index, _input);                                
                                updatedInput = this.deleteMethod(methodName, _input);
                            }
                            else
                            {
                                updatedInput = _input.Replace(sourceCode, updatedSource);
                            }

                            Hits++;
                            return this.Run(updatedInput, match.Index + flightToRemove.Length);
                        }

                        if (sourceCode.IndexOf(flightToRemove, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            throw new Exception("Unknown pattern recognized.");
                        }
                        break;

                    case "FeatureClass":
                        int lineStart = _input.LastIndexOf('\n', match.Index);
                        int lineEnd = _input.IndexOf('\n', lineStart + 1);
                        updatedInput = _input.Remove(lineStart, lineEnd-lineStart+1);
                        Hits++;
                        return this.Run(updatedInput, match.Index);

                    default:
                        throw new Exception($"Unsupported xml element: {containingXMLElement}");

                }
                return this.Run(_input, match.Index+match.Length);
            }

            return _input;
        }

        private bool hasSameCountOf(string word, string s1, string s2)
        {
            int count1 = Regex.Matches(s1, word, RegexOptions.IgnoreCase).Count;
            int count2 = Regex.Matches(s2, word, RegexOptions.IgnoreCase).Count;
            return count1 == count2;
        }

        private int CountLeadingSpaces(string s)
        {
            int count = 0;
            foreach (char c in s.Replace(Environment.NewLine, ""))
            {
                if (c == ' ')
                    count++;
                else
                    break;
            }
            return count;
        }
    }

}
