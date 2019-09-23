using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace Jack
{
    public class JackTokenizer
    {
        public enum JackTokenClass {
            keyword,
            identifier,
            symbol,
            stringConst,
            intConst
        }

        public enum JackKeywordTypes {
            CLASS, CONSTRUCTOR, FUNCTION, METHOD,FIELD, STATIC, VAR, INT, CHAR, BOOLEAN,
            VOID, TRUE, FALSE, NULL, THIS, LET, DO, IF, ELSE, WHILE, RETURN
        }



        private class ClassifiedJackToken {
            private static string[] RESERVED_WORDS = {
                "class", "constructor", "function",
                "method", "field", "static", "var", "int",
                "char", "boolean", "void", "true", "false",
                "null", "this", "let", "do", "if", "else",
                "while", "return"
            };

            private static string[] SYMBOLS = {
                "{", "}", "(", ")", "[", "]", ".", ",", ";", "+", "-", "*",
                "/", "&", "|", "<", ">", "=", "~"
            };

            public JackTokenClass type;
            public string value;
            private int intValue;

            public ClassifiedJackToken(string token) {
                this.value = token;
                if (RESERVED_WORDS.Contains(token)) {
                    this.type = JackTokenClass.keyword;
                }
                else if (token.StartsWith("\"") && token.EndsWith("\"")) {
                    this.type = JackTokenClass.stringConst;
                }
                else if (SYMBOLS.Contains(token)) {
                    this.type = JackTokenClass.symbol;
                }
                else if (int.TryParse(token, out this.intValue)) {
                    this.type = JackTokenClass.intConst;
                }
                else {
                    this.type = JackTokenClass.identifier;
                }
            }

            public JackKeywordTypes parseKeyWord() {
                if (this.type != JackTokenClass.keyword)
                    throw new Exception("You cannot parse to keyword a '" + this.type.ToString() + "' token");
                return (JackKeywordTypes)Enum.Parse(typeof(JackKeywordTypes), this.value.ToUpper());
            }

            public char parseChar() {
                if (this.type != JackTokenClass.symbol)
                    throw new Exception("You cannot parse to char a '" + this.type.ToString() + "' token");
                return this.value[0];
            }

            public string parseStringVal() {
                if (this.type != JackTokenClass.stringConst)
                    throw new Exception("You cannot parse to stringVal a '" + this.type.ToString() + "' token");
                return this.value.Replace("\"", "");
            }

            public int parseInt() {
                if (this.type != JackTokenClass.intConst)
                    throw new Exception("You cannot parse to intConst a '" + this.type.ToString() + "' token");
                return this.intValue;
            }
        }
        
        


        private string code;
        private string[] tokens;
        private ClassifiedJackToken[] classifiedTokens;

        private int currentTokenIndex;
        private ClassifiedJackToken currentToken;

        public JackTokenizer(string fileName)
        {
            using (StreamReader sr = new StreamReader(fileName))
            {
                this.code = sr.ReadToEnd();
            }
            this.removeComents();
            this.findTokens();
            this.classifyTokens();
            this.currentTokenIndex = 0;
        }

        private void removeComents(){
            Regex rx = new Regex(@"//.*", RegexOptions.Compiled);
            this.code = rx.Replace(this.code, "");
        }

        private void findTokens() {
            Regex rx = new Regex("\\{|\\}|\\(|\\)|\\[|\\]|\\.|\\,|;|\\+|-|\\*|\\/|&|\\||<|>|=|~|(\".*\")|(([a-z]|[A-Z]|_)+([a-z]|[A-Z]|_|[0-9])*)|[0-9]+");
            MatchCollection matches = rx.Matches(this.code);
            this.tokens = new string[matches.Count];
            int i = 0;
            foreach (Match m in matches) {
                this.tokens[i++] = m.Value;
            }
        }

        private void classifyTokens() {
            this.classifiedTokens = new ClassifiedJackToken[this.tokens.Length];
            int i = 0;
            foreach(var tk in this.tokens) 
            {
                this.classifiedTokens[i++] =  new ClassifiedJackToken(tk);
            }
        }

        public bool hasMoreTokens() {
            return this.currentTokenIndex < this.tokens.Length;
        }

        public bool advance() {
            if(this.currentTokenIndex < this.tokens.Length){
                this.currentToken = this.classifiedTokens[this.currentTokenIndex];
                this.currentTokenIndex++;
                return true;
            }
            else {
                return false;
            }
        }

        public JackTokenClass tokenType() {
            return this.currentToken.type;
        }

        public JackKeywordTypes keyWord() {
            return this.currentToken.parseKeyWord();
        }

        public char symbol() {
            return this.currentToken.parseChar();
        }

        public string identifier() {
            return this.currentToken.value;
        }

        public int intVal() {
            return this.currentToken.parseInt();
        }

        public string stringVal() {
            return this.currentToken.parseStringVal();
        }

        
    }
}