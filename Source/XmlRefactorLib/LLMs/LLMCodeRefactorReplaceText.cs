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

        private string rewriteSource(string sourceCode)
        {            
            Match match = parameters.Match.Match(sourceCode);

            if (match.Success)
                //    if (sourceCode.IndexOf(parameters.TextToReplace, StringComparison.OrdinalIgnoreCase) > 0)
            {
                //string sourceCodeToRefactor = Regex.Replace(sourceCode, parameters.Replace("(", "\\(").Replace(")", "\\)"), parameters.Replacement, RegexOptions.IgnoreCase);
                string sourceCodeToRefactor = parameters.Match.Regex().Replace(sourceCode, parameters.Replacement);
                
                string newCode = sourceCodeToRefactor;
                newCode = LLM.prompt(@"Rewrite the conditions to make them as simple as possible while preserving the logic, and remove unreachable code. ", sourceCodeToRefactor);

                if (newCode.Contains(" if") &&
                    (newCode.Contains("true") || newCode.Contains("false")))
                {
                    newCode = LLM.prompt(@"Remove unreachable code while preserving the logic.", newCode);
                }
                /*
                if (newCode.Count(c => c == '\n') < 10)
                {
                    newCode = LLM.prompt("Remove variables that are declared and only referenced once while preserving the simplicity of the logic. ", newCode);
                }
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
                return newCode;
            }
            return null;
        }


        private string removeFirstLinesStartingWith(string sourceCode, string potentialStart)
        {
            do
            {
                string firstLine = GetFirstLine(sourceCode);
                if (firstLine.Trim().StartsWith(potentialStart))
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
            sourceCode = sourceCode.Replace(Environment.NewLine + Environment.NewLine + "]]>", "");

            sourceCode = removeFirstLinesStartingWith(sourceCode, "///");
            sourceCode = removeFirstLinesStartingWith(sourceCode, "[");
            sourceCode = removeFirstLinesStartingWith(sourceCode, "///");  // To support attributes before XML doc            

            XmlMatch m = new XmlMatch();

            m.AddWhiteSpaceNoLineBreaksRequired();
            m.AddLiteral("if");
            m.AddStartParenthesis();
            m.AddNotOptional();
            m.addMatch(parameters.Match);

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
    }
}
