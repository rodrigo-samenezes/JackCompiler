using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace Jack
{
    public class JackTokenizer
    {
        public enum JackTokenType {
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



        public class ClassifiedJackToken {
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

            public JackTokenType type;
            public string value;
            private int intValue;

            public ClassifiedJackToken(string token) {
                this.value = token;
                if (RESERVED_WORDS.Contains(token)) {
                    this.type = JackTokenType.keyword;
                }
                else if (token.StartsWith("\"") && token.EndsWith("\"")) {
                    this.type = JackTokenType.stringConst;
                }
                else if (SYMBOLS.Contains(token)) {
                    this.type = JackTokenType.symbol;
                }
                else if (int.TryParse(token, out this.intValue)) {
                    this.type = JackTokenType.intConst;
                }
                else {
                    this.type = JackTokenType.identifier;
                }
            }

            public JackKeywordTypes ParseKeyWord() {
                if (this.type != JackTokenType.keyword)
                    throw new Exception("You cannot parse to keyword a '" + this.type.ToString() + "' token");
                return (JackKeywordTypes)Enum.Parse(typeof(JackKeywordTypes), this.value.ToUpper());
            }

            public char ParseChar() {
                if (this.type != JackTokenType.symbol)
                    throw new Exception("You cannot parse to char a '" + this.type.ToString() + "' token");
                return this.value[0];
            }

            public string ParseStringVal() {
                if (this.type != JackTokenType.stringConst)
                    throw new Exception("You cannot parse to stringVal a '" + this.type.ToString() + "' token");
                return this.value.Replace("\"", "");
            }

            public int ParseInt() {
                if (this.type != JackTokenType.intConst)
                    throw new Exception("You cannot parse to intConst a '" + this.type.ToString() + "' token");
                return this.intValue;
            }

            public JackTokenType TokenType() {
                return this.type;
            }

            public string Identifier() {
                return this.value;
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
            this.RemoveComents();
            this.FindTokens();
            this.ClassifyTokens();
            this.currentTokenIndex = 0;
        }

        private void RemoveComents(){
            Regex rx = new Regex(@"(//.*)|(/\*[\s\S]*\*/)*", RegexOptions.Compiled);
            this.code = rx.Replace(this.code, "");
        }

        private void FindTokens() {
            Regex rx = new Regex("\\{|\\}|\\(|\\)|\\[|\\]|\\.|\\,|;|\\+|-|\\*|\\/|&|\\||<|>|=|~|(\".*\")|(([a-z]|[A-Z]|_)+([a-z]|[A-Z]|_|[0-9])*)|[0-9]+");
            MatchCollection matches = rx.Matches(this.code);
            this.tokens = new string[matches.Count];
            int i = 0;
            foreach (Match m in matches) {
                this.tokens[i++] = m.Value;
            }
        }

        private void ClassifyTokens() {
            this.classifiedTokens = new ClassifiedJackToken[this.tokens.Length];
            int i = 0;
            foreach(var tk in this.tokens) 
            {
                this.classifiedTokens[i++] =  new ClassifiedJackToken(tk);
            }
        }

        public bool HasMoreTokens() {
            return this.currentTokenIndex < this.tokens.Length;
        }

        public bool Advance() {
            if(this.currentTokenIndex < this.tokens.Length){
                this.currentToken = this.classifiedTokens[this.currentTokenIndex];
                this.currentTokenIndex++;
                return true;
            }
            else {
                return false;
            }
        }

        public JackTokenType TokenType() {
            return this.currentToken.TokenType();
        }

        public JackKeywordTypes KeyWord() {
            return this.currentToken.ParseKeyWord();
        }

        public char Symbol() {
            return this.currentToken.ParseChar();
        }

        public string Identifier() {
            return this.currentToken.value;
        }

        public int IntVal() {
            return this.currentToken.ParseInt();
        }

        public string StringVal() {
            return this.currentToken.ParseStringVal();
        }

        public ClassifiedJackToken GetClassifiedToken() {
                return this.currentToken;
        }

        
    }
}