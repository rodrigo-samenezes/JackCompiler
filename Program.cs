using System;
using System.IO;
using System.Linq;

namespace Jack
{
    class Program
    {
        private static char[] forbidenSymbols = {'<', '>', '&'};
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando");
            JackTokenizer jk = new JackTokenizer("Ex.jack");
            string xml = "<tokens>\n";
            while (jk.Advance()) {
	            JackTokenizer.JackTokenType tokenClass = jk.TokenType();
	            xml += "<" + tokenClass.ToString() + ">";
	            switch(tokenClass) {
                    case JackTokenizer.JackTokenType.identifier:
                        xml += jk.Identifier();
                        break; 
                    case JackTokenizer.JackTokenType.intConst:
                        xml += jk.IntVal().ToString();
                        break; 
                    case JackTokenizer.JackTokenType.keyword:
                        xml += jk.KeyWord().ToString().ToLower();
                        break;
                    case JackTokenizer.JackTokenType.stringConst:
                        xml += jk.StringVal();
                        break;
                    case JackTokenizer.JackTokenType.symbol:
                        char sym = jk.Symbol();
                        if (sym == '<')
                            xml += "&lt;";
                        else if (sym == '>')
                            xml += "&gt;";
                        else if (sym == '&')
                            xml += "&amp;";
                        else 
                            xml += jk.Symbol();
                        break; 
                }
	            xml += "</" + tokenClass.ToString() + ">\n";
            }
            xml+= "</tokens>";
            File.WriteAllText("tokens.xml", xml);
        }
    }
}
