using System.Collections.Generic;
using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using XmlRefactor;

class Program
{
    static int Main(string[] args)
    {
        if (args == null || args.Length < 2 || args[0].Contains("?") || args.Length > 4)
        {
            Console.WriteLine("XmlRefactorConsole - a tool to automate refactoring of X++ XML files");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("XmlRefactorConsole <path> <Rule> [RuleParameter] [CacheFile]");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine(@"XmlRefactorConsole e:\git\appsuite RuleRemoveFlightReferences MyFlight myCache.json");
            return 0;
        }

        try
        {
            GlobalLib lib = new GlobalLib();

            lib.settings.DirectoryPath = args[0];
            lib.settings.RuleToRun = args[1];
            ScannerCache scannerCache = null;

            if (args.Length > 2)
            {
                lib.settings.RuleParameter = args[2];
            }
            if (args.Length > 3)
            {
                string cacheFile = args[3];

                if (File.Exists(cacheFile))
                {
                    scannerCache = ScannerCache.DeserializeDictionaryFromFile(cacheFile);
                }
                else
                {
                    Console.WriteLine($"Cache file {cacheFile} could not be found!");
                    return 2;
                }
            }

            List<Rule> rules = new List<Rule>();
            Rule rule = Rule.createRuleFromClassName(lib.settings.RuleToRun);
            if (rule == null)
            {
                Console.WriteLine($"Rule {lib.settings.RuleToRun} could not be created");
                return 1;
            }
            rule.Settings = lib.settings;
            rule.InputParameter = lib.settings.RuleParameter;
            rules.Add(rule);

            Stopwatch timer = new Stopwatch();
            timer.Start();
            Scanner scanner = new Scanner(scannerCache);        
            
            scanner.Run(lib.settings.DirectoryPath, true, rules, UpdateResults, UpdateProgress, SignalEnd);
            timer.Stop();
            Console.Write("\r" + new string(' ', Console.BufferWidth - 1));
            
            Console.WriteLine($"\rCompleted in {timer.Elapsed.ToString()}. Scanned {scannedFiles} files and found {hits} in {files} files.");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine();
            Console.WriteLine($"\rFile being processed: "+Scanner.FILENAME);
            Console.WriteLine($"\rException: "+e.ToString());
            return 10000;
        }
        finally
        {
            Console.ResetColor();
        }
    }

    static int hits = 0, files = 0, scannedFiles = 0;
    
    static void UpdateResults(ResultItem item)
    {        
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\rUpdated {item.filename}");
        files++;
        hits += item.hits;
    }

    static void UpdateProgress(string filename)
    {
        scannedFiles++;
        if (scannedFiles % 10000 == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"\rScanned {scannedFiles} files            ");
        }
    }
    static void SignalEnd()
    {
    }

}