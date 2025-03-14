using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleResolveWhiteSpaceBaseLess : Rule
    {
        public override string RuleName()
        {
            return "Resolve WhiteSpace baseless";
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
            xpoMatch.AddLiteral("THEIRS");
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

        private string trim(string input)
        {
            string[] sep = new string[1];
            sep[0] = "\r\n";
            string[] split = input.Split(sep, StringSplitOptions.None);
            string trimmed = string.Empty;

            foreach (var s in split)
            {
//                if (s.Contains("<ButtonDisplay"))
//                    continue;

                trimmed += s+ "\r\n";
            }


            return trimmed.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").ToLowerInvariant();

        }
        public string Run(string input, int startAt = 0)
        {
            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                string capture = match.Captures[0].ToString();
                string[] sep = new string[3];
                sep[0] = "++++++++++++++++++++++++++++++++++++++++++++++++\r\n";
                sep[1] = "====";
                sep[2] = "<<<<";
                string[] split = capture.Split(sep, StringSplitOptions.None);

                string theirs = split[1];
                string yours = split[3];


                string theirsTrim = trim(theirs);
                string yoursTrim = trim(yours);

                string output = "XXX";

                if (theirsTrim == yoursTrim)
                    output = theirs;

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
