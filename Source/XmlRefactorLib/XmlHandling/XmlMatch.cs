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
            return this.Regex().Match(input, startAt);
        }

        public Regex Regex()
        {
            if (regex == null)
            {
                regex = new Regex(this.Expression, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            return regex;
        }

        private string Expression
        {
            get { return builder.ToString(); }
        }
        public XmlMatch()
        {
            builder = new StringBuilder(100);            
        }
        public XmlMatch AddWhiteSpaceRequired()
        {            
            builder.Append(@"[\s]+");
            return this;
        }
        public XmlMatch AddDelimter()
        {
            builder.Append(@"[\>\.\<\:\,\)\(\]\[\s]+");
            return this;
        }
        public XmlMatch AddWhiteSpaceNoLineBreaksRequired()
        {
            builder.Append(@"[ \t]+");
            return this;
        }

        public XmlMatch AddWhiteSpace()
        {
            builder.Append(@"[\s]*");  
            return this;
        }
        public XmlMatch AddSymbol(string symbol, int count = 1)
        {
            for (int i = 1 ; i<=count; i++)
                builder.Append(@"[\"+symbol+"]");
            return this;
        }
        public XmlMatch AddLiteral(string literal)
        {
            builder.Append(System.Text.RegularExpressions.Regex.Escape(literal));
            return this;
        }

        public XmlMatch AddLiteralOptional(string literal)
        {
            builder.Append($"({literal})?");
            return this;
        }

        public XmlMatch AddOneOfLiterals(string literal1, string literal2)
        {
            builder.Append($"({literal1}|{literal2})");
            return this;
        }
        public XmlMatch AddCapture()
        {
            this.AddWhiteSpace();
            builder.Append(@"([\S]+?)");
            return this;
        }
        public XmlMatch AddCaptureWord()
        {
            builder.Append(@"([\w]+?)");
            return this;
        }

        public XmlMatch AddCaptureAnything()
        {
            this.AddWhiteSpace();
            builder.Append(@"([\S\s]+?)");
            return this;
        }
        public XmlMatch AddCaptureOptional()
        {
            this.AddWhiteSpace();
            builder.Append(@"([\S]?)");            
            return this;
        }
        public XmlMatch AddCaptureOptional(string literal)
        {
            this.AddWhiteSpace();
            builder.Append("("+literal+")?");            
            return this;
        }
        public XmlMatch AddStartBracket()
        {
            this.AddWhiteSpace();
            builder.Append(@"[\[]");
            return this;
        }
        public XmlMatch AddEndBracket()
        {
            this.AddWhiteSpace();
            builder.Append(@"[\]]");
            return this;
        }
        public XmlMatch AddStartCurlyBracket()
        {
            this.AddWhiteSpace();
            builder.Append(@"[{]");
            return this;
        }
        public XmlMatch AddEndCurlyBracket()
        {
            this.AddWhiteSpace();
            builder.Append(@"[}]");
            return this;
        }
        public XmlMatch AddStartParenthesis()
        {
            this.AddWhiteSpace();
            builder.Append(@"[(]");            
            return this;
        }
        public XmlMatch AddEndParenthesis()
        {
            this.AddWhiteSpace();
            builder.Append("[)]");            
            return this;
        }
        public XmlMatch AddComma()
        {
            this.AddWhiteSpace();
            builder.Append("[,]");
            return this;
        }
        public XmlMatch AddCommaOptional()
        {
            this.AddWhiteSpace();
            builder.Append("[,]?");
            return this;
        }
        public XmlMatch AddNotOptional()
        {
            this.AddWhiteSpace();
            builder.Append("[!]?");
            return this;
        }
        public XmlMatch AddNot()
        {
            this.AddWhiteSpace();
            builder.Append("[!]");
            return this;
        }
        public XmlMatch AddNewLine()
        {            
            builder.Append("\r?\n");            
            return this;
        }
        public XmlMatch AddSemicolon()
        {
            this.AddWhiteSpace();
            builder.Append("[;]");
            return this;
        }
        public XmlMatch AddORSymbol()
        {
            this.AddWhiteSpace();
            builder.Append("[|][|]");
            return this;
        }
        public XmlMatch AddDoubleColon()
        {
            this.AddWhiteSpace();
            builder.Append("[:][:]");
            return this;
        }
        public XmlMatch AddEqual()
        {
            this.AddWhiteSpace();
            builder.Append("[=]");
            return this;
        }
        public XmlMatch AddDot()
        {
            this.AddWhiteSpace();
            builder.Append("[.]");            
            return this;
        }
        public XmlMatch AddXMLStart(string xml, Boolean allowAttributes = true)
        {
            builder.Append(@"[<]");
            this.AddLiteral(xml);
            if (allowAttributes)
            {
                this.AddWhiteSpace();
                this.AddCaptureAnything();
            }
            builder.Append(@"[>]");
            return this;
        }

        public XmlMatch AddXMLStartTag()
        {
            builder.Append(@"[<]");
            return this;
        }
        public XmlMatch AddXMLEndTag()
        {
            builder.Append(@"[>]");
            return this;
        }

        public XmlMatch AddXMLEnd(string xml)
        {
            this.AddWhiteSpace();
            builder.Append(@"[<]");
            builder.Append(@"[/]");
            this.AddLiteral(xml);
            builder.Append(@"[>]");
            return this;
        }
        public XmlMatch addMatch(XmlMatch match)
        {
            builder.Append(match.builder.ToString());
            return this;
        }
        
    }
}
