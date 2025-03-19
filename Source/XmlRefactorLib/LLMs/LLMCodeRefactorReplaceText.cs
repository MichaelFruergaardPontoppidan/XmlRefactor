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
        private string textToReplace;
        private string originalSourceCode;
        private string pattern = "flight";

        static private string Replace(string sourceCode, string textToReplace, string replacement)
        {
            var refactor = new LLMCodeRefactorReplaceText();
            refactor.originalSourceCode = sourceCode;
            refactor.textToReplace = textToReplace;

            string sourceToRefactor = refactor.pruneSourceForLLM(sourceCode);
            string updatedSource = refactor.rewriteSource(sourceToRefactor);

            if (updatedSource != null)
            {
                return sourceCode.Replace(sourceToRefactor, updatedSource);
            }
            return null;
        }


        static public string ReplaceWithTrue(string sourceCode, string textToReplace)
        {
            return Replace(sourceCode, textToReplace, "true");
        }

        private string rewriteSource(string sourceCode)
        {            
            if (sourceCode.IndexOf(textToReplace, StringComparison.OrdinalIgnoreCase) > 0)
            {
                string sourceCodeToRefactor = Regex.Replace(sourceCode, textToReplace.Replace("(", "\\(").Replace(")", "\\)"), "true", RegexOptions.IgnoreCase);

                string newCode = sourceCodeToRefactor;
               // if (newCode.Contains(" if"))
                {
                    newCode = LLM.prompt(@"
                        If possible, simplify boolean logic in IF, WHERE and WHILE conditions. You can only change the condition block in statements, and remove unreachable code. 
                        Examples:
                        if (a && true) becomes if (a)
                        if (a && false) becomes if (false)
                        if (a || true) becomes if (true)
                        if (a && !true) becomes if (false)
                        while (a && !true) becomes while (false)
                        if (a || b || true)) becomes if (a || b)
                        ", sourceCodeToRefactor);
                }
                //newCode = LLM.prompt("Remove unreachable code", newCode);
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
                if (sourceCode.EndsWith(Environment.NewLine) && !newCode.EndsWith(Environment.NewLine))
                {
                    newCode += Environment.NewLine;
                }

                return newCode;
            }
            return null;
        }

        private string pruneSourceForLLM(string originalSource)
        {
            string sourceCode = originalSource;
            sourceCode = sourceCode.Replace("<![CDATA[" + Environment.NewLine, "");
            sourceCode = sourceCode.Replace(Environment.NewLine + Environment.NewLine + "]]>", "");

            do
            {
                string firstLine = GetFirstLine(sourceCode);
                if (firstLine.Trim().StartsWith("///"))
                {
                    sourceCode = sourceCode.Remove(0, firstLine.Length + 1);
                }
                else
                {
                    break;
                }
            } while (true);

            XmlMatch m = new XmlMatch();

            m.AddWhiteSpaceNoLineBreaksRequired();
            m.AddLiteral("if");
            m.AddStartParenthesis();

            switch (pattern)
            {
                case "flight":
                    string flightToRemove = textToReplace.Substring(0, textToReplace.IndexOf(":"));

                    m.AddLiteral(flightToRemove);
                    m.AddDoubleColon();
                    m.AddLiteral("instance");
                    m.AddStartParenthesis();
                    m.AddEndParenthesis();
                    m.AddDot();
                    m.AddLiteral("isEnabled");
                    m.AddStartParenthesis();
                    m.AddEndParenthesis();
                    break;
            }

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
}
