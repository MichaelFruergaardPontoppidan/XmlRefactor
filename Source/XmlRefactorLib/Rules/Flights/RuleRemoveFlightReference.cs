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
                        
                        string flightEnabledCall = flightToRemove + "::instance().isenabled()";
                        string updatedSource = LLMCodeRefactorReplaceText.ReplaceWithTrue(sourceCode, flightEnabledCall);
                        if (updatedSource == null)
                        {
                            updatedSource = this.rewriteSource(sourceCode);
                        }

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
    }

}
