using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleConvertToCheckInTestCases : Rule
    {
        public RuleConvertToCheckInTestCases()
        {
        }

        public override string RuleName()
        {
            return "Convert normal tests to CITs";
        }

        public override bool Enabled()
        {
            return false;
        }
        override public string Grouping()
        {
            return "Tests";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("Source", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Source");
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
                var sourceFile = Path.GetFileNameWithoutExtension(Scanner.FILENAME).ToLowerInvariant();

                if (!sourceFile.StartsWith("whs"))
                {
                    // Skip for now
                    return _input;
                }

                if (sourceFile.Contains("entity") ||
                    sourceFile.Contains("consistency"))
                {
                    // Skip for now
                    return _input;
                }

                string xml = _input;
                var stringToUpdate = match.Value;
                _input = this.processSource(_input, stringToUpdate);
                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }
        
        private string processSource(string _input, string _methodContents)
        {
            string lowerCaseCode = _methodContents.ToLowerInvariant();

            if (lowerCaseCode.Contains("systestcasedependsonreport"))
            {
                // Skip tests with report dependencies
                return _input;
            }

            if (lowerCaseCode.Contains("systestcheckintest"))
            {
                // Already a CIT
                return _input;
            }

            if (!lowerCaseCode.Contains("systestmethod"))
            {
                // Not a test
                return _input;
            }

            if (lowerCaseCode.Contains("formadaptor"))
            {
                // Uses form adaptors - too slow, so keep as normal test
                return _input;
            }

            int posOfClassDecl = _input.IndexOf("{");
            int posOfCIT = _input.ToLowerInvariant().IndexOf("systestcheckintest", 0, posOfClassDecl);

            if (posOfCIT > 0)
            {
                // CIT by class level attribute
                return _input;
            }

            return this.appendAttribute(_input, _methodContents);
        }

        private string appendAttribute(string _input, string _methodString)
        {
            if (_methodString.Contains("SysTestMethodAttribute"))
            {
                Hits++;
                _input = _input.Replace(_methodString, _methodString.Replace("SysTestMethodAttribute", "SysTestCheckInTest"));
            }
            else if (_methodString.Contains("SysTestMethod"))
            {
                Hits++;
                _input = _input.Replace(_methodString, _methodString.Replace("SysTestMethod", "SysTestCheckInTest"));
            }           

            return _input;
        }
    }
}
