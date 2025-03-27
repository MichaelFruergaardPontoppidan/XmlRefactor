using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dotenv.net;
using XmlRefactor.Properties;


namespace XmlRefactor
{
    public class GlobalLib
    {
        public Settings settings = XmlRefactor.Properties.Settings.Default;
        
        public GlobalLib()
        {
            DotEnv.Load();
        }

    }
}
