using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleObsoleteAddDate : Rule
    {
        Dictionary<string, string> obsoleteDates = new Dictionary<string, string>();
        public RuleObsoleteAddDate()
        {
            this.initObsoleteDateFromFile(@"../../../XmlRefactorLib/RulesInput/ObsoleteIn70.txt", @"31\01\2016");
            this.initObsoleteDateFromFile(@"../../../XmlRefactorLib/RulesInput/ObsoleteIn72.txt", @"31\05\2017");
            this.initObsoleteDateFromFile(@"../../../XmlRefactorLib/RulesInput/ObsoleteIn73.txt", @"30\11\2017");
            this.initObsoleteDateFromFile(@"../../../XmlRefactorLib/RulesInput/ObsoleteIn80.txt", @"31\03\2018");
            this.initObsoleteDateFromFile(@"../../../XmlRefactorLib/RulesInput/ObsoleteIn81.txt", @"30\06\2018");
            this.initObsoleteDateFromFile(@"../../../XmlRefactorLib/RulesInput/ObsoleteIn100.txt", @"31\03\2019");
            this.initObsoleteDateFromFile(@"../../../XmlRefactorLib/RulesInput/ObsoleteIn107.txt", @"30\11\2019");
            this.initObsoleteDateFromFile(@"../../../XmlRefactorLib/RulesInput/ObsoleteIn1013.txt", @"30\6\2020");
            this.initObsoleteDateFromFile(@"../../../XmlRefactorLib/RulesInput/ObsoleteIn1017.txt", @"30\11\2020");
        }

        public override string RuleName()
        {
            return "Add date to SysObsolete";
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
            /*
            xpoMatch.AddXMLStart("Method", false);
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddXMLStart("Name", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Name");
            */
            xpoMatch.AddLiteral("SysObsolete");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddEndParenthesis();
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddEndBracket();
            /*
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Method");
            */
        }

        public override string Run(string _input)
        {
            return this.Run(_input, 0);
        }


        private string getDate(string methodName)
        {
            string AOTPath = MetaData.AOTPath(methodName).ToLowerInvariant();

            if (obsoleteDates.ContainsKey(AOTPath))
            {
                return obsoleteDates[AOTPath];
            }
            return @"30\06\2021";
    }

        public string Run(string _input, int _startAt = 0)
        {
            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success)
            {
                string methodName = MetaData.extractPreviousXMLElement("Name", match.Index, _input);
                if (methodName != string.Empty)
                {
                    string obsAttr = match.Groups[0].Value.Trim();
                    Boolean isParamsGiven = obsAttr.IndexOf('(') < obsAttr.IndexOf(']');

                    if (!isParamsGiven)
                    {
                        obsAttr = obsAttr.Substring(0, obsAttr.IndexOf(']'));
                    }
                    else
                    {
                        int pos = this.findEndParanthesis(obsAttr);
                        if (pos == -1)
                        {
                            return this.Run(_input, match.Index + 50);
                        }
                        obsAttr = obsAttr.Substring(0, pos);
                    }

                    //Already has a date
                    if (!obsAttr.Contains(@"\"))
                    {
                        string obsoleteDate = this.getDate(methodName);
                        if (obsoleteDate != string.Empty)
                        {
                            string newObsAttr = obsAttr.Trim();

                            if (!isParamsGiven)
                            {
                                newObsAttr += "(''";
                            }

                            if (!newObsAttr.ToLowerInvariant().EndsWith("false") &&
                                !newObsAttr.ToLowerInvariant().EndsWith("true"))
                            {
                                if (newObsAttr.Trim().ToLowerInvariant().EndsWith("'") ||
                                    newObsAttr.Trim().ToLowerInvariant().EndsWith("\""))
                                {
                                    newObsAttr += ", false";
                                }
                                else
                                {
                                    newObsAttr += "'', false";
                                }
                            }

                            newObsAttr += ", " + obsoleteDate;

                            if (!isParamsGiven)
                            {
                                newObsAttr += ")";
                            }

                            int attributePos = newObsAttr.ToLowerInvariant().IndexOf("sysobsoleteattribute");
                            if (attributePos > -1)
                            {
                                newObsAttr = newObsAttr.Remove(attributePos + "sysobsolete".Length, "attribute".Length);
                            }

                            int pos = _input.IndexOf(obsAttr, _startAt);
                            string updatedInput = _input.Remove(pos, obsAttr.Length);
                            updatedInput = updatedInput.Insert(pos, newObsAttr);
                            Hits++;
                            return this.Run(updatedInput, match.Index + 50);
                        }
                    }
                }
                _input = this.Run(_input, match.Index + 50);
            }

            return _input;
        }

        private int findEndParanthesis(string input)
        {
            int pos = 0;
            Boolean insideSingleQString = false;
            Boolean insideDoubleQString = false;
            foreach (char c in input.ToCharArray())
            {
                switch (c)
                {
                    case '\'':
                        if (!insideDoubleQString)
                        {
                            insideSingleQString = !insideSingleQString;
                        }
                        break;

                    case '"':
                        if (!insideSingleQString)
                        {
                            insideDoubleQString = !insideDoubleQString;
                        }
                        break;

                    case ')':
                        if (!insideSingleQString && !insideDoubleQString)
                        {
                            return pos;
                        }
                        break;
                }
                pos++;
            }
            return -1;
        }

        private void initObsoleteDateFromFile(string file, string date)
        {
            var stringArray = File.ReadAllLines(file);
            foreach (var item in stringArray)
            {
                var item2 = item.Replace("/", "\\").ToLowerInvariant();
                if (!obsoleteDates.ContainsKey(item2))
                {
                    obsoleteDates.Add(item2, date);
                }
            }
        }
    }
}
