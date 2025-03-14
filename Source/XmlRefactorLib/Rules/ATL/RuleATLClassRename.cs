using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleATLClassRename : Rule
    {
        HashSet<string> classMethodHashSet = new HashSet<string>();
        public RuleATLClassRename()
        {
        }

        public override string RuleName()
        {
            return "Rename of references to base classes";
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
            xpoMatch.AddLiteral(@"\!\[CDATA\[");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddLiteral(@"\]\>\<\/");

        }

        public override string Run(string _input)
        {
            return this.Run(_input, 0);
        }

        private string renameClassRef(string _input, string _oldname, string _newname)
        {
            string res = _input;
/*
            if (Scanner.FILENAME.Contains("AtlScenarioProductVariantUnitConversionEnabled"))
                res = res;
            */
            Regex reg = new Regex(@"[\W\s]"+_oldname+@"[\W\s]", RegexOptions.IgnoreCase);
            Match match = reg.Match(_input);

            while (match.Success)
            {
                res = res.Remove(match.Index + 1, _oldname.Length).Insert(match.Index + 1, _newname);
                match = reg.Match(res, match.Index);

                Hits++;
            }

            return res;
        }

        private string renameStaticDataSetupObject(string _input, string _regExPattern)
        {
            string res = _input;

            Regex reg = new Regex(_regExPattern, RegexOptions.IgnoreCase);
            Match match = reg.Match(_input);

            if (match.Success)
            {
                string className = match.Groups[1].Value;
                string newClassName = "new " + className + "(";

                res = res.Remove(match.Index, match.Length);
                res = res.Insert(match.Index, newClassName);

                if (res.Contains("as "+ className))
                {
                    res = res.Remove(match.Index + newClassName.Length + 1, 4 + className.Length);
                }

                res = res.Replace("public static", "public final");
                res = res.Replace("AtlDataRootNode _dataRoot", "");
                res = res.Replace("AtlDataCommon _dataRoot", "");
            }

            return res;
        }

        private string renameDataSetupObject(string _input)
        {
            string res = _input;

            res = renameStaticDataSetupObject(res, @"_dataRoot\.dataSetupObject\(classNum\(([\w]+?)\)");
            res = renameStaticDataSetupObject(res, @"_data\.dataSetupObject\(classNum\(([\w]+?)\)");
            res = renameStaticDataSetupObject(res, @"data\.dataSetupObject\(classNum\(([\w]+?)\)");

            res = res.Replace("data.", "AtlDataRootNode::construct().");
            res = res.Replace("super(_data)", "super()");

            return res;
        }


        public string Run(string _input, int _startAt = 0)
        {
            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success)
            {
                string xml = _input;
                var stringToUpdate = match.Value;
                var updatedString = stringToUpdate;

                updatedString = this.renameClassRef(updatedString, "AtlAbstractCommand", "AtlCommand");
                updatedString = this.renameClassRef(updatedString, "AtlDataBase", "AtlDataNode");
                updatedString = this.renameClassRef(updatedString, "AtlDataRoot", "AtlDataRootNode");
                updatedString = this.renameClassRef(updatedString, "AtlEntityBase", "AtlEntity");
                updatedString = this.renameClassRef(updatedString, "AtlISpecificationCommon", "AtlISpecification");
                updatedString = this.renameClassRef(updatedString, "AtlQueryBase", "AtlQuery");
                updatedString = this.renameClassRef(updatedString, "AtlQueryEnumerator", "AtlQueryRunEnumerator");
                updatedString = this.renameClassRef(updatedString, "AtlSpecificationBase", "AtlSpecification");
                updatedString = this.renameClassRef(updatedString, "AtlSpecificationCollection", "AtlSpecifications");
                updatedString = this.renameClassRef(updatedString, "AtlSubSpecificationCollection", "AtlSubSpecifications");
                updatedString = this.renameClassRef(updatedString, "AtlFieldDependencyAttribute", "AtlDependentField");
                updatedString = this.renameClassRef(updatedString, "AtlFluentSetterAttribute", "AtlDependentFluentSetter");
                updatedString = this.renameClassRef(updatedString, "AtlMethodDependencyAttribute", "AtlDependentMethod");
                updatedString = this.renameClassRef(updatedString, "AtlRelationDependencyAttribute", "AtlDependentRelation");
                updatedString = this.renameClassRef(updatedString, "AtlGenerateEntityTableAttribute", "AtlEntityGenerationDependentTable");
                updatedString = this.renameClassRef(updatedString, "AtlGenerateQueryTableAttribute", "AtlQueryGenerationDependentTable");
                updatedString = this.renameClassRef(updatedString, "AtlGenerateSpecTableAttribute", "AtlSpecGeneratationDependentTable");
                updatedString = this.renameClassRef(updatedString, "AtlFieldDependency", "AtlDependentField");
                updatedString = this.renameClassRef(updatedString, "AtlFluentSetter", "AtlDependentFluentSetter");
                updatedString = this.renameClassRef(updatedString, "AtlMethodDependency", "AtlDependentMethod");
                updatedString = this.renameClassRef(updatedString, "AtlRelationDependency", "AtlDependentRelation");
                updatedString = this.renameClassRef(updatedString, "AtlGenerateEntityTable", "AtlEntityGenerationDependentTable");
                updatedString = this.renameClassRef(updatedString, "AtlGenerateQueryTable", "AtlQueryGenerationDependentTable");
                updatedString = this.renameClassRef(updatedString, "AtlGenerateSpecTable", "AtlSpecGeneratationDependentTable");

                if (Scanner.FILENAME.ToLowerInvariant().Contains("atldata"))
                {
                    updatedString = renameDataSetupObject(updatedString);
                }

                updatedString = updatedString.Replace("new AtlDataRootNode()", "AtlDataRootNode::construct()");
                updatedString = updatedString.Replace("new AtlSpecifications()", "AtlSpecifications::construct()");
                updatedString = updatedString.Replace("AtlDataRootNode        ", "AtlDataRootNode    ");

                if (updatedString != stringToUpdate)
                {
                    _input = _input.Replace(stringToUpdate, updatedString);
                }
                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }
            

     
    }
}
