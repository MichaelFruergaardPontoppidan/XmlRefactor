// See https://aka.ms/new-console-template for more information

class Program
{
    static void Main(string[] args)
    {
        if (args[0].Contains("?"))
        {
            Console.WriteLine("XmlRefactorConsole - a tool to automate refactoring of X++ XML files");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("XmlRefactorConsole <path> [Rule] [RuleParameter]");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine(@"XmlRefactorConsole <path> e:\git\appsuite RuleRemoveFlightReferences MyFlight");
        }
    }
}

