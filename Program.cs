using System;

namespace Jack
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando");
            JackTokenizer jk = new JackTokenizer("Ex.jack");
            Console.WriteLine("Tokens: ");
            foreach(var s in jk.tokens) {
                Console.Write(s + " ");
            }
            Console.WriteLine("\n\nFim");
            Console.WriteLine("\n\nCode:");
            Console.WriteLine(jk.code);


        }
    }
}
