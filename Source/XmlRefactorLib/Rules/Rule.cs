﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using XmlRefactor.Properties;

namespace XmlRefactor
{
    public abstract class Rule
    {
        private XmlMatch priv_xpoMatch;
        private string now = ( (int) System.DateTime.Now.TimeOfDay.TotalSeconds).ToString();
        public string InputParameter = string.Empty;
        
        public Settings Settings { get; set; }

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

        public virtual void Init(string parameter)
        {
            InputParameter = parameter;
        }

        protected virtual void buildXpoMatch()
        {
        }

        private string mustContainLower = string.Empty;
        private bool skipInited = false;
        
        virtual public bool skip(string input)
        {
            if (!skipInited)
            {
                mustContainLower = this.mustContain().ToLower();
                skipInited = true;
            }
            
            if (mustContainLower != String.Empty)
            {
                return !input.Contains(mustContainLower);
            }
            return false;
        }            

        virtual public string mustContain()
        {
            return String.Empty;
        }

        abstract public string Run(string input);

        virtual public string PostRun(string input)
        {
            return input;
        }

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
        virtual public bool IsXppRule()
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

        public static List<Rule> AllRules(Settings settings)
        {
            var rules = new List<Rule>();

            Assembly assembly = Assembly.GetExecutingAssembly();

            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass &&
                    !type.IsAbstract &&
                    isTypeRule(type))
                {
                    Rule rule = (Rule)assembly.CreateInstance(type.FullName);
                    rule.Settings = settings;
                    rules.Add(rule);
                }
            }
            return rules;
        }

        private static bool isTypeRule(Type type)
        {
            if (type.Name == "Rule")
                return true;

            if (type.BaseType != null)
                return isTypeRule(type.BaseType);

            return false;
        }

        public static Rule createRuleFromClassName(string name)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            String fullName = "XmlRefactor." + name;
            return assembly.CreateInstance(fullName) as Rule;
        }
    }
}
