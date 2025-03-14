using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using dotenv.net;

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
