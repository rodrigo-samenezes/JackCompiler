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
        private JackTokenizer tokenizer;
        private JackTokenizer.ClassifiedJackToken[] tokens;
        private int currentTokenIndex;
        public CompilationEngine(string filename)
        {
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

        private JackTokenizer.ClassifiedJackToken Nx(bool increment = true) {
            var current =  this.tokens[this.currentTokenIndex];
            if (increment){
                this.currentTokenIndex++;
            }
            return current;
        }

        /**'class' className '{' classVarDec* subroutineDec* '}' */
        public void CompileClass()
        {
            try {
                Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.CLASS);
                Nx().ExpectIdentifier();
                Nx().ExpectSymbol('{');
                this.CompileClassVarDecs();
                this.CompileSubroutineDecs();
                Nx().ExpectSymbol('}');

                
            }
            catch (Exception ex) {
                Console.WriteLine("Error: ", ex.Message);
            }
        }

        private void CompileClassVarDecs() {
            var expectedKeywords = new JackTokenizer.JackKeywordTypes[] {
                JackTokenizer.JackKeywordTypes.STATIC,
                JackTokenizer.JackKeywordTypes.FIELD
            };
            while(Nx(increment: false).ExpectKeyword(expectedKeywords, throwException: false)) {
                this.CompileClassVarDec();
            }
        }

        private void CompileClassVarDec() {
            //( 'static' | 'field' ) type varName ( ',' varName)* ';'
            Nx(); //keyword static | field
            Nx().ExpectType();
            Nx().ExpectIdentifier();
            var nx = Nx();
            nx.ExpectSymbol(new char[] {',', ';'});
            while(nx.ParseChar() == ',') {
                Nx().ExpectIdentifier();
                nx = Nx();
                nx.ExpectSymbol(new char[] {',', ';'});
            }
        }

        private void CompileSubroutineDecs() {
            var expectedKeywords = new JackTokenizer.JackKeywordTypes[] {
                JackTokenizer.JackKeywordTypes.CONSTRUCTOR,
                JackTokenizer.JackKeywordTypes.FUNCTION,
                JackTokenizer.JackKeywordTypes.METHOD
            };
            while(Nx(false).ExpectKeyword(expectedKeywords, throwException: false)) {
                this.CompileSubroutineDec();
            }
        }

        private void CompileSubroutineDec() {
            //( 'constructor' | 'function' | 'method' ) ( 'void' | type) subroutineName '(' parameterList ')' subroutineBody
            Nx(); //keyword 'constructor' | 'function' | 'method'
            Nx().ExpectType(acceptVoid: true);
            Nx().ExpectIdentifier();
            Nx().ExpectSymbol('(');
            this.CompileParameterList();
            Nx().ExpectSymbol(')');
            this.CompileSubroutineBody();
        }

        private void CompileParameterList() {
            while (Nx(increment: false).ExpectType(throwException: false)) {
                Nx(); // type
                Nx().ExpectIdentifier();
                if (Nx(increment: false).ExpectSymbol(',', throwException: false)){
                    Nx(); // ','
                }
            }
        }
        private void CompileSubroutineBody() {
            // '{' varDec* statements '}'
            Nx().ExpectSymbol('{');
            this.CompileVarDecs();
            this.CompileStatements();
            Nx().ExpectSymbol('}');
        }

        private void CompileVarDecs() {
            while(Nx(increment: false).ExpectKeyword(JackTokenizer.JackKeywordTypes.VAR, throwException: false)) {
                this.CompileVarDec();
            }
        }

        private void CompileVarDec() {
            Nx(); // 'var'
            Nx().ExpectType();
            Nx().ExpectIdentifier();
            var nx = Nx();
            nx.ExpectSymbol(new char[] {',', ';'});
            while(nx.ParseChar() == ',') {
                Nx().ExpectIdentifier();
                nx = Nx();
                nx.ExpectSymbol(new char[] {',', ';'});
            }
        }
        private void CompileStatements() {
            var expectedKeywords = new JackTokenizer.JackKeywordTypes[] {
                JackTokenizer.JackKeywordTypes.LET,
                JackTokenizer.JackKeywordTypes.IF,
                JackTokenizer.JackKeywordTypes.WHILE,
                JackTokenizer.JackKeywordTypes.DO,
                JackTokenizer.JackKeywordTypes.RETURN
            };
            while(Nx(increment: false).ExpectKeyword(expectedKeywords, throwException: false)) {
                this.CompileStatement();
            }
        }

        private void CompileStatement() {
            var nx = Nx(increment: false);
            switch (nx.ParseKeyWord()) {
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

        private void CompileLetStatement() {
            // 'let' varName ( '[' expression ']' )? '=' expression ';'
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.LET);
            Nx().Identifier();
            var nx = Nx();
            if (nx.ExpectSymbol('[', throwException: false)) {
                this.CompileExpression();
                Nx().ExpectSymbol(']');
                nx = Nx();
            }
            nx.ExpectSymbol('=');
            this.CompileExpression();
            Nx().ExpectSymbol(';');
        }

        private void CompileIfStatement() {
            // 'if' '(' expression ')' '{' statements '}' ( 'else' '{' statements '}' )?
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.IF);
            Nx().ExpectSymbol('(');
            this.CompileExpression();
            Nx().ExpectSymbol(')');
            Nx().ExpectSymbol('{');
            this.CompileStatements();
            Nx().ExpectSymbol('}');
            //else part
            var nx = Nx(increment: false);
            if (nx.ExpectKeyword(JackTokenizer.JackKeywordTypes.ELSE, throwException: false)) {
                Nx(); // 'else'
                Nx().ExpectSymbol('{');
                this.CompileStatements();
                Nx().ExpectSymbol('}');
            }
        }

        private void CompileWhileStatement() {
            // 'while' '(' expression ')' '{' statements '}'
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.WHILE);
            Nx().ExpectSymbol('(');
            this.CompileExpression();
            Nx().ExpectSymbol(')');
            Nx().ExpectSymbol('{');
            this.CompileStatements();
            Nx().ExpectSymbol('}');
        }
        private void CompileDoStatement() {
            // 'do' subroutineCall ';'
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.DO);
            this.CompileSubroutineCall();
            Nx().ExpectSymbol(';');
        }

        private void CompileReturnStatement() {
            // 'return' expression? ';'
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.RETURN);
            var nx = Nx(increment: false);
            if (!nx.ExpectSymbol(';', throwException: false)){
                Nx(); // ';'
            }
            this.CompileExpression();
            Nx().ExpectSymbol(';');
        }

        private void CompileExpression() {
            // term (op term)
            /* term: integerConstant | stringConstant | keywordConstant | 
                     varName | varName '[' expression ']' | subroutineCall |
                     '(' expression ')' | unaryOp term
             */
            // op: '+' | '-' | '* | '/' | '&' | '|' | '<' | '>' | '='
            this.CompileTerm();
        }

        private void CompileTerm() {
            /* term: varName | varName '[' expression ']' | subroutineCall |
             */
            var nx = Nx();
            if (
                nx.TokenType() == JackTokenizer.JackTokenType.intConst ||
                nx.TokenType() == JackTokenizer.JackTokenType.identifier
            ) {
                //if is constant
            }
            else {
                throw new Exception("Expected constInt or identifier but found '" + nx.value + "'");
            }
            
        }

        private void CompileSubroutineCall() {
            throw new NotImplementedException();
        }

        

    }

    public static class ClassifiedJackTokenExtensions {


        public static bool ExpectKeyword(this JackTokenizer.ClassifiedJackToken token, JackTokenizer.JackKeywordTypes keyword, bool throwException = true) {
            if( token.TokenType() == JackTokenizer.JackTokenType.keyword &&
                token.ParseKeyWord() == keyword){
                    return true;
            }
            else {
                if (throwException)
                    throw new Exception ("Expected keyword 'class' but found '" + token.value + "'.");
                else return false;
            }
        }

        public static bool ExpectKeyword(this JackTokenizer.ClassifiedJackToken token, JackTokenizer.JackKeywordTypes[] keywords, bool throwException = true) {
            if( token.TokenType() == JackTokenizer.JackTokenType.keyword &&
                keywords.Contains(token.ParseKeyWord())){
                    return true;
            }
            else {
                if (throwException){
                    
                    throw new Exception ("Expected one of these keywords '" + keywords.Select(x => x.ToString()).ToString() + "' but found '" + token.value + "'.");
                }
                else return false;
            }
        }
        public static string ExpectIdentifier(this JackTokenizer.ClassifiedJackToken token) {
            if( token.TokenType() == JackTokenizer.JackTokenType.identifier){
                    return token.Identifier();
                }
            else {
                throw new Exception ("Expected identifier but found '" + token.value + "'.");
            }
        }

        public static bool ExpectSymbol(this JackTokenizer.ClassifiedJackToken token, char symbol, bool throwException = true) {
            if( token.TokenType() == JackTokenizer.JackTokenType.symbol  
                && token.ParseChar() == symbol
            ){
                return true;
            }
            else {
                if (throwException){
                    throw new Exception ("Expected symbol '{' but found '" + token.value + "'.");
                }
                else return false;
            }
        }

        public static bool ExpectSymbol(this JackTokenizer.ClassifiedJackToken token, char[] symbols) {
            if (token.TokenType() == JackTokenizer.JackTokenType.symbol && symbols.Contains(token.ParseChar())){
                return true;
            }
            else {
                throw new Exception ("Expected on of theses symbols '" + symbols.ToString() + "' but found '" + token.value + "'.");
            }
        }

        public static bool ExpectType(this JackTokenizer.ClassifiedJackToken token, bool acceptVoid = false, bool throwException = true) {
            if( token.TokenType() == JackTokenizer.JackTokenType.identifier)
                return true;  
            else if (
                token.TokenType() == JackTokenizer.JackTokenType.keyword 
                && (
                    token.ParseKeyWord() == JackTokenizer.JackKeywordTypes.INT ||   
                    token.ParseKeyWord() == JackTokenizer.JackKeywordTypes.CHAR ||   
                    token.ParseKeyWord() == JackTokenizer.JackKeywordTypes.BOOLEAN ||
                    (acceptVoid ? token.ParseKeyWord() == JackTokenizer.JackKeywordTypes.VOID : false)
                )    
            ) {
                return true;
            }
            else {
                if (throwException) {
                    throw new Exception ("Expected a type keyword but found '" + token.value + "'.");
                }
                else return false;
            }
        }
    }
}