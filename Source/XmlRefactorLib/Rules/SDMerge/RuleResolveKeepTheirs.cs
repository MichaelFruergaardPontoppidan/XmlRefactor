using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleResolveKeepTheirs : Rule
    {
        public override string RuleName()
        {
            return "Keep theirs";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "SDMerge";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddSymbol(">", 4);
            xpoMatch.AddWhiteSpaceRequired();
            xpoMatch.AddLiteral("ORIGINAL");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddSymbol("<", 4);
            xpoMatch.AddWhiteSpaceRequired();
            xpoMatch.AddLiteral("END");
            xpoMatch.AddWhiteSpaceRequired();
            xpoMatch.AddSymbol("+",48);

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
                string capture = match.Captures[0].ToString();
                string[] sep = new string[3];
                sep[0] = "++++++++++++++++++++++++++++++++++++++++++++++++\r\n";
                sep[1] = "\r\n====";
                sep[2] = "\r\n<<<<";
                string[] split = capture.Split(sep, StringSplitOptions.None);

                string original = split[1];
                string theirs = split[3];
                string yours = split[5];

                
                string output = theirs;

                if (output != "XXX")
	            {
                    string updatedInput = input.Remove(match.Index, match.Length);
                    updatedInput = updatedInput.Insert(match.Index, output);
                    Hits++;
                    return this.Run(updatedInput, match.Index);
		 
	            }
                return this.Run(input, match.Index+4);

            }

            return input;
        }
    }
}
