using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleLockDownInternalUseOnly : Rule
    {
        HashSet<string> classMethodHashSet = new HashSet<string>();
        public RuleLockDownInternalUseOnly()
        {
            this.initializeClassMethodHashSet();
        }

        public override string RuleName()
        {
            return "Mark with InternalUseOnly";
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

        private bool isTargeted(string methodName)
        {
            string AOTPath = MetaData.AOTPath(methodName).ToLowerInvariant();
            return classMethodHashSet.Contains(AOTPath);
        }

        public string Run(string _input, int _startAt = 0)
        {
            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success &&
                !_input.ToLowerInvariant().Contains(" implements "))
            {
                string methodName = match.Groups[1].Value.Trim();
                if (this.isTargeted(methodName))
                {
                    string xml = _input;
                    var stringToUpdate = match.Value;

                    if (!stringToUpdate.Contains("Replaceable") &&
                        !stringToUpdate.Contains("QueryRangeFunction") &&
                        !stringToUpdate.Contains("Hookable") &&
                        !stringToUpdate.Contains("Wrappable") &&
                        !stringToUpdate.Contains("SysObsolete") &&
                        !stringToUpdate.Contains("DataMember"))
                    {

                        _input = this.appendAttribute(_input, stringToUpdate, methodName);
                        Hits++;
                        /*
                        using (StreamWriter sw = File.AppendText(@"e:\temp\MethodsUpdated.txt"))
                        {
                            sw.WriteLine(MetaData.AOTPath(methodName));
                        }
                        */
                    }
                }
                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }

        private void initializeClassMethodHashSet()
        {
            var stringArray = File.ReadAllLines(@"../../../XmlRefactorLib/RulesInput/MethodsToLockDown.txt");
            classMethodHashSet.Clear();
            foreach (var item in stringArray)
            {
                var item2 = item.Replace("/", "\\").ToLowerInvariant();
                if (!classMethodHashSet.Contains(item2))
                {
                    classMethodHashSet.Add(item2);
                }
            }
        }

        private int signatureLineStartPos(string source, string methodName)
        {
            int startPos = 0;
            string potentialLine = string.Empty;
            int pos2 = 0;
            do
            {
                int pos = source.IndexOf(" " + methodName, startPos);
                pos2 = source.LastIndexOf(Environment.NewLine, pos) + 1;
                potentialLine = source.Substring(pos2, pos-pos2).TrimStart();
                startPos = pos + 1;
            } 
            while (potentialLine.StartsWith("//"));

            return pos2;
        }

        private string getLineAtPos(string source, int pos)
        {
            int pos2 = source.LastIndexOf(Environment.NewLine, pos) + 1;
            return source.Substring(pos2, pos - pos2);
        }

        private int attributeEndPos(string source, int posOfSignatureLine)
        {
            int startPos = posOfSignatureLine;
            string potentialLine = string.Empty;
            int pos = 0;
            do
            {
                pos = source.LastIndexOf("]", startPos);
                if (pos > 0)
                {
                    potentialLine = this.getLineAtPos(source, pos).TrimStart();
                    startPos = pos - 1;
                }
            }
            while (pos > 0 && potentialLine.StartsWith("//"));

            return pos;
        }

        private string appendAttribute(string _input, string source, string _methodName)
        {
            string attribute = "Microsoft.Dynamics.BusinessPlatform.SharedTypes.InternalUseOnlyAttribute";

            if (source.Contains(attribute))
            {
                return _input;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, _methodName) + 1;
            string theline = source.Substring(signatureLinePosInSource);

            if (theline.ToLowerInvariant().Contains("private "))
            {
                return _input;
            }

            int attributeEndPosInSource = this.attributeEndPos(source, signatureLinePosInSource);

            if (attributeEndPosInSource > 0)
            {
                int pos = _input.IndexOf(source) + attributeEndPosInSource;
                _input = _input.Insert(pos, ", " + attribute);
            }
            else
            {
                int spaces = theline.Length - theline.TrimStart().Length;
                int pos = _input.IndexOf(source) + signatureLinePosInSource;
                _input = _input.Insert(pos, " ".PadLeft(spaces) + "[" + attribute + "]" + Environment.NewLine);
            }
            return _input;
        }
    }
}
