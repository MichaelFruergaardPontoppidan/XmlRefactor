using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleDimension : Rule
    {
        public override string RuleName()
        {
            return "DimensionDefaultingEngine";
         //   DimensionDefaultingEngine::getDefaultDimensionSpecifiers
        }
        public override bool Enabled()
        {
            return false;
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddDelimter();
            xpoMatch.AddLiteral("DimensionDefaultingEngine");
            xpoMatch.AddDoubleColon();
            xpoMatch.AddCapture();
            xpoMatch.AddStartParenthesis();
        }
        override public string Grouping()
        {
            return "DimensionDefaultingEngine";
        }
        public override string Run(string input)
        {
            Match match = xpoMatch.Match(input);

            if (match.Success)
            {
                string updatedInput = input.Remove(match.Index+1, match.Length-1);
                string capture = match.Groups[1].Value.Trim();
                string replacement = "";
                switch (capture)
                {
                    case "getLedgerDimensionSpecifiers":
                        replacement = "LedgerDimensionDefaultingEngine::getLedgerDimensionSpecifiers(";
                        break;
                    case "getDefaultDimensionSpecifiers":
                        replacement = "LedgerDimensionDefaultingEngine::getDefaultDimensionSpecifiers(";
                        break;
                    case "createLedgerDimension":
                        replacement = "LedgerDimensionFacade::serviceCreateLedgerDimension(";
                        break;
                
                }
                if (replacement != "")
                {
                    updatedInput = updatedInput.Insert(match.Index + 1, replacement);
                    Hits++;
                    return this.Run(updatedInput);
                }
            }

            return input;
        }
    }
}
