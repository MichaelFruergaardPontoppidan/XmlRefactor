using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;

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
            return true;
        }

        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("Source", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Source");        
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
                string sourceCode = match.Groups[1].Value;
                string flightEnabledCall = flightToRemove + "::instance().isenabled()";
                if (sourceCode.IndexOf(flightEnabledCall, StringComparison.OrdinalIgnoreCase)>0)
                {
                    sourceCode = sourceCode.Replace("<![CDATA[" + Environment.NewLine, "");
                    sourceCode = sourceCode.Replace(Environment.NewLine + Environment.NewLine + "]]>", "");
                    string sourceCodeToRefactor = Regex.Replace(sourceCode, flightEnabledCall.Replace("(", "\\(").Replace(")", "\\)"), "true", RegexOptions.IgnoreCase);
    
                    string newCode = LLM.prompt("Remove unreachable code", sourceCodeToRefactor);
                    string newCode2 = LLM.prompt("Without introducing new return statements, simplify conditional logic keeping all semantics intact; if not possible, return the code unchanged.", newCode);

                    if (hasSameCountOf("return", newCode, newCode2) &&
                        hasSameCountOf("ttsbegin", newCode, newCode2)) 
                    {
                        // LLM followed instructions, accept reply
                        newCode = newCode2;
                    }

                    newCode = LLM.prompt(@"If any lines contain just a variable declaration then move the type declaration to the first line where the variable is assigned." +
                        "For example: " +
                        "int myInt;" +
                        "myInt = 5;" +
                        "becomes" +
                        "int myInt = 5;" +
                        "Do not change any other variants of variable declarations or usage", newCode);

                    if (this.CountLeadingSpaces(newCode) == 0)
                    {
                        // Fix indentation
                        int indentation = this.CountLeadingSpaces(sourceCode);

                        newCode = newCode.Replace(@"\r", String.Empty); //Typically none
                        newCode = newCode.Replace("\n", Environment.NewLine);
                        newCode = newCode.Replace(Environment.NewLine, Environment.NewLine + new string(' ', indentation));
                        newCode = new string(' ', indentation) + newCode;
                    }
                    string updatedInput = _input.Replace(sourceCode, newCode);
                    
                    Hits++;
                    return this.Run(updatedInput, match.Index+ newCode.Length);
                } 
                else if (sourceCode.IndexOf(flightToRemove, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    throw new Exception("Unknown pattern recognized.");
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
