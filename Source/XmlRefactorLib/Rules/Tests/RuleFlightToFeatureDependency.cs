using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleFlightToFeatureDependency : Rule
    {
        public RuleFlightToFeatureDependency()
        {
        }

        public override string RuleName()
        {
            return "SysTestCaseFlightDependency -> SysTestFeatureDependency";
        }

        public override bool Enabled()
        {
            return true;
        }
        override public string Grouping()
        {
            return "Tests";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddLiteral("SysTestCaseFlightDependency");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddLiteral("classStr");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddCapture();
            xpoMatch.AddEndParenthesis();
        }

        public override string Run(string _input)
        {
            return this.Run(_input, 0);
        }

        public string Run(string _input, int _startAt = 0)
        {
            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success)
            {
                string className = match.Groups[0].Value;
                if (!className.Contains("Toggle"))
                {
                    string original = match.Value;
                    string newValue = original.Replace("SysTestCaseFlightDependency", "SysTestFeatureDependency");
                    string updatedInput = _input.Replace(original, newValue);
                    Hits++;
                    return this.Run(updatedInput);
                }
            }

            return _input;
        }
    

    }
}
