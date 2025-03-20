using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace XmlRefactor
{
    public class Scanner 
    {
        private ResultDelegate resultCallback;
        private ProgressDelegate progressCallback;
        private SignalEndDelegate signalEndCallback;
        private bool commit = false;
        private List<Rule> rules;

        public Scanner()
        {
        }

        void scanFolder(string path)
        {
            string[] files = null;
            
            if (Directory.Exists(path))
            {
                files = System.IO.Directory.GetFiles(path, "*.xml");
                
                foreach (string file in files)
                {
                    scanFile(file);
                }

                string[] folders = System.IO.Directory.GetDirectories(path);
                foreach (string folder in folders)
                {
                    string folderLower = folder.ToLowerInvariant();
                    if (!folderLower.Contains("xppmetadata"))
                    {
                        if (!onlyScanXppFolders ||
                            !folderLower.Contains("ax") ||
                            folderLower.Contains("axclass") ||
                            folderLower.Contains("axtable") ||
                            folderLower.Contains("axform") ||
                            folderLower.Contains("axquery") ||
                            folderLower.Contains("axmacro") ||
                            folderLower.Contains("axview") ||
                            folderLower.Contains("axdataentity") ||
                            folderLower.Contains("axmap") 
                            )
                        {
                            scanFolder(folder);
                        }
                    }
                }
            }
        }
        public static string FILENAME;

        private bool onlyScanXppFolders = false;
        private bool allRulesAreXppRules()
        {
            foreach (Rule rule in rules)
            {
                if (!rule.IsXppRule())
                    return false;
            }
            return true;
        }

        void scanFile(string filename)
        {
            progressCallback(filename);
            
            if (File.Exists(filename))
            {
                XmlReader SourceFile = new XmlReader(filename);
                FILENAME = filename;
                string fileText = SourceFile.Text();
                string processedText = fileText;
                string skipText = processedText.ToLower();
                int hits = 0;
                foreach (Rule rule in rules)
                {
                    if (rule.skip(skipText))
                        continue;
                    rule.Hits = 0;
                    processedText = /*rule.formatXML*/(rule.Run(processedText));
                    processedText = /*rule.formatXML*/(rule.PostRun(processedText));
                    hits += rule.Hits;
                }
                
                if (fileText != processedText)
                {
                    ResultItem item = new ResultItem();
                    item.filename = filename;
                    item.before = fileText;
                    item.after = processedText;
                    item.hits = hits;   
                    resultCallback(item);

                    if (commit)
                    {
                        System.Text.Encoding outEncoding;
                        outEncoding = SourceFile.fileEncoding;

                        SourceFile = null;
                        File.SetAttributes(filename, FileAttributes.Archive);
                        FileStream destinationStream = new FileStream(filename, FileMode.Create);
                        using (StreamWriter destinationFile = new StreamWriter(destinationStream, outEncoding))
                        {
                            destinationFile.Write(processedText);
                        }
                    }
                }                
            }
        }

        public void Run(
            string path, 
            bool commitValue,
            List<Rule> rulesValue,
            ResultDelegate resultDelegate, 
            ProgressDelegate progressDelegate, 
            SignalEndDelegate signalEndDelegate)
        {
            commit = commitValue;
            rules = rulesValue;
            resultCallback = resultDelegate;
            progressCallback = progressDelegate;
            signalEndCallback = signalEndDelegate;
            onlyScanXppFolders = this.allRulesAreXppRules();
            this.scanFolder(path);
            signalEndCallback();
        }
    }
}
