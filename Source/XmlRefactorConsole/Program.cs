using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;
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

        Scanner scanner = new Scanner();
        scanner.Run(lib.settings.DirectoryPath, true, rules, UpdateResults, UpdateProgress, SignalEnd);
    }
    static void UpdateResults(ResultItem item)
    {
        Console.WriteLine($"Updated file {item.filename}");

    }
    static void UpdateProgress(string filename)
    {
        Console.WriteLine($"Scanning {filename}");

    }
    static void SignalEnd()
    {
        Console.WriteLine("Completed");
    }

}