using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleWHSCatchWeightFeatureFlightingDetour : Rule
    {
        public override string RuleName()
        {
            return "Remove WHSCatchWeightFeatureFlightingDetour";
        }
        public override bool Enabled()
        {
            return true;
        }
        protected override void buildXpoMatch()
        {
            //using (var detour = new WHSCatchWeightFeatureFlightingDetour(true))

            xpoMatch.AddLiteral("using");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddCapture();
            xpoMatch.AddWhiteSpaceRequired();
            xpoMatch.AddCapture();
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddEqual();
            xpoMatch.AddWhiteSpaceRequired();
            xpoMatch.AddLiteral("new");
            xpoMatch.AddWhiteSpaceRequired();
            xpoMatch.AddLiteral("WHSCatchWeightFeatureFlightingDetour");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddLiteral("true");
            xpoMatch.AddEndParenthesis();
            xpoMatch.AddEndParenthesis();
        }
        override public string Grouping()
        {
            return "Obsolete";
        }

        private string indentLeft(string input, int pos, string line)
        {
            if (input.Substring(pos, 4) == "    ")
                return input.Remove(pos, 4);

            if (input.Substring(pos,1) == "\t")
                return input.Remove(pos, 1);

            return input;
        }

        public override string Run(string input)
        {
            Match match = xpoMatch.Match(input);

            if (match.Success)
            {
                int preceedingCR = input.LastIndexOf(Environment.NewLine, match.Index);
                int trailingCR = input.IndexOf(Environment.NewLine, match.Index);
                int nlLenght = Environment.NewLine.Length;
                string updatedInput = input.Remove(preceedingCR + nlLenght, trailingCR- preceedingCR);
                string file = Scanner.FILENAME;
                int startPos = preceedingCR+ nlLenght;
                int indentation = 0;
                while (true)
                {
                    int nextCR = updatedInput.IndexOf(Environment.NewLine, startPos);
                    string line = updatedInput.Substring(startPos, nextCR - startPos+ nlLenght);

                    if (line.Replace(Environment.NewLine, "").Trim() == "{")
                    {
                        indentation++;
                        if (indentation == 1)
                        {
                            updatedInput = updatedInput.Remove(startPos, line.Length);
                            continue;
                        }
                        else
                        {
                            updatedInput = this.indentLeft(updatedInput, startPos, line);
                        }
                    }
                    else if (line.Replace(Environment.NewLine, "").Trim() == "}")
                    {
                        indentation--;
                        if (indentation <= 0)
                        {
                            if (indentation == 0)
                            {
                                updatedInput = updatedInput.Remove(startPos, line.Length);
                            }
                            break;
                        }
                        else
                        {
                            updatedInput = this.indentLeft(updatedInput, startPos, line);
                        }
                    }
                    else
                    {
                        updatedInput = this.indentLeft(updatedInput, startPos, line);
                    }
                    startPos = updatedInput.IndexOf(Environment.NewLine, startPos)+nlLenght; 
                }
                
                Hits++;
                return this.Run(updatedInput);
            }

            return input;
        }
    }
}
