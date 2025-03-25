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
        private List<RefactorReplaceParameters> referencesToDelete = new List<RefactorReplaceParameters>();

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
            xpoMatch.AddDelimter();
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
            if (this.InputParameter != string.Empty)
                return this.InputParameter;

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
            m.AddCommaOptional()
               .AddWhiteSpace()
               .AddOneOfLiterals("SysTestFeatureDependency", "SysTestCaseFlightDependency")
               .AddStartParenthesis()
               .AddLiteral("classStr")
               .AddStartParenthesis()
               .AddLiteral(flightToRemove)
               .AddEndParenthesis()
               .AddComma()
               .AddWhiteSpace()
               .AddCaptureWord()
               .AddWhiteSpace()
               .AddEndParenthesis()
               .AddCommaOptional()
               .AddWhiteSpace();

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
             m.AddXMLStart("Method", false)
                .AddWhiteSpace()
                .AddXMLStart("Name", false)
                .AddLiteral(methodName)
                .AddXMLEnd("Name")
                .AddWhiteSpace()
                .AddXMLStart("Source", false)
                .AddCaptureAnything()
                .AddXMLEnd("Source")
                .AddWhiteSpace()
                .AddXMLEnd("Method")
                .AddWhiteSpace();

            Match match = m.Match(sourceCode);
            if (match.Success)
            {               
                return sourceCode.Remove(match.Index, match.Length);
            }
            throw new Exception($"Unable to delete method: {methodName} from file {Scanner.FILENAME}");
        }

        private bool canMethodBeDeleted(string source)
        {
            //private boolean validateCleanUpMode(<optional>)
            //{
            //    return true;
            //}

            XmlMatch m = new XmlMatch();
            m.AddLiteral("private")
                .AddWhiteSpace()
                .AddLiteral("boolean")
                .AddWhiteSpace()
                .AddCapture() // Method name
                .AddStartParenthesis()
                .AddCaptureOptional() // Parameters
                .AddEndParenthesis()
                .AddStartCurlyBracket()
                .AddWhiteSpace()
                .AddLiteral("return")
                .AddWhiteSpace()
                .AddCaptureWord() // true / false
                .AddWhiteSpace()
                .AddSemicolon()
                .AddEndCurlyBracket()
            ;
            Match match = m.Match(source);
            if (match.Success)
            {
                string methodName = match.Groups[1].Value;
                string returnValue = match.Groups[3].Value;
                if (returnValue == "true" || returnValue == "false")
                {
                    XmlMatch m2 = new XmlMatch();
                    m2.AddLiteral("this").AddDot().AddLiteral(methodName).AddStartParenthesis().AddCaptureOptional().AddEndParenthesis();

                    referencesToDelete.Add(
                        new RefactorReplaceParameters()
                        {
                            Tag = methodName,
                            Replacement = returnValue,
                            Match = m2
                        });

                    return true;
                }
            }
            return false;
        }
 

        private string deleteReferences(string source, string methodName, string replacement)
        {
            return source;
        }

        private string PostRunForMethod(string sourceCode, RefactorReplaceParameters referenceToDelete)
        {
            string updatedSource = LLMCodeRefactorReplaceText.Replace(sourceCode, referenceToDelete);
            return updatedSource;
        }

        public override string PostRun(string _input)
        {
            string source = _input;
            foreach (var r in referencesToDelete)
            {
                XmlMatch m = new XmlMatch();
                m.AddLiteral(r.Tag);
                int startPos = 0;

                do
                {
                    Match match = m.Match(source, startPos);
                    if (match.Success)
                    {
                        string containingXMLElement = MetaData.XMLGetElementNameBeforeIndex(source, match.Index);
                        switch (containingXMLElement)
                        {
                            case "Source":
                            case "Declaration":

                                string sourceCode = MetaData.extractPreviousXMLElement(containingXMLElement, match.Index, _input);
                                string updatedSourceCode = this.PostRunForMethod(sourceCode, r);

                                if (updatedSourceCode != null)
                                {
                                    if (sourceCode.EndsWith(Environment.NewLine) && !updatedSourceCode.EndsWith(Environment.NewLine))
                                    {
                                        updatedSourceCode += Environment.NewLine;
                                    }
                                    source = source.Replace(sourceCode, updatedSourceCode);
                                    Hits++;
                                }
                                break;
                        }
                        startPos = match.Index + match.Length;
                    }
                    else
                    {
                        break;
                    }
                }
                while (true);
            }

            return source;
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
                        string methodName = MetaData.extractPreviousXMLElement("Name", match.Index, _input);

                        int sourcePos = _input.IndexOf(sourceCode);
                        Boolean scopedToMethod = containingXMLElement == "Source" && methodName.ToLower() != "classdeclaration";
                        XmlMatch m2 = new XmlMatch();

                        m2.AddLiteral(flightToRemove)
                            .AddDoubleColon()
                            .AddLiteral("instance")
                            .AddStartParenthesis()
                            .AddEndParenthesis()
                            .AddDot()
                            .AddLiteral("isEnabled")
                            .AddStartParenthesis()
                            .AddEndParenthesis();

                        var flightRefactorParameters = new RefactorReplaceParameters()
                        {
                            Replacement = "true",
                            KeepBooleanConsts = !scopedToMethod,
                            Match = m2
                        };

                        string updatedSource = LLMCodeRefactorReplaceText.Replace(sourceCode, flightRefactorParameters);

                        bool canBeDeleted = false;
                        string replacement = string.Empty;

                        if (updatedSource != null &&
                            updatedSource != sourceCode)
                        {
                            canBeDeleted = this.canMethodBeDeleted(updatedSource);

                            if (canBeDeleted)
                            {
                                updatedSource = String.Empty;
                            }

                            if (!scopedToMethod)
                            {
                                Match m3 = null;
                                RefactorReplaceParameters parameters3 = null;
                                (m3, parameters3) = LLMCodeRefactorReplaceText.CanRefactorBoolAssignment(updatedSource, _input.Replace(sourceCode, updatedSource), true);
                                if (m3 != null)
                                {
                                    updatedSource = updatedSource.Remove(m3.Index, m3.Length);
                                    parameters3.KeepBooleanConsts = true;                                    
                                    referencesToDelete.Add(parameters3);                                    
                                }
                            }
                        }
                        else
                        {
                            updatedSource = this.rewriteSource(sourceCode);
                        }

                        if (updatedSource != null)
                        {
                           
                            if (updatedSource == String.Empty)
                            {
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
