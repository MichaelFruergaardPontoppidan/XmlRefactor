using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using dotenv.net;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace XmlRefactor
{
    static class Program
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Properties.Settings _settings = XmlRefactor.Properties.Settings.Default;
            
            DotEnv.Load();


            if (args == null || args.Length == 0)
            {
                Application.Run( new Form1());
            }
            else
            {
             //   throw new Exception("ccc");
                if (args[0].Contains("?"))
                {
                    AttachConsole(ATTACH_PARENT_PROCESS);
                    Console.WriteLine();
                    Console.WriteLine("XmlRefactor - a tool to automate refactoring of X++ XML files");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("XmlRefactor <path> [Rule] [RuleParameter]");
                    Console.WriteLine();
                    Console.WriteLine("Example:");
                    Console.WriteLine(@"XmlRefactor <path> e:\git\appsuite RuleRemoveFlightReferences MyFlight");
                    FreeConsole(); // Detach the console
                    Environment.Exit(0);                    
                }
                else
                {
                    _settings.DirectoryPath = args[0];

                    if (args.Length > 0)
                    {
                        _settings.RuleToRun = args[1];
                    }

                    if (args.Length > 1)
                    {
                        _settings.RuleParameter = args[2];
                    }
                    Console.WriteLine("XmlRefactor running silent mode");

                    _settings.Save();
                    Form1 f = new Form1();
                    f.Silent = true;
                    f.Show();
                    f.start();

                    Application.Run(f);
                }
            }
        }
    }
}
