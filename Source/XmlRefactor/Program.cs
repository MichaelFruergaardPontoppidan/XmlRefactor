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
                _settings.DirectoryPath = args[0];

                if (args.Length > 0)
                {
                    _settings.RuleToRun = args[1];
                }

                if (args.Length > 1)
                {
                    _settings.RuleParameter = args[2];
                }

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
