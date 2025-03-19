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
                
                string newCode = sourceCodeToRefactor;
                if (newCode.Contains(" if"))
                {
                    newCode = LLM.prompt(@"
                        If possible, simplify boolean logic in IF, WHERE and WHILE conditions. You can only change the condition block in statements. 
                        Examples:
                        if (a && true) becomes if (a)
                        if (a && false) becomes if (false)
                        if (a || true) becomes if (true)
                        if (a && !true) becomes if (false)
                        while (a && !true) becomes while (false)
                        if (a || b || true)) becomes if (a || b)
                        ", sourceCodeToRefactor);
                }
                newCode = LLM.prompt("Remove unreachable code", newCode);
                if (newCode.Count(c => c == '\n') < 10)
                {
                    newCode = LLM.prompt("Remove unnessesary variables without reducing code readability. Follow clean code principles.", newCode);
                }
                /*
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
                */

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
            //m.AddLiteral("SysTestFeatureDependency");
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
                string onOff = match.Groups[2].Value;

                switch (onOff.ToLower())
                {
                    case "true":
                        int length = match.Length;
                        string trimmedValue = match.Value.Trim();
                        if (trimmedValue.StartsWith(",") && trimmedValue.EndsWith(","))
                        {
                            length = match.Value.LastIndexOf(",");
                        }
                        string updatedSource = sourceCode.Remove(match.Index, length);
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


        private string pruneSourceForLLM(string originalSource)
        {
            string sourceCode = originalSource.Replace("<![CDATA[" + Environment.NewLine, "");
            sourceCode = sourceCode.Replace(Environment.NewLine + Environment.NewLine + "]]>", "");

            XmlMatch m = new XmlMatch();

            m.AddWhiteSpaceNoLineBreaksRequired();
            m.AddLiteral("if");            
            m.AddStartParenthesis();
            m.AddLiteral(flightToRemove);
            m.AddDoubleColon();
            m.AddLiteral("instance");
            m.AddStartParenthesis();
            m.AddEndParenthesis();
            m.AddDot();
            m.AddLiteral("isEnabled");
            m.AddStartParenthesis();
            m.AddEndParenthesis();
            Match match = m.Match(sourceCode);
            if (match.Success)
            {
                sourceCode = sourceCode.Remove(0, match.Index);
                int startIndentation = this.CountLeadingSpaces(sourceCode);
                var lines = sourceCode.Replace("\r", "").Split('\n');
                string newSource = string.Empty;
                bool endBracket = false;
                foreach (string line in lines)
                {
                    int indentation = this.CountLeadingSpaces(line);
                    if (indentation >= startIndentation || line.Trim().Length == 0)
                    {
                        if (endBracket &&
                            indentation == startIndentation &&
                            !line.Contains("else"))
                        {
                            // After the end bracket, break, unless an else block is coming.
                            break;
                        }
                        newSource += line+'\n';
                        endBracket = ((line.Trim() == "}" && indentation == startIndentation));
                    }
                    else
                    {
                        break;
                    }
                }
                sourceCode = newSource.Replace("\n", "\r\n");
            }
            if (!originalSource.Contains(sourceCode))
            {
                throw new Exception("Unable to prune");
            }

            return sourceCode;
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
                        int sourcePos = _input.IndexOf(sourceCode);
                        sourceCode = this.pruneSourceForLLM(sourceCode);

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
                                if (sourceCode.EndsWith(Environment.NewLine) && !updatedSource.EndsWith(Environment.NewLine))
                                {
                                    updatedSource += Environment.NewLine;
                                }
                                updatedInput = _input.Replace(sourceCode, updatedSource);
                            }

                            Hits++;
                            return this.Run(updatedInput, sourcePos);
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
