using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XpoRefactor
{
    class RuleDialogAddFieldValueTypes : RuleDialogAddFieldTypes
    {
        protected override string methodName()
        {
            return "addFieldValue";
        }
        public override string ToString()
        {
            return "ID-Go-Away: dialog.addFieldValue(types:: -> dialog.addFieldValue(identifierStr(";
        }
    }
}
