using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleEndLengthyOperation : Rule
    {
        public override string RuleName()
        {
            return "endLengthyOperation()";
        }
        public override bool Enabled()
        {
            return false;
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddLiteral("endLengthyOperation");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddEndParenthesis();
            xpoMatch.AddSemicolon();
        }
        override public string Grouping()
        {
            return "Deprecated APIs";
        }
        public override string Run(string input)
        {
            Match match = xpoMatch.Match(input);

            if (match.Success)
            {
                string updatedInput = input.Remove(match.Index, match.Length);
                Hits++;
                return this.Run(updatedInput);
            }

            return input;
        }
    }
}
