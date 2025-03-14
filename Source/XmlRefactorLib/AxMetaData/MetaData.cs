using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace XmlRefactor
{
    static class MetaData
    {
        private static Dictionary<string, List<string>> files = new Dictionary<string, List<string>>();
        private static List<string> metadatafolders = null;
        private static Dictionary<string, string> FormForMenuItem = new Dictionary<string, string>();
        private static Dictionary<string, string> baseclasses = new Dictionary<string, string>();
        private static Dictionary<string, bool> methods = new Dictionary<string, bool>();

        private static string getEnumFromFile(string filename, string fieldName)
        {
            if (File.Exists(filename))
            {
                XmlReader SourceFile = new XmlReader(filename);
                string fileText = SourceFile.Text();

                string enumName = extractFromXML(fileText, "//AxTable/Fields/AxTableField[translate(Name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')=\"" + fieldName.ToLowerInvariant() + "\"]/EnumType");

                return enumName;
            }
            return String.Empty;
        }
        private static string getEnumFromDataEntityFile(string filename, string fieldName)
        {
            if (File.Exists(filename))
            {
                XmlReader SourceFile = new XmlReader(filename);
                string fileText = SourceFile.Text();

                string enumName = extractFromXML(fileText, "//AxDataEntityView/Fields/AxDataEntityViewField[translate(Name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')=\"" + fieldName.ToLowerInvariant() + "\"]/EnumType");

                if (enumName == String.Empty)
                {
                    string DSName = extractFromXML(fileText, "//AxDataEntityView/Fields/AxDataEntityViewField[translate(Name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')=\"" + fieldName.ToLowerInvariant() + "\"]/DataSource");
                    string DSFieldName = extractFromXML(fileText, "//AxDataEntityView/Fields/AxDataEntityViewField[translate(Name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')=\"" + fieldName.ToLowerInvariant() + "\"]/DataField");
                    string TableName = getTableNameFromQueryRangeSearch(fileText, "//AxDataEntityView/ViewMetadata/DataSources/AxQuerySimpleRootDataSource", "[Name=\"" + DSName + "\"]/Table");

                    enumName = getEnumFromTable(TableName, DSFieldName);
                }
                return enumName;
            }
            return String.Empty;
        }
        
        static public string extractFromXML(string xml, string xpath)
        {
            xml = RemoveAllNamespaces(xml);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
           
            XmlNode titleNode = doc.SelectSingleNode(xpath);
            if (titleNode != null)
                return titleNode.InnerText;

            return String.Empty;
        }
        static public XmlNodeList extractMultipleFromXML(string xml, string xpath)
        {
            xml = RemoveAllNamespaces(xml);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.SelectNodes(xpath);
        }

        private static List<string> metadataFolders()
        {
            if (metadatafolders == null)
            {
                metadatafolders = new List<string>();

                foreach (var folder in System.IO.Directory.GetDirectories(@"C:\AOSService\PackagesLocalDirectory\"))
                {
                    foreach (var subfolder in System.IO.Directory.GetDirectories(folder))
                    {
                        metadatafolders.Add(subfolder);
                    }
                }
            }
            return metadatafolders;
        }

        private static string getSymbolFromSystem(string enumName, string value)
        {/*
            switch (enumName)
            {
                case "NoYes":
                    switch (value)
                    {
                        case "0": return "No";
                        case "1": return "Yes";
                    }
                    break;
            }*/
            return String.Empty;

        }

        private static Boolean IsMenuItemPointingToForm(string filename, string formName)
        {
            if (FormForMenuItem.ContainsKey(filename))
            {
                return formName.ToLowerInvariant() == FormForMenuItem[filename].ToLowerInvariant();
            }

            if (File.Exists(filename))
            {
                XmlReader SourceFile = new XmlReader(filename);
                string fileText = SourceFile.Text();
                
                string objectName = MetaData.extractFromXML(fileText, "//AxMenuItemDisplay/Object");
                string objectType = MetaData.extractFromXML(fileText, "//AxMenuItemDisplay/ObjectType");
                if (objectType == string.Empty ||
                    objectType == "Form")
                {
                    FormForMenuItem.Add(filename, objectName);
                    if (objectName.ToLowerInvariant() == formName.ToLowerInvariant())
                    {                    
                        return true;
                    }
                }
            }
            return false;
        }


        private static string getSymbolFromFile(string filename, string value)
        {
            if (File.Exists(filename))
            {
                XmlReader SourceFile = new XmlReader(filename);
                string fileText = SourceFile.Text();
                string symbol = MetaData.extractFromXML(fileText, "//AxEnum/EnumValues/AxEnumValue[Value=\"" + value + "\"]/Name");

                if (symbol == String.Empty && value == "0")
                {
                    symbol = MetaData.extractFromXML(fileText, "//AxEnum/EnumValues/AxEnumValue[not (Value)]/Name");
                    // Debug.WriteLine(string.Format("{2}: {0} does not have value {1}", filename, value, Scanner.FILENAME));

                }

                return symbol;
            }
            return String.Empty;
        }

        public static string RemoveAllNamespaces(string xmlDocument)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));

            return xmlDocumentWithoutNs.ToString();
        }

        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                    xElement.Add(attribute);

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }


        public static string tableFormRef(string tablename)
        {
            foreach (var folder in metadataFolders())
            {
                var filename = folder + @"\axtable\" + tablename + ".xml";
                if (File.Exists(filename))
                {
                    XmlReader SourceFile = new XmlReader(filename);
                    return extractFromXML(SourceFile.Text(), "//AxTable/FormRef");
                }
            }

            return "";
        }

        public static Boolean isTablePersisted(string tablename)
        {
            foreach (var folder in metadataFolders())
            {
                var filename = folder + @"\axtable\" + tablename + ".xml";
                if (File.Exists(filename))
                {
                    XmlReader SourceFile = new XmlReader(filename);
                    string propValue = extractFromXML(SourceFile.Text(), "//AxTable/TableType");

                    return propValue == string.Empty;
                }
            }

            return true;
        }
        public static Boolean isFormUsingArgsMenuItemName(string formname)
        {
            foreach (var folder in metadataFolders())
            {
                var filename = folder + @"\axform\" + formname + ".xml";
                if (File.Exists(filename))
                {
                    XmlReader SourceFile = new XmlReader(filename);
                    if (SourceFile.Text().ToLowerInvariant().Contains(".menuitemname()"))
                    {
                        return true;
                    }
                    return false;
                }
            }

            return false;

        }

        private static Stack<string> traverseXML(XmlNode node, Stack<string> path, string xmlStopToken)
        {
            if (node.Name == xmlStopToken)
            {
                return path;
            }
            
            if (node.FirstChild != null)
            {        
                path.Push(node.Name);
                var stackFound = MetaData.traverseXML(node.FirstChild, path, xmlStopToken);
                if (stackFound != null)
                {
                    return stackFound;
                }
                path.Pop();
                
            }

            if (node.NextSibling != null)
            {
                var stackFound2 = MetaData.traverseXML(node.NextSibling, path, xmlStopToken);
                if (stackFound2 != null)
                {
                    return stackFound2;
                }
            }

            return null;
        }

        public static Stack<string> XMLPathToFirstToken(string xml, string xmlStopToken)
        {

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Stack<string> path = MetaData.traverseXML(doc.FirstChild, new Stack<string>(), xmlStopToken);
            return path;
        }


        public static string extractPreviousXMLElement(string name, int pos, string source)
        {
            int namePos1 = source.LastIndexOf("<" + name + ">", pos);
            if (namePos1 == -1)
            {
                return String.Empty;
            }

            namePos1 += ("<" + name + ">").Length;
            int namePos2 = source.IndexOf("</"+name+">", namePos1);
            return source.Substring(namePos1, namePos2 - namePos1);
        }
        public static string extractNextXMLElement(string name, int pos, string source)
        {
            int namePos1 = source.IndexOf("<" + name + ">", pos);
            if (namePos1 == -1)
            {
                return String.Empty;
            }

            namePos1 += ("<" + name + ">").Length;
            int namePos2 = source.IndexOf("</" + name + ">", namePos1);
            return source.Substring(namePos1, namePos2 - namePos1);
        }

        public static Boolean isCodeProtectedByMenuItem(string source, int pos)
        {
            if (source.Contains("<AxClass"))  
            {
                string methodName = extractPreviousXMLElement("Name", pos, source).ToLowerInvariant();

                if (methodName == "main")
                {
                    return true;
                }
            }
            else if (source.Contains("<AxForm"))  // Is form
            {

                string controlXml = extractPreviousXMLElement("Control", pos, source);

                //Method is directly on control
                if (controlXml != string.Empty)
                { 
                    string controlName = extractNextXMLElement("Name", 0, controlXml);

                    string menuitemNameOnControl = extractFromXML(source, ".//AxFormControl[Name=\"" + controlName + "\"]/MenuItemName");

                    if (menuitemNameOnControl != String.Empty)
                    {
                        return true;
                    }
                }
                else
                {
                    string datasourceXml = extractPreviousXMLElement("DataSource", pos, source);
                    //Method is in root of form
                    if (datasourceXml == string.Empty)
                    {
                        string methodName = extractPreviousXMLElement("Name", pos, source).ToLowerInvariant();

                        if (methodName == "run" ||   // These are the entry point on the form
                            methodName == "init")
                        {
                            return true;
                        }
                        
                        string lookFor = ("element." + methodName + "(");
                        int posCalledFrom = source.ToLowerInvariant().IndexOf(lookFor);
                        if (posCalledFrom != -1)
                        {
                            return isCodeProtectedByMenuItem(source, posCalledFrom);
                        }
                    }
                }
            }
            return false;
        }

        public static Boolean isFormUsingArgsMenuItemNameAndMenuItem(string formname, string menuitemname)
        {
            foreach (var folder in metadataFolders())
            {
                var filename = folder + @"\axform\" + formname + ".xml";
                if (File.Exists(filename))
                {
                    XmlReader SourceFile = new XmlReader(filename);
                    string text = SourceFile.Text().ToLowerInvariant();
                    if (text.Contains(".menuitemname()") &&
                        text.Contains("menuitemdisplaystr("+menuitemname+")"))
                    {
                        return true;
                    }
                    return false;
                }
            }

            return false;

        }

        public static Boolean isFormLookup(string formname)
        {
            foreach (var folder in metadataFolders())
            {
                var filename = folder + @"\axform\" + formname + ".xml";
                if (File.Exists(filename))
                {
                    XmlReader SourceFile = new XmlReader(filename);
                    string propValue = extractFromXML(SourceFile.Text(), "//AxForm/Design/Style");

                    if (propValue == "Lookup")
                    {
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }

        public static Boolean isFormDialog(string formname)
        {
            foreach (var folder in metadataFolders())
            {
                var filename = folder + @"\axform\" + formname + ".xml";
                if (File.Exists(filename))
                {
                    XmlReader SourceFile = new XmlReader(filename);
                    string propValue = extractFromXML(SourceFile.Text(), "//AxForm/Design/Pattern");

                    if (propValue == "Dialog")
                    {
                        return true;
                    }
                    return false;
                }
            }

            return false;
        }


        private static List<string> getFormRootDataSourcesFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                List<string> res = new List<string>();
                XmlReader SourceFile = new XmlReader(filename);                
                XmlNodeList nodes = extractMultipleFromXML(SourceFile.Text(), "//AxForm/DataSources/AxFormDataSource[not (JoinSource)]/Table");

                foreach (XmlNode node in nodes)
                {
                    res.Add(node.InnerText);
                }

                return res;
            }
            return null;
        }

        public static List<string> getFormRootDataSources(string formName)
        {
            foreach (var folder in metadataFolders())
            {
                var res = getFormRootDataSourcesFromFile(folder + @"\axform\" + formName + ".xml");

                if (res != null)
                {
                    return res;
                }
            }

            return new List<string>();
        }

        public static string getEnumFromTable(string tableName, string fieldName)
        {
            foreach (var folder in metadataFolders())
            {
                string enumName = getEnumFromFile(folder + @"\axtable\" + tableName + ".xml", fieldName);
                if (enumName != String.Empty)
                {
                    return enumName;
                }
                enumName = getEnumFromDataEntityFile(folder + @"\axDataEntityView\" + tableName + ".xml", fieldName);
                if (enumName != String.Empty)
                {
                    return enumName;
                }
            }

            return String.Empty;
        }

        private static List<string> getFilesInFolderType(string folder)
        {
            if (!files.ContainsKey(folder))
            {
                var filesInFolder = new List<string>();
                foreach (var metadataFolder in metadataFolders())
                {
                    try
                    {
                        foreach (string file in System.IO.Directory.GetFiles(metadataFolder + @"\" + folder, "*.xml"))
                        {
                            filesInFolder.Add(file.ToLowerInvariant());
                        }
                    }
                    catch
                    { //Folder didnt exist - just ignore 
                    }
                }
                files.Add(folder, filesInFolder);
            }
            return files[folder];
        }

        public static List<string> getMenuItemsFromFormName(string formName)
        {
            var menuitems = new List<string>();

            foreach (var file in getFilesInFolderType("axmenuitemdisplay"))
            {
                if (IsMenuItemPointingToForm(file, formName))
                {
                    menuitems.Add(System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }
            return menuitems;
        }

        public static string getSymbolFromEnum(string enumName, string value)
        {
            string symbolFromSystem = getSymbolFromSystem(enumName, value);
            if (symbolFromSystem != String.Empty)
            {
                return symbolFromSystem;
            }

            foreach (var folder in metadataFolders())
            {
                string symbol = getSymbolFromFile(folder + @"\axenum\" + enumName + ".xml", value);
                if (symbol != String.Empty)
                {
                    return symbol;
                }
            }

            return String.Empty;
        }

        public static bool isMethodOverridding(string className, string methodName)
        {
            className = className.ToLowerInvariant();
            methodName = methodName.ToLowerInvariant();
            string baseClass = MetaData.baseclass(className);

            while (baseClass != String.Empty)
            {
                if (MetaData.hasMethod(baseClass, methodName))
                {
                    return true;
                }
                baseClass = MetaData.baseclass(baseClass);
            }
            return false;
        }

        private static string baseclass(string className)
        {
            if (baseclasses.ContainsKey(className))
                return baseclasses[className];

            string result = String.Empty;

            foreach (var file in getFilesInFolderType("axclass").Where(s => s.Contains(@"\axclass\"+className+".xml")))
            {
                if (File.Exists(file))
                {
                    XmlReader SourceFile = new XmlReader(file);
                    string fileText = SourceFile.Text().ToLowerInvariant();
                    int pos = fileText.IndexOf(" extends ");
                    if (pos > 0)
                    {
                        pos = pos + " extends ".Length;
                        int pos2 = fileText.IndexOf(Environment.NewLine, pos);
                        int pos3 = Math.Min(pos2, fileText.IndexOf(" ", pos));
                        result = fileText.Substring(pos, pos3-pos);
                        break;                        
                    }
                }
            }
            baseclasses[className] = result;
            return result;
        }

        public static bool hasMethod(string className, string methodname)
        {
            bool result = false;
            string key = className + "." + methodname;

            if (methods.ContainsKey(key))
                return methods[key];

            foreach (var file in getFilesInFolderType("axclass").Where(s => s.ToLowerInvariant().Contains(@"\axclass\" + className.ToLowerInvariant() + ".xml")))
            {
                if (File.Exists(file))
                {
                    XmlReader SourceFile = new XmlReader(file);
                    string fileText = SourceFile.Text().ToLowerInvariant();
                    result = fileText.Contains("<name>"+methodname.ToLowerInvariant()+"</name>");
                    break;
                }
            }
            methods[key] = result;            
            return result;
        }

        private static string getTableNameFromQueryRangeSearch(string xml, string prefix, string postfix)
        {
            string sub = "/DataSources/AxQuerySimpleEmbeddedDataSource";

            for (int i = 0; i <= 10; i++)
            {
                string xPath = prefix;
                for (int j = 0; j < i; j++)
                {
                    xPath += sub;
                }
                xPath += postfix;

                string tableName = MetaData.extractFromXML(xml, xPath);
                if (tableName != String.Empty)
                {
                    return tableName;
                }
            }
            return String.Empty;
        }

        public static string getTableNameFromQueryRange(string xml, string rangeName)
        {
            string tableName = getTableNameFromQueryRangeSearch(xml, "//AxView/ViewMetadata/DataSources/AxQuerySimpleRootDataSource", "[Ranges/AxQuerySimpleDataSourceRange/Name=\"" + rangeName + "\"]/Table");

            if (tableName == String.Empty)
                tableName = getTableNameFromQueryRangeSearch(xml, "//AxQuery/DataSources/AxQuerySimpleRootDataSource", "[Ranges/AxQuerySimpleDataSourceRange/Name=\"" + rangeName + "\"]/Table");

            if (tableName == String.Empty)
                tableName = getTableNameFromQueryRangeSearch(xml, "//AxDataEntityView/ViewMetadata/DataSources/AxQuerySimpleRootDataSource", "[Ranges/AxQuerySimpleDataSourceRange/Name=\"" + rangeName + "\"]/Table");

            return tableName;
        }
        static public string AOTPath(string methodName)
        {
            int pos = Scanner.FILENAME.LastIndexOf("\\");
            int pos2 = Scanner.FILENAME.LastIndexOf("\\", pos - 1);
            string AOTPath = Scanner.FILENAME.Substring(pos2);
/*              .Replace("AxClass", "Classes")
                .Replace("AxView", "Views")
                .Replace("AxTable", "Tables")
                .Replace("AxForm", "Forms");
*/
            AOTPath = AOTPath.Substring(0, AOTPath.Length - 4);

            if (methodName !=String.Empty)
            {
                AOTPath += "\\Methods\\" + methodName;

            }
            return AOTPath;
        }

        static HashSet<string> externallyReferencedPaths;
        static HashSet<string> externallyReferencedTypes;

        static void ReadExternalReferences()
        {
            if (externallyReferencedPaths == null)
            {
                externallyReferencedPaths = new HashSet<string>();
                externallyReferencedTypes = new HashSet<string>();

                var stringArray = File.ReadAllLines(@"../../../XmlRefactorLib/RulesInput/MethodsReferencedExternally.txt");
                foreach (var item in stringArray)
                {
                    externallyReferencedPaths.Add(item.ToLowerInvariant());
                    int pos = item.IndexOf("\\", 1);
                    pos = item.IndexOf("\\", pos+1);
                    string type = item.Substring(0, pos);

                    externallyReferencedTypes.Add(type.ToLowerInvariant());
                }
            }
        }

        static public bool isReferencedExternally(string methodName)
        {
            MetaData.ReadExternalReferences();
            string AOTPath = MetaData.AOTPath(methodName);
            if (methodName != String.Empty)
            {
                if (externallyReferencedPaths.Contains(AOTPath.ToLowerInvariant()))
                    return true;
            }
            else
            {
                if (externallyReferencedTypes.Contains(AOTPath.ToLowerInvariant()))
                    return true;
            }
            return false;
        }

    }
}
