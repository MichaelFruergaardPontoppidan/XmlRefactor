using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace XmlRefactor
{
    class RuleReplaceableSwitch : Rule
    {
        public override string RuleName()
        {
            return "Switch -> Replaceable";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "Extensibility";
        }
        protected override void buildXpoMatch()
        {            
            xpoMatch.AddXMLStart("Source", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Source");
        }
        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        public string Run(string input, int startAt = 0)
        {
            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                string xml = input;

                string source = match.Value.ToLowerInvariant();

                if (source.Contains("replaceable"))
                    return input;

                if (source.Contains("private "))
                    return input;

                int posSwitch = source.IndexOf("switch");

                if (posSwitch == -1)
                    return input;

                int posThrow = source.IndexOf("throw ");

                if (posThrow == -1)
                    return input;

                if (posSwitch < posThrow)                    
                {
                    int posLineBreak = source.IndexOf('\n', posThrow)+1;
                    string afterThrow = source.Substring(posLineBreak);
                    int posLastBracket = afterThrow.LastIndexOf('}');

                    afterThrow = afterThrow.Substring(1, posLastBracket);

                    int endSemiColons = afterThrow.Count(c => c == ';');
                    if (endSemiColons == 0 ||
                        endSemiColons == 1 && afterThrow.Contains("return "))
                    {
                        int posFirstBracket = source.IndexOf('{');

                        string beforeSwitch = source.Substring(posFirstBracket, posSwitch - posFirstBracket);
                        
                        if (!beforeSwitch.Contains("::") &&
                            !beforeSwitch.Contains("(") &&
                            !beforeSwitch.Contains("."))
                        {

                            int posParametersStart = source.Substring(1, posFirstBracket).LastIndexOf('(');
                            int posSignatureLineStart = source.Substring(1, posParametersStart).LastIndexOf('\n');

                            string updatedInput = input.Insert(match.Index + posSignatureLineStart + 2, "    [Replaceable]\r\n");
                            Hits++;
                            return this.Run(updatedInput);
                        }
                    }

                }

            }

            return input;
        }
    }
}
