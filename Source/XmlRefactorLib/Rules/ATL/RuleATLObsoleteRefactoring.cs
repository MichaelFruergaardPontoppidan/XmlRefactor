using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleATLObsoleteRefactoring : Rule
    {
        public RuleATLObsoleteRefactoring()
        {
        }

        public override string RuleName()
        {
            return "Refactor calls to obsolete APIs";
        }

        public override bool Enabled()
        {
            return false;
        }
        override public string Grouping()
        {
            return "ATL";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("AxClass");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("AxClass");
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

                string source = match.Groups[2].Value;
                            string newSource = Regex.Replace(source, @"\.setWHSUOMSeqGroup\(", @".setUnitSequenceGroup(", RegexOptions.IgnoreCase);
                            newSource = Regex.Replace(newSource, @"\.setUnitOfMeasure\(", @".setUnit(", RegexOptions.IgnoreCase);
                            newSource = Regex.Replace(newSource, @"\.setSalesUnitOfMeasure\(", @".setSalesUnit(", RegexOptions.IgnoreCase);
                            newSource = Regex.Replace(newSource, @"\.setPurchaseUnitOfMeasure\(", @".setPurchaseUnit(", RegexOptions.IgnoreCase);
                            //newSource = Regex.Replace(newSource, @"\.setPmfProductType\(", @".setProductionType(", RegexOptions.IgnoreCase);
                            newSource = Regex.Replace(newSource, @"\.setWHSReservationHierarchy\(", @".setReservationHierarchy(", RegexOptions.IgnoreCase);
                            //newSource = Regex.Replace(newSource, @"\.setPlannedOrderType\(", @".setDefaultOrderType(", RegexOptions.IgnoreCase);
                            newSource = Regex.Replace(newSource, @"\.setPmfPlanningItemId\(", @".setPlanningFormulaItemId(", RegexOptions.IgnoreCase);
                            //newSource = Regex.Replace(newSource, @"\.setReqGroup\(", @".setCoverageGroup(", RegexOptions.IgnoreCase);
                            newSource = Regex.Replace(newSource, @"SysTestCaseDatasetDependency\(\'DemoData\'\, ", @"SysTestCaseDataDependency(", RegexOptions.IgnoreCase);
                            newSource = Regex.Replace(newSource, @"SysTestCaseDatasetDependencyAttribute\(\'DemoData\'\, ", @"SysTestCaseDataDependency(", RegexOptions.IgnoreCase);

                _input = _input.Replace(source, newSource);
                Hits++;

            }

            return _input;
        }
            
        private string getLineAtPos(string source, int pos)
        {
            int pos2 = source.LastIndexOf(Environment.NewLine, pos) + 1;
            return source.Substring(pos2, pos - pos2);
        }

    }
}
