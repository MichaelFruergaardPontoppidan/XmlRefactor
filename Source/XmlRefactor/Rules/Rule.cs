using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using XmlRefactor.Properties;

namespace XmlRefactor
{
    public abstract class Rule
    {
        private XmlMatch priv_xpoMatch;
        private string now = ( (int) System.DateTime.Now.TimeOfDay.TotalSeconds).ToString();
        int hits = 0;
        internal Settings Settings { get; set; }

        public int Hits { get; set; }
        
        protected string logFileName()
        {
            string s = @"c:\temp\log_" + this.RuleName() + "_" + now + ".txt";
            return s;
        }
        protected XmlMatch xpoMatch
        {
            get
            {
                if (priv_xpoMatch == null)
                {
                    priv_xpoMatch = new XmlMatch();
                    this.buildXpoMatch();
                }
                return priv_xpoMatch;
            }
        }
        
        protected virtual void buildXpoMatch()
        {
        }
        
        virtual public bool skip(string input)
        {
            string mustContain = this.mustContain().ToLower();
            if (mustContain != String.Empty)
            {
                return !input.ToLower().Contains(mustContain);
            }
            return false;
        }

        virtual public void Init()
        {
        }

        virtual public string mustContain()
        {
            return String.Empty;
        }

        abstract public string Run(string input);
        override sealed public string ToString()
        {
            return this.Grouping() + "." + this.RuleName();
        }
        abstract public string RuleName();
        virtual public string Grouping()
        {
            return "";
        }
        virtual public bool Enabled()
        {
            return false;
        }
        
        protected string breakString(string input, string s1, string s2)
        {
            int pos;
            int len = input.Length;
            
            pos = input.IndexOf(s1+" "+s2);
            if (pos != -1)
            {
                int startLine = input.LastIndexOf("\n",pos,pos);
                input = input.Remove(pos + s1.Length, 1);
                input = input.Insert(pos+s1.Length, "\r\n"+new String('\t',pos-startLine));
                input = breakString(input, s1, s2);
            }
            return input;
        }

        public string formatXML(string input)
        {
            string output = this.breakString(input, "<FormControlExtension", "i:nil");
            
            output = this.breakString(output, "<AxFormControl xmlns=\"\"", "i:type");

            
            return output;
        }

    }
}
