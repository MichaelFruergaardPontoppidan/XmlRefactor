using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace XmlRefactor
{
    class AddZeroDeleteToTables : Rule
    {
        private readonly HashSet<string> tablesToAdd = new HashSet<string>();
        private const string flightName = "ITMAddShouldThrowExceptionOnZeroDeleteFlight";
        private const string ZeroDeleteSignature = "shouldThrowExceptionOnZeroDelete()";

        public AddZeroDeleteToTables() 
        {
            InitTablesToAdd(@"../../../XmlRefactorLib/RulesInput/TablesToAddZeroDelete.txt");
        }

        private void InitTablesToAdd(string filePath)
        {
            var stringArray = File.ReadAllLines(filePath);
            foreach (string item in stringArray)
            {
                var tableName = new string(item.TakeWhile((c) => c != '\n').ToArray());
                tablesToAdd.Add(tableName);
            }
        }

        public override string RuleName()
        {
            return "Add ZeroDelete to tables";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "ZeroDelete";
        }

        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("AxTable");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("AxTable");
        }

        public override string Run(string input)
        {
            string tableName = Path.GetFileNameWithoutExtension(Scanner.FILENAME);

            // Skip if table not in our list
            if (!tablesToAdd.Contains(tableName))
            {
                return input;
            }

            Debug.WriteLine("checking for add zero");
            Match match = xpoMatch.Match(input);

            if (!match.Success)
            {
                return input;
            }

            string xml = match.Value;

            if (xml.Contains(ZeroDeleteSignature))
            {
                // Table already has ZeroDelete
                return input;
            }

            return AddZeroDeleteMethodToTable(input, xml);            
        }

        private string AddZeroDeleteMethodToTable(string input, string xml)
        {
            // Check for existing methods
            string pattern = @"(?s)\]\]>\s*</Source>\s*</Method>\s*</Methods>";

            if (Regex.IsMatch(xml, pattern))
            {
                // Has existing methods
                string newMethodCode = CreateZeroDeleteMethod(flightName);
                string modifiedXml = Regex.Replace(xml, pattern, newMethodCode + "\r\n$&");
                return input.Replace(xml, modifiedXml);
            }

            // No existing methods - check for empty methods tag
            string emptyMethodsPattern = @"<Methods\s*/>";

            if (Regex.IsMatch(xml, emptyMethodsPattern))
            {
                // Replace empty Methods tag
                string newMethodCode = CreateZeroDeleteMethodNoExistingMethods(flightName);
                string modifiedXml = Regex.Replace(xml, emptyMethodsPattern, newMethodCode);
                return input.Replace(xml, modifiedXml);
            }

            return input;
        }

        private string CreateZeroDeleteMethod(string flightName)
        {
            return @"]]></Source>
			</Method>
			<Method>
				<Name>shouldThrowExceptionOnZeroDelete</Name>
				<Source><![CDATA[
    /// <summary>
    /// Determines if concurrent deletes should throw exception.
    /// </summary>
    /// <returns>true if exception should be thrown; otherwise false.</returns>
    [Hookable(false)]
    public boolean shouldThrowExceptionOnZeroDelete()
    {
        return super() || " + flightName + @"::instance().isEnabled();
    }
";
        }

        private string CreateZeroDeleteMethodNoExistingMethods(string flightName)
        {
            return @"		<Methods>
			<Method>
				<Name>shouldThrowExceptionOnZeroDelete</Name>
				<Source><![CDATA[
    /// <summary>
    /// Determines if concurrent deletes should throw exception.
    /// </summary>
    /// <returns>true if exception should be thrown; otherwise false.</returns>
    [Hookable(false)]
    public boolean shouldThrowExceptionOnZeroDelete()
    {
        return super() || " + flightName + @"::instance().isEnabled();
    }

]]></Source>
			</Method>
		</Methods>";
        }
    }
}
