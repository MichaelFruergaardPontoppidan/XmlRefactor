using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleATLMethod : Rule
    {
        HashSet<string> classMethodHashSet = new HashSet<string>();
        public RuleATLMethod()
        {
        }

        public override string RuleName()
        {
            return "Update methods";
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


        protected string doAtlData(string stringToUpdate, string signatureLine, string methodName)
        {
            var updatedString = stringToUpdate;

            if (Scanner.FILENAME.ToLowerInvariant().Contains("atldata"))
            {
                if (methodName.ToLowerInvariant() != "new" ||
                    this.isReturning(signatureLine, methodName, "AtlData") ||
                    this.isReturning(signatureLine, methodName, "AtlQuery") ||
                    this.isReturning(signatureLine, methodName, "AtlSpec")
                    )
                {

                    updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                    signatureLine = this.signatureLine(updatedString, methodName);
                    updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                    //   updatedString = this.fixDataSetupObject(updatedString);
                }
            }

            return updatedString;
        }
        protected string doAtlSpec(string stringToUpdate, string signatureLine, string methodName)
        {
            var updatedString = stringToUpdate;

            if (Scanner.FILENAME.ToLowerInvariant().Contains("atlspec"))
            {
                switch (methodName.ToLowerInvariant())
                {
                    case "tostring":
                        updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        break;
                    case "basetableid":
                        updatedString = this.makeProtectedFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                        break;
                    case "newfrombuffer":
                        updatedString = this.appendAttribute(stringToUpdate, signatureLine, "Hookable(false)");
                        break;
                    case "issatisfiedby":
                    case "actualdescription":
                        updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        break;
                    case "construct":
                    case "expectedrecordtableid":
                    case "entityat":
                    case "firstentity":
                    case "secondentity":
                    case "getrecordbyprimarykey":
                    case "lastentity":
                    case "firstordefaultentity":
                    case "defaultifempty":
                    case "singleentity":
                        updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                        updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        break;

                    default:
                        if (methodName.ToLowerInvariant().StartsWith("new") &&
                            methodName.ToLowerInvariant() != "new" &&
                            signatureLine.Contains("static "))
                        {
                            updatedString = this.appendAttribute(stringToUpdate, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("set"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("single") ||
                            methodName.ToLowerInvariant().StartsWith("second") ||
                            methodName.ToLowerInvariant().StartsWith("first")
                            )
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("with"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendFieldNumDerivedAttribute(updatedString, signatureLine, "(addQueryRange|addQueryRangeExpression|parmExpectedFieldValue|withExpectedFieldValue)");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("for"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendFieldNumDerivedAttribute(updatedString, signatureLine, "(addQueryRange|addQueryRangeExpression|parmExpectedFieldValue)");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }

                        else if (signatureLine.Contains("protected ") && methodName.ToLowerInvariant() != "new")
                        {
                            updatedString = this.makeProtectedFinal(stringToUpdate, signatureLine);
                        }
                        else if (this.isReturning(signatureLine, methodName, "ATlSpec") &&
                            signatureLine.Contains("public "))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (this.isReturning(signatureLine, methodName, "QueryBuildDataSour") &&
                            signatureLine.Contains("public "))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        }
                        break;
                }
            }

            return updatedString;
        }

        protected string doAtlCommandAndCreator(string stringToUpdate, string signatureLine, string methodName)
        {
            var updatedString = stringToUpdate;

            if (Scanner.FILENAME.ToLowerInvariant().Contains("atlcommand") ||
                Scanner.FILENAME.ToLowerInvariant().Contains("atlcreator"))
            {
                switch (methodName.ToLowerInvariant())
                {
                    case "tostring":
                        updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        break;                   
                    case "construct":
                        updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        break;

                    default:
                        if (methodName.ToLowerInvariant().StartsWith("new") &&
                            methodName.ToLowerInvariant() != "new" &&
                            signatureLine.Contains("static "))
                        {
                            updatedString = this.appendAttribute(stringToUpdate, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("set"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("with"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendFieldNumDerivedAttribute(updatedString, signatureLine, "(addQueryRange|addQueryRangeExpression|parmExpectedFieldValue|withExpectedFieldValue)");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("for"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendFieldNumDerivedAttribute(updatedString, signatureLine, "(addQueryRange|addQueryRangeExpression|parmExpectedFieldValue)");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (signatureLine.Contains("protected ") && methodName.ToLowerInvariant() != "new")
                        {
                            updatedString = this.makeProtectedFinal(stringToUpdate, signatureLine);
                        }
                        else if (signatureLine.Contains("public ") &&
                                !signatureLine.Contains("abstract ") &&
                                methodName.ToLowerInvariant() != "new")
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        break;
                }
            }

            return updatedString;
        }


        protected string doAtlQuery(string stringToUpdate, string signatureLine, string methodName)
        {
            var updatedString = stringToUpdate;

            if (Scanner.FILENAME.ToLowerInvariant().Contains("atlquery"))
            {
                switch (methodName.ToLowerInvariant())
                {
                    case "basetableid":
                        updatedString = this.makeProtectedFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                        break;
                    case "newfrombuffer":
                        updatedString = this.appendAttribute(stringToUpdate, signatureLine, "Hookable(false)");
                        break;
                    case "construct":
                    case "entityat":
                    case "firstentity":
                    case "secondentity":
                    case "lastentity":
                    case "firstordefaultentity":
                    case "defaultifempty":
                    case "singleentity":
                        updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                        updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        break;

                    default:
                        if (methodName.ToLowerInvariant().StartsWith("new") &&
                            methodName.ToLowerInvariant() != "new" &&
                            signatureLine.Contains("static "))
                        {
                            updatedString = this.appendAttribute(stringToUpdate, signatureLine, "Hookable(false)");
                        }

                        else if (methodName.ToLowerInvariant().StartsWith("single") ||
                            methodName.ToLowerInvariant().StartsWith("second") ||
                            methodName.ToLowerInvariant().StartsWith("first")
                            )
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("with"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendFieldNumDerivedAttribute(updatedString, signatureLine, "(addQueryRange|addQueryRangeExpression)");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("for"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendFieldNumDerivedAttribute(updatedString, signatureLine, "(addQueryRange|addQueryRangeExpression)");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }

                        else if (signatureLine.Contains("protected ") && methodName.ToLowerInvariant() != "new")
                        {
                            updatedString = this.makeProtectedFinal(stringToUpdate, signatureLine);
                        }
                        else if (this.isReturning(signatureLine, methodName, "ATlQuery"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (this.isReturning(signatureLine, methodName, "QueryBuildDataSour") &&
                            signatureLine.Contains("public "))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        }
                        break;
                }
            }
            return updatedString;
        }

        protected string doAtlEntity(string stringToUpdate, string signatureLine, string methodName)
        {
            if (signatureLine.Contains("delegate "))
                return stringToUpdate;

            var updatedString = stringToUpdate;

            if (Scanner.FILENAME.ToLowerInvariant().Contains("atlentity") &&
                !Scanner.FILENAME.ToLowerInvariant().Contains("atlentityloadline"))
            {
                switch (methodName.ToLowerInvariant())
                {
                    case "lines":
                        updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        break;
                    case "new":
                        updatedString = this.appendAttribute(stringToUpdate, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                        break;
                    case "shouldcallmodifiedfield":
                    case "find":
                    case "save":
                    case "record":
                    case "init":
                    case "inventdim":
                    case "inventdims":
                    case "construct":
                    case "newfromrecord":
                        updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                        updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        break;
                    case "mainrecordbase":
                        updatedString = this.makeProtectedFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                        break;
                    default:
                        if (methodName.ToLowerInvariant().StartsWith("new") &&
                            methodName.ToLowerInvariant() != "new" &&
                            signatureLine.Contains("static "))
                        {
                            updatedString = this.appendAttribute(stringToUpdate, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("parm") ||
                            methodName.ToLowerInvariant().StartsWith("set"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendFieldNumDerivedAttribute(updatedString, signatureLine, "(parmMainRecordField)");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "SysGeneratedCode('ATLGenerator', '1.0.0.0')");
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (methodName.ToLowerInvariant().StartsWith("add"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (this.isReturning(signatureLine, methodName, "ATlQuery"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (this.isReturning(signatureLine, methodName, "ATlCommand"))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        else if (signatureLine.Contains("public "))
                        {
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                        }
                        break;
                }

            }

            return updatedString;
        }

        public string Run(string _input, int _startAt = 0)
        {
            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success)
            {
                string methodName = match.Groups[1].Value.Trim();
                string xml = _input;
                var stringToUpdate = match.Value;
                var updatedString = stringToUpdate;

                string signatureLine = this.signatureLine(stringToUpdate, methodName);

                if (!Scanner.FILENAME.ToLowerInvariant().Contains("_extension"))
                {
                    updatedString = this.doAtlData(updatedString, signatureLine, methodName);
                    updatedString = this.doAtlSpec(updatedString, signatureLine, methodName);
                    updatedString = this.doAtlQuery(updatedString, signatureLine, methodName);
                    updatedString = this.doAtlCommandAndCreator(updatedString, signatureLine, methodName);

                    if (!_input.Contains("interface "))
                    {
                        updatedString = this.doAtlEntity(updatedString, signatureLine, methodName);
                    }
                }
                
                 /*
                if (Scanner.FILENAME.ToLowerInvariant().Contains("atlentity") &&
                    Scanner.FILENAME.ToLowerInvariant().Contains("_extension"))
                {
                    switch (methodName.ToLowerInvariant())
                    {
                        case "save":
                            updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                            signatureLine = this.signatureLine(updatedString, methodName);
                            updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                            break;
                    }
                    if (methodName.ToLowerInvariant().StartsWith("set"))
                    {
                        updatedString = this.makePublicFinal(stringToUpdate, signatureLine);
                        signatureLine = this.signatureLine(updatedString, methodName);
                        updatedString = this.appendAttribute(updatedString, signatureLine, "Hookable(false)");
                    }

                }
                */
               
                if (updatedString != stringToUpdate)
                {
                    Hits++;
                    _input = _input.Replace(stringToUpdate, updatedString);
                }

                _input = this.Run(_input, match.Index + 10);
            }

            return _input;
        }


        private string appendFieldNumDerivedAttribute(string stringToUpdate, string signatureLine, string callerPrefix)
        {
            if (stringToUpdate.Contains("AtlDependentFluentSetter") ||
                stringToUpdate.Contains("AtlDependentField") ||
                stringToUpdate.Contains("AtlDependentMethod") ||
                stringToUpdate.Contains("AtlDependentRelation")
                )
            {
                return stringToUpdate;
            }
            Regex reg = new Regex(callerPrefix+@"\(fieldnum\(([\w\,\W]+?)\)", RegexOptions.IgnoreCase);
            Match match = reg.Match(stringToUpdate);
            if (match.Success)
            {
                string depValue = match.Groups[2].Value;
                string attr = "AtlDependentField(fieldStr(" + depValue+"))";
                return this.appendAttribute(stringToUpdate, signatureLine, attr);
            }
            /*
            if (callerPrefix.Contains("parmMainRecordField"))
            {
                reg = new Regex(@"return\W*([\w]+?)\.([\w]+?)\;", RegexOptions.IgnoreCase);
                match = reg.Match(stringToUpdate);
                if (match.Success)
                {
                    string tableName = match.Groups[1].Value;
                    string fieldName = match.Groups[2].Value;
                    string attr = "AtlFieldDependency(fieldStr(" + tableName +", " + fieldName +"))";
                    return this.appendAttribute(stringToUpdate, signatureLine, attr);
                }
            }
            */
            reg = new Regex(@"this\.([\w]+?)\(", RegexOptions.IgnoreCase);
            match = reg.Match(stringToUpdate);
            if (match.Success)
            {
                string depValue = match.Groups[1].Value;
                if (depValue.ToLowerInvariant().StartsWith("with"))
                {
                    string className = Path.GetFileNameWithoutExtension(Scanner.FILENAME);
                    string attr = "AtlDependentFluentSetter(methodStr(" + className + ", " + depValue + "))";
                    return this.appendAttribute(stringToUpdate, signatureLine, attr);
                }
            }


            return stringToUpdate;
        }

        private string fixDataSetupObject(string stringToUpdate)
        {
            Regex reg = new Regex(@"data.dataSetupObject\(classNum\(([\w]+?)\)\);", RegexOptions.IgnoreCase);
            Match match = reg.Match(stringToUpdate);
            if (match.Success)
            {
                string className = match.Groups[1].Value.Trim();
                stringToUpdate = stringToUpdate.Remove(match.Index, match.Length).Insert(match.Index, "new " + className + "();");
            }
            return stringToUpdate;
        }

        private bool isReturning(string signatureLine, string methodName, string type)
        {
            Regex reg = new Regex(type + @"([\w]+?)\W*" + methodName, RegexOptions.IgnoreCase);
            Match match = reg.Match(signatureLine);
            return match.Success;
        }

        private string signatureLine(string source, string methodName)
        {
            if (methodName.ToLowerInvariant() == "classdeclaration")
                return methodName;
            int startPos = 1;
            string res = String.Empty;
            do
            {
                startPos++;
                int pos = source.IndexOf(" "+methodName, startPos);
                if (pos > 0)
                {
                    int lineStart = source.LastIndexOf(Environment.NewLine, pos) + 2;
                    int lineEnd = source.IndexOf(Environment.NewLine, lineStart);
                    res = source.Substring(lineStart, lineEnd - lineStart);
                }
            }
            while (res.TrimStart().StartsWith("//") || res.TrimStart().StartsWith("["));

            return res; 
        }
        private string makeProtectedFinal(string stringToUpdate, string signatureLine)
        {
            if (signatureLine.Contains("internal ") ||
                signatureLine.Contains("private ") ||
                signatureLine.Contains("static "))
            {
                return stringToUpdate;
            }
            signatureLine = signatureLine.Trim();
            int pos = stringToUpdate.IndexOf(signatureLine);
            signatureLine = signatureLine.ToLowerInvariant();
            if (signatureLine.Contains("protected "))
            {
                pos += signatureLine.IndexOf("protected ") + "protected ".Length;

                if (!signatureLine.Contains("final ") && !signatureLine.Contains("abstract "))
                    stringToUpdate = stringToUpdate.Insert(pos, "final ");
            }
            else if (signatureLine.Contains("public "))
            {
                pos += signatureLine.IndexOf("public ") + "public ".Length;

                if (!signatureLine.Contains("final ") && !signatureLine.Contains("abstract "))
                    stringToUpdate = stringToUpdate.Insert(pos, "final ");
            }
            else
            {
                if (!signatureLine.Contains("final ") && !signatureLine.Contains("abstract "))
                    stringToUpdate = stringToUpdate.Insert(pos, "final ");

                if (!signatureLine.Contains("protected "))
                    stringToUpdate = stringToUpdate.Insert(pos, "protected ");
            }
            return stringToUpdate;
        }

        private string makePublicFinal(string stringToUpdate, string signatureLine)
        {
            if (signatureLine.Contains("internal ") ||
                signatureLine.Contains("private ") ||
                signatureLine.Contains("static "))
            {
                return stringToUpdate;
            }
            signatureLine = signatureLine.Trim();
            int pos = stringToUpdate.IndexOf(signatureLine);
            signatureLine = signatureLine.ToLowerInvariant();
            if (signatureLine.Contains("public "))
            {
                pos += signatureLine.IndexOf("public ") + "public ".Length;

                if (!signatureLine.Contains("final ") && !signatureLine.Contains("abstract "))
                    stringToUpdate = stringToUpdate.Insert(pos, "final ");
            }
            else if (signatureLine.Contains("protected "))
            {
                pos += signatureLine.IndexOf("protected ") + "protected ".Length;

                if (!signatureLine.Contains("final ") && !signatureLine.Contains("abstract "))
                    stringToUpdate = stringToUpdate.Insert(pos, "final ");
            }
            else
            {
                if (!signatureLine.Contains("final ") && !signatureLine.Contains("abstract "))
                    stringToUpdate = stringToUpdate.Insert(pos, "final ");

                if (!signatureLine.Contains("public "))
                    stringToUpdate = stringToUpdate.Insert(pos, "public ");
            }
            return stringToUpdate;
        }

        private string appendAttribute(string source, string signatureLine, string _attribute)
        {
            if (signatureLine.Contains("internal ") ||
                (_attribute.Contains("Hookable") && signatureLine.Contains("protected ")) ||
                signatureLine.Contains("private "))
            {
                return source;
            }
            if (source.Contains(_attribute))
            {
                return source;
            }

            int pos = _attribute.IndexOf("(");
            if (pos>0 &&
                source.Contains(_attribute.Substring(0, pos-1)))
            {
                return source;
            }

            int signatureLinePosInSource = source.IndexOf(signatureLine);
            string theline = source.Substring(signatureLinePosInSource);
            
            int attributeEndPosInSource = this.attributeEndPos(source, signatureLinePosInSource);

            if (attributeEndPosInSource > 0)
            {
                source = source.Insert(attributeEndPosInSource, ", " + _attribute);
            }
            else
            {
                theline = theline.Replace("\t", "    ");
                int spaces = theline.Length - theline.TrimStart().Length;
                source = source.Insert(signatureLinePosInSource, " ".PadLeft(spaces) + "[" + _attribute + "]" + Environment.NewLine);
            }
            return source;
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
  
     
    }
}
