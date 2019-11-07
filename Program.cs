using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using System.Linq;

namespace Jack
{
    class Program
    {
        public class CliOptions
        {
            [Option('c', "compile", Required = true, HelpText = "Path of file or folder to be compiled")]
            public string InputFileOrFolder { get; set; }


            [Option('t', "tokens", Required = false, Default = false, HelpText = "Show xml with tokens")]
            public bool showXmlTokens { get; set; }


            [Option('s', "syntactic", Required = false, Default = false, HelpText = "Show xml with syntactic")]
            public bool showXmlSyntactic { get; set; }
        }

        private static char[] forbidenSymbols = { '<', '>', '&' };

        private static List<string> GetFilesToCompile(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                var files = Directory.GetFiles(path);
                var query = from f in files where f.EndsWith(".jack") || f.EndsWith(".JACK") select f;
                return query.ToList();
            }
            else
            {
                var list = new List<string>();
                list.Add(path);
                return list;
            }

        }
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CliOptions>(args)
            .WithParsed<CliOptions>(o =>
            {
                var files = GetFilesToCompile(o.InputFileOrFolder);
                foreach (string f in files)
                {
                    Console.WriteLine("Compiling file " + f);
                    if (o.showXmlTokens){
                        OnlyTokenizer(f);
                    }
                    CompilationEngine ce = new CompilationEngine(f);
                    ce.CompileClass();
                    if (o.showXmlSyntactic){
                        File.WriteAllText(f.Substring(0, f.Length - 4) + "syntactic.xml", PrettyXml.Fire(ce.getXml()));
                    }
                    Console.WriteLine("Sintático FIM");
                }
            });

        }

        static void OnlyTokenizer(string file)
        {
            Console.WriteLine("Iniciando Tokens");
            JackTokenizer jk = new JackTokenizer(file);
            string xml = "<tokens>\n";
            while (jk.Advance())
            {
                JackTokenizer.JackTokenType tokenClass = jk.TokenType();
                xml += "<" + tokenClass.ToString() + ">";
                switch (tokenClass)
                {
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
            xml += "</tokens>";
            File.WriteAllText(file.Substring(0, file.Length - 4) + "tokens.xml", xml);
            Console.WriteLine("Tokens FIM");

        }
    }
}
