using System.Collections.Generic;
using System;
using System.Reflection;
using System.Diagnostics;
using XmlRefactor;
using System.IO;

class Program
{

    static int Main(string[] args)
    {
        if (args == null || args.Length != 4 || args[0].Contains("?"))
        {
            Console.WriteLine("XmlRefactorCacheBuilder - a tool to create a cache of XML files");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("XmlRefactorCacheBuilder <path> <cachefile> <Rule> <RuleParameterFile>");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine(@"XmlRefactorCacheBuilder e:\git\appsuite e:\tmp\XmlCache.json RuleRemoveFlightReferences MyFlights.txt");
            return 0;
        }

        try
        {
            GlobalLib lib = new GlobalLib();

            lib.settings.DirectoryPath = args[0];
            string cacheFile = args[1];
            lib.settings.RuleToRun = args[2];
            ScannerCache scannerCache = new ScannerCache();

            string ruleInput = @"../../../XmlRefactorCacheBuilder/" + args[3];

            if (!File.Exists(ruleInput))
            {
                Console.WriteLine($"Rule input {ruleInput} could not be found!");
                return 2;
            }
        
            var inputs = ReadInputFile(ruleInput);
            List<Rule> rules = new List<Rule>();
            foreach (string s in inputs)
            {
                Rule rule = Rule.createRuleFromClassName(lib.settings.RuleToRun);
                if (rule == null)
                {
                    Console.WriteLine($"Rule {lib.settings.RuleToRun} could not be created");
                    return 1;
                }
                rule.Settings = lib.settings;
                rule.Init(s);
                rules.Add(rule);
            }

            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();
            Scanner scanner = new Scanner(scannerCache);
            scanner.cacheBuildMode = true;
            scanner.Run(lib.settings.DirectoryPath, true, rules, UpdateResults, UpdateProgress, SignalEnd);
            scannerCache.ToFile(cacheFile);
            timer.Stop();
            Console.Write("\r" + new string(' ', Console.BufferWidth - 1));

            Console.WriteLine($"\rCompleted in {timer.Elapsed.ToString()}. Scanned {scannedFiles} files.");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine();
            Console.WriteLine($"\rFile being processed: " + Scanner.FILENAME);
            Console.WriteLine($"\rException: " + e.ToString());
            return 10000;
        }
        finally
        {
            Console.ResetColor();
        }
    }
    static List<string> ReadInputFile(string filePath)
    {
        List<string> lines = new List<string>();

        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string s = line.Trim();
                if (line != string.Empty)
                {
                    lines.Add(s);
                }
            }
        }

        return lines;
    }

    static int scannedFiles = 0;
    static void UpdateResults(ResultItem item)
    {
      
    }

    static void UpdateProgress(string filename)
    {
        scannedFiles++;
        if (scannedFiles % 1000 == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"\rScanned {scannedFiles} files            ");
        }
    }
    static void SignalEnd()
    {
    }

}