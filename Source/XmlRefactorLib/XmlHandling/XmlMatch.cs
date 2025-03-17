using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    public class XmlMatch
    {
        private StringBuilder builder;
        private Regex regex;

        public Match Match(string input, int startAt = 0)
        {
            if (regex == null)
            {
                regex = new Regex(this.Expression, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            return regex.Match(input, startAt);
        }

        private string Expression
        {
            get { return builder.ToString(); }
        }
        public XmlMatch()
        {
            builder = new StringBuilder(100);            
        }
        public void AddWhiteSpaceRequired()
        {
            //include this line when running commit
            builder.Append(@"[\s]+");
        }
        public void AddDelimter()
        {
            builder.Append(@"[\,\(\[\s]+");
        }

        public void AddWhiteSpace()
        {
            //include this line when running commit
            builder.Append(@"[\s]*");  
        }
        public void AddSymbol(string symbol, int count = 1)
        {
            for (int i = 1 ; i<=count; i++)
                builder.Append(@"[\"+symbol+"]");
        }
        public void AddLiteral(string literal)
        {
            builder.Append(literal);
        }
        public void AddOneOfLiterals(string literal1, string literal2)
        {
            builder.Append($"{literal1}|{literal2}");
        }
        public void AddCapture()
        {
            this.AddWhiteSpace();
            builder.Append(@"([\S]+?)");
        }
        public void AddCaptureWord()
        {
            builder.Append(@"([\w]+?)");
        }

        public void AddCaptureAnything()
        {
            this.AddWhiteSpace();
            builder.Append(@"([\S\s]+?)");
        }
        public void AddCaptureOptional()
        {
            this.AddWhiteSpace();
            builder.Append(@"([\S]?)");            
        }
        public void AddCaptureOptional(string literal)
        {
            this.AddWhiteSpace();
            builder.Append("("+literal+")?");            
        }
        public void AddStartBracket()
        {
            this.AddWhiteSpace();
            builder.Append(@"[\[]");
        }
        public void AddEndBracket()
        {
            this.AddWhiteSpace();
            builder.Append(@"[\]]");
        }

        public void AddStartParenthesis()
        {
            this.AddWhiteSpace();
            builder.Append(@"[(]");            
        }
        public void AddEndParenthesis()
        {
            this.AddWhiteSpace();
            builder.Append("[)]");            
        }
        public void AddComma()
        {
            this.AddWhiteSpace();
            builder.Append("[,]");
        }
        public void AddCommaOptional()
        {
            this.AddWhiteSpace();
            builder.Append("[,]?");
        }
        public void AddNewLine()
        {
            this.AddWhiteSpace();
            builder.Append("[#]");
            this.AddWhiteSpace();
        }
        public void AddSemicolon()
        {
            this.AddWhiteSpace();
            builder.Append("[;]");
        }
        public void AddORSymbol()
        {
            this.AddWhiteSpace();
            builder.Append("[|][|]");
        }
        public void AddDoubleColon()
        {
            this.AddWhiteSpace();
            builder.Append("[:][:]");
        }
        public void AddEqual()
        {
            builder.Append("[=]");
        }
        public void AddDot()
        {
            this.AddWhiteSpace();
            builder.Append("[.]");            
        }
        public void AddXMLStart(string xml, Boolean allowAttributes = true)
        {
            builder.Append(@"[<]");
            this.AddLiteral(xml);
            if (allowAttributes)
            {
                this.AddWhiteSpace();
                this.AddCaptureAnything();
            }
            builder.Append(@"[>]");
        }

        public void AddXMLStartTag()
        {
            builder.Append(@"[<]");
        }
        public void AddXMLEndTag()
        {
            builder.Append(@"[>]");
        }

        public void AddXMLEnd(string xml)
        {
            this.AddWhiteSpace();
            builder.Append(@"[<]");
            builder.Append(@"[/]");
            this.AddLiteral(xml);
            builder.Append(@"[>]");
        }

    }
}
