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

//TODO: If removal of test method removes the last method on the test class, then delete the entire class. Example: WHSWareReleaseUseCrossDockForShipConsPoliciesFlightV3Test
//TODO: When a class is internal, then a boolean flag that is non-private can be removed.
namespace XmlRefactor
{

    class RuleRemoveFlightReference : Rule
    {
        private string flightToRemove = String.Empty;
        private List<RefactorReplaceParameters> referencesToDelete = new List<RefactorReplaceParameters>();

        public RuleRemoveFlightReference()
        {
        }
        public override void Init(string parameter)
        {
            base.Init(parameter);
            flightToRemove = this.getFlightToRemove();
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
            string updatedSource = this.rewriteAttributes(sourceCode);
            
            if (updatedSource == null)
                return null;

            updatedSource = this.rewriteCommentLineWithReference(updatedSource);

            return updatedSource;
        }

        private string rewriteCommentLineWithReference(string sourceCode)
        {
            XmlMatch m = new XmlMatch();
                m.AddLiteral(flightToRemove);

            Match match = m.Match(sourceCode);
            if (match.Success)
            {
                int startOfLine = sourceCode.LastIndexOf(Environment.NewLine, match.Index);
                int endOfLine = sourceCode.IndexOf(Environment.NewLine, match.Index);
                string line = sourceCode.Substring(startOfLine, endOfLine - startOfLine);

                if (line.Trim().StartsWith("//"))
                {
                    string newLine = LLM.prompt($"Rewrite this comment by removing references to flight(s), {flightToRemove} and enablement. That is now redundant as the flights are always enabled.", line);
                    if (newLine.Trim().StartsWith("//") &&
                        newLine.Trim().Length > 2)
                    { 
                        sourceCode = sourceCode.Replace(line.Trim(), newLine.Trim());
                    }
                    else
                    {
                        // Remove the line
                        sourceCode = sourceCode.Remove(startOfLine, endOfLine - startOfLine);
                    }
                }
            }

            return sourceCode;
        }
              
        private string rewriteAttributes(string sourceCode)
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

            m = new XmlMatch();
            m.AddCommaOptional()
               .AddWhiteSpace()
               .AddOneOfLiterals("SysTestFeatureDependency", "SysTestCaseFlightDependency")
               .AddStartParenthesis()
               .AddLiteral("classStr")
               .AddStartParenthesis()
               .AddLiteral(flightToRemove)
               .AddEndParenthesis()
               .AddEndParenthesis()
               .AddCommaOptional()
               .AddWhiteSpace();

            match = m.Match(sourceCode);
            if (match.Success)
            {
                int length = match.Length;
                string trimmedValue = match.Value.Trim();
                if (trimmedValue.StartsWith(",") && trimmedValue.EndsWith(","))
                {
                    length = match.Value.LastIndexOf(",");
                }
                string updatedSource = sourceCode.Remove(match.Index, length);
                return updatedSource;
            }

            m = new XmlMatch();
            m.AddCommaOptional()
               .AddWhiteSpace()
               .AddLiteral("DataMaintenanceFeatureClass")
               .AddStartParenthesis()
               .AddLiteral("classStr")
               .AddStartParenthesis()
               .AddLiteral(flightToRemove)
               .AddEndParenthesis();

            match = m.Match(sourceCode);
            if (match.Success)
            {
                int startOfLine = sourceCode.LastIndexOf(Environment.NewLine, match.Index);
                int endOfLine = sourceCode.IndexOf(Environment.NewLine, match.Index);
                string line = sourceCode.Substring(startOfLine, endOfLine - startOfLine).Trim();

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    sourceCode = sourceCode.Remove(startOfLine, endOfLine - startOfLine);
                }
            }

            return sourceCode;
        }

        private (string, int) deleteMethod(string methodName, string sourceCode)
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
                return (sourceCode.Remove(match.Index, match.Length), match.Index);
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
                m.AddDelimter();
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

                                string sourceCode = MetaData.extractPreviousXMLElement(containingXMLElement, match.Index, source);
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

            if (source.IndexOf(flightToRemove, StringComparison.OrdinalIgnoreCase) > 0)
            {
                if (source.IndexOf("FeatureStateProvider", StringComparison.OrdinalIgnoreCase) > 0)
                    throw new NotSupportedException($"Unsupported pattern: FeatureStateProvider, for flight: {flightToRemove}");

                if (source.IndexOf("SysDetour", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    Console.WriteLine($"Unsupported pattern: SysDetour, for flight: {flightToRemove} in {Scanner.FILENAME}");
                }
                else
                {
                    Console.WriteLine($"Unknown pattern recognized in {Scanner.FILENAME}");
                }
                //      throw new Exception("Unknown pattern recognized.");
            }

            return source;
        }



        private bool isFlightClass()
        {
            return (Scanner.FILENAME.EndsWith(flightToRemove + ".xml", StringComparison.OrdinalIgnoreCase));
        }

        public override bool skip(string input)
        {
            if (isFlightClass())
            {
                //Skip the flight class itself
                return true;
            }
            return base.skip(input);
        }

        public string Run(string _input, int _startAt = 0)
        {  
            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success)
            {
                string containingXMLElement = MetaData.XMLGetElementNameBeforeIndex(_input, match.Index);
                string updatedInput;

                switch (containingXMLElement)
                {
                    case "Message":
                    case "Path":
                        string bpSuppression = MetaData.extractPreviousXMLElementInclusive("Diagnostic", match.Index, _input);
                        int pos = _input.IndexOf(bpSuppression);
                        if (pos > 0)
                        {
                            int diagLineStart = _input.LastIndexOf(Environment.NewLine, pos);
                            int diagLineEnd = _input.IndexOf(Environment.NewLine, pos+bpSuppression.Length);
                            updatedInput = _input.Remove(diagLineStart, diagLineEnd-diagLineStart);
                            Hits++;
                            return this.Run(updatedInput, match.Index);
                        }
                        break;
            
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

                        if (updatedSource != null &&
                            updatedSource != sourceCode)
                        {                           
                            if (updatedSource == String.Empty)
                            {
                                if (scopedToMethod)
                                {
                                    (updatedInput, sourcePos) = this.deleteMethod(methodName, _input);
                                }
                                else
                                {
                                    //Delete file
                                    return string.Empty;
                                }
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
                        break;

                    case "FeatureClass":
                        int lineStart = _input.LastIndexOf('\n', match.Index);
                        int lineEnd = _input.IndexOf('\n', lineStart + 1);
                        updatedInput = _input.Remove(lineStart, lineEnd-lineStart+1);
                        Hits++;
                        return this.Run(updatedInput, match.Index);

                    case "Name":
                        // Do not change method names
                        break;

                    default:
                        throw new Exception($"Unsupported xml element: {containingXMLElement}");

                }
                return this.Run(_input, match.Index+match.Length);
            }

            return _input;
        }
    }

}
