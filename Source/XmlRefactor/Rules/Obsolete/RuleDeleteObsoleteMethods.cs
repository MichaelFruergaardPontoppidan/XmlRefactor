using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleDeleteObsoleteMethods : Rule
    {
        HashSet<string> methodsToDelete = new HashSet<string>();

        public RuleDeleteObsoleteMethods()
        {
            this.initMethodsToDelete(@"../../RulesInput/MethodsToDelete.txt");
        }

        public override string RuleName()
        {
            return "Delete obsolete error methods";
        }

        public override bool Enabled()
        {
            return false;
        }
        override public string Grouping()
        {
            return "Obsolete";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddXMLStart("Method", false);
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddXMLStart("Name", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Name");
        
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Method");
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
                string methodName = MetaData.extractNextXMLElement("Name", match.Index, _input);
                if (methodName != string.Empty)
                {
                    string AOTPath = MetaData.AOTPath(methodName).ToLowerInvariant();
                    if (methodsToDelete.Contains(AOTPath))
                    {
                        string text = _input.Substring(match.Index, match.Length);
                        if (!text.Contains("SysObsolete"))
                        {
                            Debug.WriteLine(string.Format("{0} isn't marked with SysObsolete", AOTPath));
                        }
                        string updatedInput = _input.Remove(match.Index, match.Length);
                        Hits++;
                        return this.Run(updatedInput, match.Index);
                    }
                }
                _input = this.Run(_input, match.Index + 50);
            }

            return _input;
        }

        private void initMethodsToDelete(string file)
        {
            var stringArray = File.ReadAllLines(file);
            foreach (var item in stringArray)
            {
                var item2 = item.Replace("/", "\\").ToLowerInvariant();
                if (!methodsToDelete.Contains(item2))
                {
                    methodsToDelete.Add(item2);
                }
            }
        }
    }
}
