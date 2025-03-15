using System.Collections.Generic;
using System;
using System.Reflection;
using System.Diagnostics;
using XmlRefactor;

class Program
{
    static void Main(string[] args)
    {
        if (args == null || args.Length < 2 || args[0].Contains("?") || args.Length > 3)
        {
            Console.WriteLine("XmlRefactorConsole - a tool to automate refactoring of X++ XML files");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("XmlRefactorConsole <path> <Rule> [RuleParameter]");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine(@"XmlRefactorConsole <path> e:\git\appsuite RuleRemoveFlightReferences MyFlight");
            return;
        }

//        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        GlobalLib lib = new GlobalLib();

        lib.settings.DirectoryPath = args[0];
        lib.settings.RuleToRun = args[1];

        if (args.Length > 2)
        {
            lib.settings.RuleParameter = args[2];
        }

        List<Rule> rules = new List<Rule>();
        Rule rule = Rule.createRuleFromClassName(lib.settings.RuleToRun);
        if (rule == null)
        {
            Console.WriteLine($"Rule {lib.settings.RuleToRun} could not be created");
            return;
        }
        rule.Settings = lib.settings;
        rules.Add(rule);

        Stopwatch timer = new Stopwatch();
        timer.Start();
        Scanner scanner = new Scanner();
        scanner.Run(lib.settings.DirectoryPath, true, rules, UpdateResults, UpdateProgress, SignalEnd);
        timer.Stop();
        Console.WriteLine($"Completed in {timer.Elapsed.ToString()}. Scanned {scannedFiles} files and found {hits} in {files} files." );
    }

    static int hits = 0, files = 0, scannedFiles = 0;
    static void UpdateResults(ResultItem item)
    {
        Console.WriteLine($"Updated {item.filename}");
        files++;
        hits += item.hits;
    }

    static void UpdateProgress(string filename)
    {
        scannedFiles++;
     //   Console.WriteLine($"Scanning {filename}");
    }
    static void SignalEnd()
    {
    }

}