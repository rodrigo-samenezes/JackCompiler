using System;
using System.Collections.Generic;
using System.Linq;

namespace Jack
{
    public class CompilationEngine
    {
        private JackTokenizer.JackKeywordTypes[] keywordConstants = new JackTokenizer.JackKeywordTypes[]{
            JackTokenizer.JackKeywordTypes.TRUE,
            JackTokenizer.JackKeywordTypes.FALSE,
            JackTokenizer.JackKeywordTypes.NULL,
            JackTokenizer.JackKeywordTypes.THIS
        };

        private char[] opSymbols = new char[] { '+' , '-' , '*' , '/' , '&' , '|' , '<' , '>' , '=' };
        private JackTokenizer tokenizer;
        private JackTokenizer.ClassifiedJackToken[] tokens;
        private int currentTokenIndex;
        private string xml;
        public CompilationEngine(string filename)
        {
            xml = "";
            this.currentTokenIndex = 0;
            this.tokenizer = new JackTokenizer(filename);
            this.ExtractTokens();
        }

        private void ExtractTokens()
        {
            var tokensList = new List<JackTokenizer.ClassifiedJackToken>();
            while (this.tokenizer.Advance())
            {
                var token = this.tokenizer.GetClassifiedToken();
                tokensList.Add(token);
            }
            this.tokens = tokensList.ToArray();
        }

        private JackTokenizer.ClassifiedJackToken Nx(bool increment = true)
        {
            var current = this.tokens[this.currentTokenIndex];
            if (increment)
            {
                xml += current.toXml();
                this.currentTokenIndex++;
            }
            return current;
        }

        public string getXml()
        {
            return this.xml;
        }
        /**'class' className '{' classVarDec* subroutineDec* '}' */
        public void CompileClass()
        {
            try
            {
                xml += "<class>\n";
                Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.CLASS);
                Nx().ExpectIdentifier();
                Nx().ExpectSymbol('{');
                this.CompileClassVarDecs();
                this.CompileSubroutineDecs();
                Nx().ExpectSymbol('}');
                xml += "</class>\n";
                Console.WriteLine(xml);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("TokenIndex: " + currentTokenIndex.ToString());
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void CompileClassVarDecs()
        {
            var expectedKeywords = new JackTokenizer.JackKeywordTypes[] {
                JackTokenizer.JackKeywordTypes.STATIC,
                JackTokenizer.JackKeywordTypes.FIELD
            };
            while (Nx(increment: false).ExpectKeyword(expectedKeywords, throwException: false))
            {
                this.CompileClassVarDec();
            }
        }

        private void CompileClassVarDec()
        {
            xml += "<classVarDec>\n";
            //( 'static' | 'field' ) type varName ( ',' varName)* ';'
            Nx(); //keyword static | field
            Nx().ExpectType();
            Nx().ExpectIdentifier();
            var nx = Nx();
            nx.ExpectSymbol(new char[] { ',', ';' });
            while (nx.ParseChar() == ',')
            {
                Nx().ExpectIdentifier();
                nx = Nx();
                nx.ExpectSymbol(new char[] { ',', ';' });
            }
            xml += "</classVarDec>\n";
        }

        private void CompileSubroutineDecs()
        {
            var expectedKeywords = new JackTokenizer.JackKeywordTypes[] {
                JackTokenizer.JackKeywordTypes.CONSTRUCTOR,
                JackTokenizer.JackKeywordTypes.FUNCTION,
                JackTokenizer.JackKeywordTypes.METHOD
            };
            while (Nx(false).ExpectKeyword(expectedKeywords, throwException: false))
            {
                this.CompileSubroutineDec();
            }
        }

        private void CompileSubroutineDec()
        {
            xml += "<subroutineDec>\n";
            //( 'constructor' | 'function' | 'method' ) ( 'void' | type) subroutineName '(' parameterList ')' subroutineBody
            Nx(); //keyword 'constructor' | 'function' | 'method'
            Nx().ExpectType(acceptVoid: true);
            Nx().ExpectIdentifier();
            Nx().ExpectSymbol('(');
            this.CompileParameterList();
            Nx().ExpectSymbol(')');
            this.CompileSubroutineBody();
            xml += "</subroutineDec>\n";
        }

        private void CompileParameterList()
        {
            xml += "<parameterList>\n";
            while (Nx(increment: false).ExpectType(throwException: false))
            {
                Nx(); // type
                Nx().ExpectIdentifier();
                if (Nx(increment: false).ExpectSymbol(',', throwException: false))
                {
                    Nx(); // ','
                }
            }
            xml += "</parameterList>\n";
        }
        private void CompileSubroutineBody()
        {
            xml += "<subroutineBody>\n";
            // '{' varDec* statements '}'
            Nx().ExpectSymbol('{');
            this.CompileVarDecs();
            this.CompileStatements();
            Nx().ExpectSymbol('}');
            xml += "</subroutineBody>\n";
        }

        private void CompileVarDecs()
        {
            while (Nx(increment: false).ExpectKeyword(JackTokenizer.JackKeywordTypes.VAR, throwException: false))
            {
                this.CompileVarDec();
            }
        }

        private void CompileVarDec()
        {
            xml += "<varDec>\n";
            Nx(); // 'var'
            Nx().ExpectType();
            Nx().ExpectIdentifier();
            var nx = Nx();
            nx.ExpectSymbol(new char[] { ',', ';' });
            while (nx.ParseChar() == ',')
            {
                Nx().ExpectIdentifier();
                nx = Nx();
                nx.ExpectSymbol(new char[] { ',', ';' });
            }
            xml += "</varDec>\n";
        }
        private void CompileStatements()
        {

            xml += "<statements>\n";
            var expectedKeywords = new JackTokenizer.JackKeywordTypes[] {
                JackTokenizer.JackKeywordTypes.LET,
                JackTokenizer.JackKeywordTypes.IF,
                JackTokenizer.JackKeywordTypes.WHILE,
                JackTokenizer.JackKeywordTypes.DO,
                JackTokenizer.JackKeywordTypes.RETURN
            };
            while (Nx(increment: false).ExpectKeyword(expectedKeywords, throwException: false))
            {
                this.CompileStatement();
            }
            xml += "</statements>\n";
        }

        private void CompileStatement()
        {
            var nx = Nx(increment: false);
            switch (nx.ParseKeyWord())
            {
                case JackTokenizer.JackKeywordTypes.LET:
                    this.CompileLetStatement();
                    break;
                case JackTokenizer.JackKeywordTypes.IF:
                    this.CompileIfStatement();
                    break;
                case JackTokenizer.JackKeywordTypes.WHILE:
                    this.CompileWhileStatement();
                    break;
                case JackTokenizer.JackKeywordTypes.DO:
                    this.CompileDoStatement();
                    break;
                case JackTokenizer.JackKeywordTypes.RETURN:
                    this.CompileReturnStatement();
                    break;
            }
        }

        private void CompileLetStatement()
        {
            xml += "<letStatement>\n";
            // 'let' varName ( '[' expression ']' )? '=' expression ';'
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.LET);
            Nx().Identifier();
            var nx = Nx();
            if (nx.ExpectSymbol('[', throwException: false))
            {
                this.CompileExpression();
                Nx().ExpectSymbol(']');
                nx = Nx();
            }
            nx.ExpectSymbol('=');
            this.CompileExpression();
            Nx().ExpectSymbol(';');
            xml += "</letStatement>\n";
        }

        private void CompileIfStatement()
        {
            // 'if' '(' expression ')' '{' statements '}' ( 'else' '{' statements '}' )?
            xml += "<ifStatement>\n";
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.IF);
            Nx().ExpectSymbol('(');
            this.CompileExpression();
            Nx().ExpectSymbol(')');
            Nx().ExpectSymbol('{');
            this.CompileStatements();
            Nx().ExpectSymbol('}');
            //else part
            var nx = Nx(increment: false);
            if (nx.ExpectKeyword(JackTokenizer.JackKeywordTypes.ELSE, throwException: false))
            {
                Nx(); // 'else'
                Nx().ExpectSymbol('{');
                this.CompileStatements();
                Nx().ExpectSymbol('}');
            }
            xml += "</ifStatement>\n";
        }

        private void CompileWhileStatement()
        {
            // 'while' '(' expression ')' '{' statements '}'
            xml += "<whileStatement>\n";
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.WHILE);
            Nx().ExpectSymbol('(');
            this.CompileExpression();
            Nx().ExpectSymbol(')');
            Nx().ExpectSymbol('{');
            this.CompileStatements();
            Nx().ExpectSymbol('}');
            xml += "</whileStatement>\n";
        }
        private void CompileDoStatement()
        {
            // 'do' subroutineCall ';'
            xml += "<doStatement>\n";
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.DO);
            this.CompileSubroutineCall();
            Nx().ExpectSymbol(';');
            xml += "</doStatement>\n";
        }

        private void CompileReturnStatement()
        {
            xml += "<returnStatement>\n";
            // 'return' expression? ';'
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.RETURN);
            var nx = Nx(increment: false);
            if (nx.ExpectSymbol(';', throwException: false))
            {
                Nx(); // ';'
            }
            else
            {
                this.CompileExpression();
                Nx().ExpectSymbol(';');
            }
            xml += "</returnStatement>\n";
        }

        private void CompileExpression()
        {
            // term (op term)
            /* term: integerConstant | stringConstant | keywordConstant | 
                     varName | varName '[' expression ']' | subroutineCall |
                     '(' expression ')' | unaryOp term
             */
            // op: '+' | '-' | '* | '/' | '&' | '|' | '<' | '>' | '='
            xml += "<expression>\n";
            this.CompileTerm();
            if (Nx(increment: false).ExpectSymbol(this.opSymbols, throwException: false)){
                Nx();// op
                this.CompileTerm();
            }
            xml += "</expression>\n";
        }

        private void CompileTerm()
        {
            /* term: varName | varName '[' expression ']' | subroutineCall |
             */
            xml += "<term>\n";
            var nx = Nx();
            if (
                nx.TokenType() == JackTokenizer.JackTokenType.intConst ||
                nx.TokenType() == JackTokenizer.JackTokenType.identifier ||
                (nx.TokenType() == JackTokenizer.JackTokenType.keyword && keywordConstants.Contains(nx.ParseKeyWord()))
            )
            {
                //if is constant
            }
            else
            {
                throw new Exception("Expected constInt or identifier but found '" + nx.value + "'");
            }
            xml += "</term>\n";

        }

        private void CompileSubroutineCall()
        {

            Nx().ExpectIdentifier();
            var nx = Nx();
            nx.ExpectSymbol(new char[] { '(', '.' });

            if (nx.ParseChar() == '(')
            {
                this.CompileExpressionList();
                Nx().ExpectSymbol(')');
            }
            else
            {
                Nx().ExpectIdentifier();
                Nx().ExpectSymbol('(');
                this.CompileExpressionList();
                Nx().ExpectSymbol(')');
            }
        }

        private void CompileExpressionList()
        {
            xml += "<expressionList>\n";
            while (true)
            {
                var nx = Nx(increment: false);
                if (nx.ExpectSymbol(')', throwException: false))
                {
                    break;
                }
                this.CompileExpression();
                nx = Nx(increment: false);
                if (nx.ExpectSymbol(',', throwException: false))
                {
                    nx = Nx();
                }
            }
            xml += "</expressionList>\n";

        }

    }

    public static class ClassifiedJackTokenExtensions
    {


        public static bool ExpectKeyword(this JackTokenizer.ClassifiedJackToken token, JackTokenizer.JackKeywordTypes keyword, bool throwException = true)
        {
            if (token.TokenType() == JackTokenizer.JackTokenType.keyword &&
                token.ParseKeyWord() == keyword)
            {
                return true;
            }
            else
            {
                if (throwException)
                    throw new Exception("Expected keyword 'class' but found '" + token.value + "'.");
                else return false;
            }
        }

        public static bool ExpectKeyword(this JackTokenizer.ClassifiedJackToken token, JackTokenizer.JackKeywordTypes[] keywords, bool throwException = true)
        {
            if (token.TokenType() == JackTokenizer.JackTokenType.keyword &&
                keywords.Contains(token.ParseKeyWord()))
            {
                return true;
            }
            else
            {
                if (throwException)
                {

                    throw new Exception("Expected one of these keywords '" + keywords.Select(x => x.ToString()).ToString() + "' but found '" + token.value + "'.");
                }
                else return false;
            }
        }
        public static string ExpectIdentifier(this JackTokenizer.ClassifiedJackToken token)
        {
            if (token.TokenType() == JackTokenizer.JackTokenType.identifier)
            {
                return token.Identifier();
            }
            else
            {
                throw new Exception("Expected identifier but found '" + token.value + "'.");
            }
        }

        public static bool ExpectSymbol(this JackTokenizer.ClassifiedJackToken token, char symbol, bool throwException = true)
        {
            if (token.TokenType() == JackTokenizer.JackTokenType.symbol
                && token.ParseChar() == symbol
            )
            {
                return true;
            }
            else
            {
                if (throwException)
                {
                    throw new Exception("Expected symbol'" + symbol + "' but found '" + token.value + "'.");
                }
                else return false;
            }
        }

        public static bool ExpectSymbol(this JackTokenizer.ClassifiedJackToken token, char[] symbols, bool throwException = true)
        {
            if (token.TokenType() == JackTokenizer.JackTokenType.symbol && symbols.Contains(token.ParseChar()))
            {
                return true;
            }
            else
            {
                if (throwException)
                {
                    throw new Exception("Expected on of theses symbols '" + symbols.ToString() + "' but found '" + token.value + "'.");
                }
                else return false;
            }
        }

        public static bool ExpectType(this JackTokenizer.ClassifiedJackToken token, bool acceptVoid = false, bool throwException = true)
        {
            if (token.TokenType() == JackTokenizer.JackTokenType.identifier)
                return true;
            else if (
                token.TokenType() == JackTokenizer.JackTokenType.keyword
                && (
                    token.ParseKeyWord() == JackTokenizer.JackKeywordTypes.INT ||
                    token.ParseKeyWord() == JackTokenizer.JackKeywordTypes.CHAR ||
                    token.ParseKeyWord() == JackTokenizer.JackKeywordTypes.BOOLEAN ||
                    (acceptVoid ? token.ParseKeyWord() == JackTokenizer.JackKeywordTypes.VOID : false)
                )
            )
            {
                return true;
            }
            else
            {
                if (throwException)
                {
                    throw new Exception("Expected a type keyword but found '" + token.value + "'.");
                }
                else return false;
            }
        }

        public static string toXml(this JackTokenizer.ClassifiedJackToken token)
        {
            string xml = "";
            JackTokenizer.JackTokenType tokenClass = token.TokenType();
            xml += "<" + tokenClass.ToString() + "> ";
            switch (tokenClass)
            {
                case JackTokenizer.JackTokenType.identifier:
                case JackTokenizer.JackTokenType.intConst:
                case JackTokenizer.JackTokenType.keyword:
                    xml += token.value;
                    break;
                case JackTokenizer.JackTokenType.stringConst:
                    xml += token.ParseStringVal();
                    break;
                case JackTokenizer.JackTokenType.symbol:
                    char sym = token.ParseChar();
                    if (sym == '<')
                        xml += "&lt;";
                    else if (sym == '>')
                        xml += "&gt;";
                    else if (sym == '&')
                        xml += "&amp;";
                    else
                        xml += token.value;
                    break;
            }
            xml += " </" + tokenClass.ToString() + ">\n";
            return xml;
        }
    }
}