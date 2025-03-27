using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    public class LLMCodeRefactorReplaceText
    {
        private RefactorReplaceParameters parameters = null;

        static public string Replace(string sourceCode, RefactorReplaceParameters _parameters)
        {
            var refactor = new LLMCodeRefactorReplaceText();
            refactor.parameters = _parameters;

            string sourceToRefactor = refactor.pruneSourceForLLM(sourceCode);
            string updatedSource = refactor.rewriteSource(sourceToRefactor);

            if (updatedSource != null)
            {
                return sourceCode.Replace(sourceToRefactor, updatedSource);
            }
            return null;
        }

        private static bool isVariableAssignedTo(string sourceCode, string variableName)
        {
            // b = 

            XmlMatch m = new XmlMatch();
            m.AddCommaOptional()
               .AddWhiteSpace()
               .AddLiteral(variableName)
               .AddEqual();

            Match match = m.Match(sourceCode);
            return match.Success;
        }

        public static (Match, RefactorReplaceParameters) CanRefactorBoolAssignment(string sourceCode, string fullSource, Boolean mustBePrivate = false)
        {
            // boolean b = true;

            XmlMatch m = new XmlMatch();
            if (mustBePrivate)
            {                
                m.AddWhiteSpace().AddLiteral("private");
                m.AddWhiteSpace().AddLiteralOptional("static");
                m.AddWhiteSpace().AddLiteralOptional("readonly");
            }

            m.AddWhiteSpace()
               .AddLiteral("boolean")
               .AddCapture()  // Variable name
               .AddEqual()
               .AddWhiteSpace()
               .AddCaptureWord() //("true", "false")
               .AddWhiteSpace()
               .AddSemicolon();
            
            Match match = m.Match(sourceCode);
            while (match.Success)
            {
                string variableName = match.Groups[1 + (mustBePrivate? 2 : 0)].Value;
                string assignment = match.Groups[2 + (mustBePrivate ? 2 : 0)].Value.ToLower();

                switch (assignment)
                {
                    case "true":
                    case "false":
                        if (!isVariableAssignedTo(sourceCode.Remove(match.Index, match.Length), variableName) &&
                            !isVariableAssignedTo(fullSource.Replace(sourceCode, ""), variableName))
                        {
                            XmlMatch m2 = new XmlMatch();
                            m2.AddLiteral(variableName);

                            return (match,
                                new RefactorReplaceParameters()
                                {
                                    Replacement = assignment,
                                    Match = m2,
                                    Tag = variableName,
                                    FullMethodOnly = true
                                });
                        }
                        break;
                }
                match = m.Match(sourceCode, match.Index + match.Length);
            }
            return (null, null);
        }

        private bool hasUnconditionalIfStatement(string sourceCode)
        {
            // if (true)
            // if (false)

            XmlMatch m = new XmlMatch();
            m.AddCommaOptional()
               .AddWhiteSpace()
               .AddLiteral("if")
               .AddStartParenthesis()
               .AddOneOfLiterals("true", "false")
               .AddEndParenthesis();

            Match match = m.Match(sourceCode);
            return match.Success;
        }

        private string rewriteSource(string sourceCode)
        {            
            Match match = parameters.Match.Match(sourceCode);

            if (match.Success)
            {
                string sourceCodeToRefactor = parameters.Match.Regex().Replace(sourceCode, parameters.Replacement);
                
                string newCode = sourceCodeToRefactor;
                newCode = LLM.prompt(@"Rewrite the conditions to make them as simple as possible while preserving the logic, and remove unreachable code. ", sourceCodeToRefactor);

                if (hasUnconditionalIfStatement(newCode))
                {
                    newCode = LLM.prompt(@"Remove unreachable code while preserving all logic.", newCode);
                }

                if (!parameters.KeepBooleanConsts)
                {
                    Match m2 = null;
                    RefactorReplaceParameters parameters2 = null;
                    (m2, parameters2) = CanRefactorBoolAssignment(newCode, newCode);
                    if (m2 != null)
                    {
                        newCode = newCode.Remove(m2.Index, m2.Length);
                        newCode = LLMCodeRefactorReplaceText.Replace(newCode, parameters2);
                    }
                }

                if (newCode != null)
                {
                    if (newCode != String.Empty)
                    {
                        newCode = newCode.Replace("\r", String.Empty); //Typically none
                        newCode = newCode.Replace("\n", Environment.NewLine);

                        if (this.CountLeadingSpaces(newCode) == 0)
                        {
                            // Fix indentation
                            int indentation = this.CountLeadingSpaces(sourceCode);

                            newCode = newCode.Replace(Environment.NewLine, Environment.NewLine + new string(' ', indentation));
                            newCode = new string(' ', indentation) + newCode;
                        }
                        if (sourceCode.EndsWith(Environment.NewLine) && !newCode.EndsWith(Environment.NewLine))
                        {
                            newCode += Environment.NewLine;
                        }
                    }
                }
                return newCode;
            }
            return sourceCode;
        }


        private string removeFirstLinesStartingWith(string sourceCode, string potentialStart)
        {
            do
            {
                string firstLine = GetFirstLine(sourceCode);
                if (firstLine.Trim().StartsWith(potentialStart) ||
                    firstLine.Trim().Length == 0)
                {
                    sourceCode = sourceCode.Remove(0, firstLine.Length + 1);
                }
                else
                {
                    break;
                }
            } while (true);

            return sourceCode;
        }

        private string pruneSourceForLLM(string originalSource)
        {
            string sourceCode = originalSource;
            sourceCode = sourceCode.Replace("<![CDATA[" + Environment.NewLine, "");
            sourceCode = sourceCode.Replace(Environment.NewLine + "]]>", "");

            sourceCode = removeFirstLinesStartingWith(sourceCode, "using");
            sourceCode = removeFirstLinesStartingWith(sourceCode, "///");
            sourceCode = removeFirstLinesStartingWith(sourceCode, "[");
            sourceCode = removeFirstLinesStartingWith(sourceCode, "///");  // To support attributes before XML doc            

            if (!parameters.FullMethodOnly)
            {
                XmlMatch m = new XmlMatch();
                /*
                m.AddNewLine();
                m.AddWhiteSpaceNoLineBreaksRequired();
                m.AddLiteral("if");
                m.AddStartParenthesis();
                m.AddNotOptional();
                m.addMatch(parameters.Match);
                */
                m.addMatch(parameters.Match);

                Match match = m.Match(sourceCode);
                if (match.Success)
                {
                    int ifPos = sourceCode.LastIndexOf(" if", match.Index);
                    int semiColonPos = sourceCode.LastIndexOf(";", match.Index);
                    int bracketEndPos = sourceCode.LastIndexOf("}", match.Index);
                    if (ifPos > 0 && 
                        ifPos > semiColonPos &&    // No ; in the middle
                        ifPos > bracketEndPos &&   // No } in the middle
                        match.Index - ifPos < 500) // Not too far apart
                    {
                        int newLinePos = sourceCode.LastIndexOf(Environment.NewLine, ifPos);
                        if (newLinePos > 0)
                        {
                            sourceCode = sourceCode.Remove(0, newLinePos+Environment.NewLine.Length);
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
                                        indentation <= startIndentation &&
                                        !line.Contains("else"))
                                    {
                                        // After the end bracket, break, unless an else block is coming.
                                        break;
                                    }
                                    newSource += line + '\n';
                                    endBracket = ((line.Trim() == "}" && indentation == startIndentation));
                                }
                                else
                                {
                                    break;
                                }
                            }
                            sourceCode = newSource.Replace("\n", "\r\n");
                        }
                    }
                }
            }
            if (!originalSource.Contains(sourceCode))
            {
                throw new Exception("Unable to prune");
            }

            return sourceCode;
        }
        private string GetFirstLine(string multilineString)
        {
            int newlineIndex = multilineString.IndexOf('\n');
            if (newlineIndex == -1)
            {
                return multilineString; // The string is a single line
            }
            return multilineString.Substring(0, newlineIndex);
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

    public class RefactorReplaceParameters
    {
        public string Tag; 
        public string Replacement;
        public XmlMatch Match;
        public Boolean FullMethodOnly = false;
        public Boolean KeepBooleanConsts = false;
    }
}
