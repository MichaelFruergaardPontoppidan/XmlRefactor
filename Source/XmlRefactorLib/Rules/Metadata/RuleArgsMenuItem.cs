using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace XmlRefactor
{
    class RuleArgsMenuItem : Rule
    {
        string methodName;
        string formName;

        public override string RuleName()
        {
            return "Missing args.menuitem";
        }

        public override bool Enabled()
        {
            return false;
        }

        public override string Grouping()
        {
            return "Metadata";
        }
        protected override void buildXpoMatch()
        {
            //xpoMatch.AddWhiteSpaceRequired();
           // xpoMatch.AddNewLine();
            xpoMatch.AddCaptureWord();
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddSymbol("=");
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddLiteral("new");
            xpoMatch.AddWhiteSpaceRequired();
            xpoMatch.AddLiteral("Args");
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddLiteral("formstr");
            xpoMatch.AddStartParenthesis();
            xpoMatch.AddCapture();
            xpoMatch.AddEndParenthesis();
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddSymbol("}");
        }
        public override string Run(string input)
        {
            return this.Run(input, 0);
        }
        public string getMenuItemsAsString(List<string> list)
        {
            string result = string.Empty;
            foreach (var s in list)
            {
                if (result == string.Empty)
                    result = s;
                else
                    result += "," + s;
            }
            return result;
        }

        private void log(string message, string severity = "info")
        {
            using (StreamWriter sw = File.AppendText(this.logFileName())) 
            {
                sw.WriteLine(string.Format("{0};{1};{2};{3};{4}", severity, Scanner.FILENAME, formName, methodName, message));
            }
        }

        public string Run(string input, int startAt = 0)
        {
            Match match = xpoMatch.Match(input, startAt);

            if (match.Success)
            {
                string xml = match.Value;
                string varName = match.Groups[1].Value.Trim();
                formName = match.Groups[2].Value.Replace('\"', ' ').Replace('\'', ' ').Trim();

                methodName = MetaData.extractPreviousXMLElement("Name", match.Index, input);

                if (MetaData.isCodeProtectedByMenuItem(input, match.Index))
                {
                    this.log("Clicked method protected by menu item.");
                    return this.Run(input, match.Index + match.Length); ;
                }

                if (xml.ToLowerInvariant().Contains(".menuitemname") ||
                    xml.ToLowerInvariant().Contains(".run(" + varName + ")"))
                {
                    this.log("Already uses menu item.");
                    return this.Run(input, match.Index + match.Length);;
                }
                
                if (MetaData.isFormLookup(formName))
                {
                    this.log("Is lookup form");
                    return this.Run(input, match.Index + match.Length);
                }

                if (MetaData.isFormDialog(formName))
                {
                    this.log("Is dialog form");
                    return this.Run(input, match.Index + match.Length);
                }

                var datasources = MetaData.getFormRootDataSources(formName);

                if (datasources.Count == 0)
                {
                    this.log("Has no data sources");
                    return this.Run(input, match.Index + match.Length);
                }

                bool allDatasourcesTmp = true;
                foreach (var table in datasources)
                {
                    if (MetaData.isTablePersisted(table))
                    {
                        allDatasourcesTmp = false;
                    }
                }

                if (allDatasourcesTmp)
                {
                    this.log("Only has temporary data sources");
                    return this.Run(input, match.Index + match.Length); ;
                }
               
                var menuItems = MetaData.getMenuItemsFromFormName(formName);
                if (menuItems.Count == 0)
                {
                    this.log("Has no menu item, but datasources: " + getMenuItemsAsString(datasources), "Warning");
                    return this.Run(input, match.Index + match.Length); ;
                }
                string menuItem = string.Empty;

                if (menuItems.Count == 1)
                {
                    menuItem = menuItems[0];
                    this.log("Has one menu item:" + menuItem + ", and datasources: " + getMenuItemsAsString(datasources), "Fixable");
                }
                
                if (menuItem == string.Empty && datasources.Count == 1)
                {
                    var formRef = MetaData.tableFormRef(datasources[0]);
                    if (formRef != string.Empty && 
                        menuItems.Contains(formRef))
                    {
                        if (MetaData.isFormUsingArgsMenuItemNameAndMenuItem(formName, formRef))
                        {
                            this.log("FormRef points to menu item, but form uses args.menuItemName() and menuitemdisplaystr(" + formRef + ")", "Warning");
                            return this.Run(input, match.Index + match.Length); ;
                        }
                        menuItem = formRef;
                        this.log("Has one data source with formRef: " + menuItem + ", and datasources: " + getMenuItemsAsString(datasources) + ", and menu items:" + getMenuItemsAsString(menuItems), "Fixable");
                    }
                }

                if (menuItem == string.Empty && menuItems.Contains(formName))
                {
                    menuItem = formName;
                    if (MetaData.isFormUsingArgsMenuItemName(formName))
                    {
                        this.log("Has multiple menu items:" + getMenuItemsAsString(menuItems) + ", and datasources: " + getMenuItemsAsString(datasources)+", and form uses args.menuItemName().", "Warning");
                        return this.Run(input, match.Index + match.Length); ;
                    }
                    this.log("Updated. Has menu item with same name as form. Menu items:" + getMenuItemsAsString(menuItems) + ", and datasources: " + getMenuItemsAsString(datasources), "Fixable");
                }

                if (menuItem != string.Empty)
                {
                    int pos = xml.IndexOf(';');
                    int nextLineStart = xml.ToLowerInvariant().IndexOfAny("abcdefghijklmnopqrstuvwxyz".ToCharArray(), pos);
                    if (nextLineStart > 0)
                    {
                        string whitespace = xml.Substring(pos, nextLineStart - pos);
                        xml = xml.Insert(nextLineStart, whitespace);
                        xml = xml.Insert(nextLineStart, varName + ".menuItemName(menuItemDisplayStr(" + menuItem + "))");
                        string updatedInput = input.Remove(match.Index, match.Length);
                        updatedInput = updatedInput.Insert(match.Index, xml);
                        Hits++;
                        return this.Run(updatedInput, match.Index + match.Length);
                    }
                    else
                    {
                        this.log("Cannot update file due to format", "Error");
                    }
                }
                else
                {
                    this.log("Has multiple menu items:" + getMenuItemsAsString(menuItems) + ", and datasources: " + getMenuItemsAsString(datasources), "Warning");
                }
                
                return this.Run(input, match.Index + match.Length);

            }

            return input;
        }
    }
}
