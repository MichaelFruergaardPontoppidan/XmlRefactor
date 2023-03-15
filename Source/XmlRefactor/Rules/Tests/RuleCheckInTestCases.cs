using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleCheckInTestCases : Rule
    {
        HashSet<string> classMethodHashSet = new HashSet<string>();
        public RuleCheckInTestCases()
        {
            this.initializeClassMethodHashSet();
        }

        public override string RuleName()
        {
            return "Enable Test Cases";
        }

        public override bool Enabled()
        {
            return false;
        }
        override public string Grouping()
        {
            return "Checkin Tests";
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
                string xml = _input;
                var stringToUpdate = match.Value;
                string methodName = MetaData.extractPreviousXMLElement("Name", match.Index, _input);
                var sourceFile = Path.GetFileNameWithoutExtension(Scanner.FILENAME);
                _input = this.processSource(_input, stringToUpdate, sourceFile + "." + methodName);
                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }

        private void initializeClassMethodHashSet()
        {
            var stringArray = File.ReadAllLines(@"../../RulesInput/CITs.csv");
            classMethodHashSet.Clear();
            foreach (var item in stringArray)
            {
                var classMethodPair = item.Split(new char[] { ',' });
                if (!classMethodHashSet.Contains(classMethodPair[0] + "." + classMethodPair[1]))
                {
                    classMethodHashSet.Add(classMethodPair[0] + "." + classMethodPair[1]);
                }
            }
        }

        private string processSource(string _input, string _methodContents, string _methodName)
        {
            if (classMethodHashSet.Contains(_methodName))
            {
                return this.appendAttribute(_input, _methodContents);
            }

            return _input;
        }

        private string appendAttribute(string _input, string _methodString)
        {
            if (_methodString.Contains("SysTestCheckinTest") || _methodString.Contains("SysTestCheckinTestAttribute") || _methodString.Contains("SysTestCheckInTest") || _methodString.Contains("SysTestCheckInTestAttribute"))
            {
                return _input;
            }

            if (_methodString.Contains("SysTestMethodAttribute"))
            {
                _input = _input.Replace(_methodString, _methodString.Replace("SysTestMethodAttribute", "SysTestMethod, SysTestCheckInTest"));
            }
            else if (_methodString.Contains("SysTestMethod"))
            {
                _input = _input.Replace(_methodString, _methodString.Replace("SysTestMethod", "SysTestMethod, SysTestCheckInTest"));
            }
            else if (_methodString.Contains("public void "))
            {
                _input = _input.Replace(_methodString, _methodString.Replace("public void ", "[SysTestMethod, SysTestCheckInTest]" + Environment.NewLine + "public void "));
            }
            else if (_methodString.Contains(" void "))
            {
                _input = _input.Replace(_methodString, _methodString.Replace(" void ", "[SysTestMethod, SysTestCheckInTest]" + Environment.NewLine + "public void "));
            }

            return _input;
        }
    }
}
