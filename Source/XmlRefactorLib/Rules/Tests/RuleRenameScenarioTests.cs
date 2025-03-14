using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleRenameScenarioTests : Rule
    {
        public RuleRenameScenarioTests()
        {
        }

        public override string RuleName()
        {
            return "Rename scenario -> ScenarioTest";
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
                string filepath = Scanner.FILENAME;
                string filename = System.IO.Path.GetFileNameWithoutExtension(filepath);

                if (filename.EndsWith("Scenarios"))
                {
                    string source = match.Groups[2].Value;
                    string newName = (filename + "Test").Replace("ScenariosTest", "ScenarioTest");
                    string newSource = Regex.Replace(source, filename, newName, RegexOptions.IgnoreCase);

                    _input = _input.Replace(source, newSource);
                    Hits++;
                }
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
