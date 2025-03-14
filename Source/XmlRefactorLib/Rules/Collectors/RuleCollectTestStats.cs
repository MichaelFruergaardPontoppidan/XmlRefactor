using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleCollectTestStats : Rule
    {
        public RuleCollectTestStats()
        {
        }

        public override string RuleName()
        {
            return "Collect test statistics";
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
                
                string filepath = Scanner.FILENAME;
                string filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
                                
                using (StreamWriter sw = File.AppendText(this.logFileName()))
                {
                    bool extendsAtlWHSTestCase = source.Contains("AtlWHSTestCase");
                    if (extendsAtlWHSTestCase)
                    {
                        bool hasDetour = source.Contains("SysDetourContext");
                        bool hasDataDependency = source.Contains("SysTestCaseDataDependency");
                        bool hasAppTrackerAssertContext = source.Contains("AppTrackerAssertContext");
                        bool hasUnitTestData = source.Contains("SysUnitTestData") || source.Contains("DirTestHelper");
                        bool isUnitTest = source.Contains("SysTestGranularity::Unit") ||                                          
                                          source.Contains("SysTestTarget");
                        bool useMobileRunner = source.Contains("WHSMobileAppTestRunner");
                        int doInserts = Regex.Matches(source, @"\.doInsert\(\)", RegexOptions.IgnoreCase).Count;
                        int doUpdates = Regex.Matches(source, @"\.doUpdate\(\)", RegexOptions.IgnoreCase).Count;

                        sw.WriteLine(string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", 
                            filename, 
                            hasDetour, 
                            hasDataDependency, 
                            hasAppTrackerAssertContext, 
                            hasUnitTestData, 
                            isUnitTest, 
                            useMobileRunner, 
                            doUpdates, 
                            doInserts));
                    }

                    Hits++;
                }


                Hits++;

            }

            return _input;
        }
            

    }
}
