using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleLockDownClassInternal : Rule
    {
        public RuleLockDownClassInternal()
        {
        }

        public override string RuleName()
        {
            return "Mark class as internal final";
        }

        public override bool Enabled()
        {
            return false;
        }
        override public string Grouping()
        {
            return "Lock down";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("Declaration", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Declaration");
        }

        public override string Run(string _input)
        {
            return this.Run(_input, 0);
        }

        private bool isTargeted(string methodName)
        {
            return true;
        }

        public string Run(string _input, int _startAt = 0)
        {
            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success)
            {
                string source = match.Groups[1].Value;
                int pos = 0;
                string line = String.Empty;
                do
                {
                    pos = source.ToLowerInvariant().IndexOf("class ", pos) + 6;
                    line = this.getLineAtPos(source, pos);
                } 
                while (line.TrimStart().StartsWith("//"));

                string newLine = line;
          
                if (line.ToLowerInvariant().Contains("internal final "))
                {
                    return _input;
                }
                if (line.ToLowerInvariant().Contains("public "))
                {
                    newLine = Regex.Replace(line, "public ", "internal ", RegexOptions.IgnoreCase);
                }
                else if (!line.ToLowerInvariant().Contains("internal "))
                {
                    newLine = Regex.Replace(line, "class ", "internal class ", RegexOptions.IgnoreCase);
                }
                if (!line.ToLowerInvariant().Contains("abstract ") &&
                    !line.ToLowerInvariant().Contains("final "))
                {
                    newLine = Regex.Replace(newLine, "internal ", "internal final ", RegexOptions.IgnoreCase);
                }

                // Re-order
                if (newLine.ToLowerInvariant().Contains("final internal "))
                {
                    newLine = Regex.Replace(newLine, "final internal ", "internal final ", RegexOptions.IgnoreCase);
                }
                
                _input = _input.Replace(line, newLine);
                Hits++;
                _input = this.Run(_input, match.Index + 1);
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
