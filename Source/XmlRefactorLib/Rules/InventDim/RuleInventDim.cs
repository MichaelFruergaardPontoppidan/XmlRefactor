using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleInventDim : Rule
    {
        public override string RuleName()
        {
            return "InventDim.doInsert() -> inventDim.insert(true)";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "InventDim";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddLiteral("inventDim");
            xpoMatch.AddDot();
            xpoMatch.AddLiteral("doInsert");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddEndParenthesis();
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
                updatedInput = updatedInput.Insert(match.Index, "inventDim.insert(true)");
                Hits++;
                return this.Run(updatedInput);
            }

            return input;
        }
    }
}
