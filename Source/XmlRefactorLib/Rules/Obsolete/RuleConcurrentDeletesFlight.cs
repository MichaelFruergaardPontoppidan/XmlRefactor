using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleConcurrentDeletesFlight : Rule
    {
        public override string RuleName()
        {
            return "RuleConcurrentDeletesFlight";
        }
        public override bool Enabled()
        {
            return false;
        }
        protected override void buildXpoMatch()
        {
            // super() || WhsEnforceConcurrentDeletesToggle::instance().isEnabled()

            xpoMatch.AddLiteral("super");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddEndParenthesis();
            xpoMatch.AddORSymbol();
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddLiteral("WhsEnforceConcurrentDeletesToggle");
            xpoMatch.AddDoubleColon();
            xpoMatch.AddLiteral("instance");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddEndParenthesis();
            xpoMatch.AddDot();
            xpoMatch.AddLiteral("isEnabled");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddEndParenthesis();
        }
        override public string Grouping()
        {
            return "Obsolete";
        }
        public override string Run(string input)
        {
            Match match = xpoMatch.Match(input);

            if (match.Success)
            {
                string updatedInput = input.Remove(match.Index, match.Length);
                updatedInput = updatedInput.Insert(match.Index, "true");
                Hits++;
                return this.Run(updatedInput);
            }

            return input;
        }
    }
}
