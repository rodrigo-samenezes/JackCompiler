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
            while (jk.advance()) {
	            JackTokenizer.JackTokenClass tokenClass = jk.tokenType();
	            xml += "<" + tokenClass.ToString() + ">";
	            switch(tokenClass) {
                    case JackTokenizer.JackTokenClass.identifier:
                        xml += jk.identifier();
                        break; 
                    case JackTokenizer.JackTokenClass.intConst:
                        xml += jk.intVal().ToString();
                        break; 
                    case JackTokenizer.JackTokenClass.keyword:
                        xml += jk.keyWord().ToString().ToLower();
                        break;
                    case JackTokenizer.JackTokenClass.stringConst:
                        xml += jk.stringVal();
                        break;
                    case JackTokenizer.JackTokenClass.symbol:
                        char sym = jk.symbol();
                        if (sym == '<')
                            xml += "&lt;";
                        else if (sym == '>')
                            xml += "&gt;";
                        else if (sym == '&')
                            xml += "&amp;";
                        else 
                            xml += jk.symbol();
                        break; 
                }
	            xml += "</" + tokenClass.ToString() + ">\n";
            }
            xml+= "</tokens>";
            File.WriteAllText("tokens.xml", xml);
        }
    }
}
