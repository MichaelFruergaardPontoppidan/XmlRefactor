using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XpoRefactor
{
    class RuleDialogAddFieldTypes : Rule
    {
        protected virtual string methodName()
        {
            return "addField";
        }
        public override string ToString()
        {
            return "ID-Go-Away: dialog.addField(types:: -> dialog.addField(identifierStr(";
        }

        public override string Run(string input)
        {
            string classQualifier = "";

            Match match = Regex.Match(input, classQualifier + "." + this.methodName() + @"[ ]?[(][ ]?types[:][:]([\w]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string typeName = match.Groups[1].Value.Trim();
                
                string updatedInput = input.Remove(match.Index, match.Length);
                bool changed = false;

                switch (typeName)
                {
                    case "String":
                        updatedInput = updatedInput.Insert(match.Index, classQualifier + "." + this.methodName() + "(identifierStr(str)");
                        changed = true;
                        break;
                    case "Integer":
                        updatedInput = updatedInput.Insert(match.Index, classQualifier + "." + this.methodName() + "(identifierStr(int)");
                        changed = true;
                        break;
                    case "Int64":
                        updatedInput = updatedInput.Insert(match.Index, classQualifier + "." + this.methodName() + "(identifierStr(int64)");
                        changed = true;
                        break;
                    case "Real":
                        updatedInput = updatedInput.Insert(match.Index, classQualifier + "." + this.methodName() + "(identifierStr(real)");
                        changed = true;
                        break;
                    case "Date":
                        updatedInput = updatedInput.Insert(match.Index, classQualifier + "." + this.methodName() + "(identifierStr(date)");
                        changed = true;
                        break;
                    case "Guid":
                        updatedInput = updatedInput.Insert(match.Index, classQualifier + "." + this.methodName() + "(identifierStr(guid)");
                        changed = true;
                        break;
                }
                if (changed)
                {
                    return this.Run(updatedInput);
                }                
            }
            return input;
        }
    }
}
