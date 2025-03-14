using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleNormalImageMapAddress : Rule
    {
        public override string RuleName()
        {
            return "<NormalImage>MapAddress</NormalImage> -> <NormalImage>Map</NormalImage>";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "BP Fixers";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddSymbol("<");
            xpoMatch.AddLiteral("NormalImage");
            xpoMatch.AddSymbol(">");
            xpoMatch.AddLiteral("MapAddress");
            xpoMatch.AddSymbol("<");
            xpoMatch.AddSymbol("/");
            xpoMatch.AddLiteral("NormalImage");
            xpoMatch.AddSymbol(">");
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
                string updatedInput = input.Remove(match.Index, match.Length);
                updatedInput = updatedInput.Insert(match.Index, "<NormalImage>Map<NormalImage>");
                Hits++;
                return this.Run(updatedInput);
            }

            return input;
        }
    }
}
