using System;
using System.Collections.Generic;
using System.IO;
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

        private char[] opSymbols = new char[] { '+', '-', '*', '/', '&', '|', '<', '>', '=' };
        private JackTokenizer tokenizer;
        private JackTokenizer.ClassifiedJackToken[] tokens;
        private int currentTokenIndex;
        private string xml;
        private string className;
        private VMWriter vm;

        SymbolTable symbolTable;

        private string filepath;

        Dictionary<SymbolTable.Kind, VMWriter.Segment> kind2segmentMapper;
        int ifLabelsCount;
        int whileLabelsCount;

        public CompilationEngine(string filepath)
        {
            this.filepath = filepath;
            xml = "";
            this.currentTokenIndex = 0;
            this.kind2segmentMapper = new Dictionary<SymbolTable.Kind, VMWriter.Segment>();
            this.kind2segmentMapper.Add(SymbolTable.Kind.STATIC, VMWriter.Segment.STATIC);
            this.kind2segmentMapper.Add(SymbolTable.Kind.FIELD, VMWriter.Segment.THIS);
            this.kind2segmentMapper.Add(SymbolTable.Kind.VAR, VMWriter.Segment.LOCAL);
            this.kind2segmentMapper.Add(SymbolTable.Kind.ARG, VMWriter.Segment.ARG);
            this.ifLabelsCount = 0;
            this.whileLabelsCount = 0;
            this.tokenizer = new JackTokenizer(filepath);
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

        private JackTokenizer.ClassifiedJackToken Nx(bool increment = true, int incrementValue = 1)
        {
            var current = this.tokens[this.currentTokenIndex + incrementValue - 1];
            if (increment)
            {
                xml += current.toXml();
                this.currentTokenIndex += incrementValue;
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
                this.symbolTable = new SymbolTable();
                xml += "<class>\n";
                Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.CLASS);
                var classNameToken = Nx();
                classNameToken.ExpectIdentifier();
                this.className = classNameToken.Identifier();
                this.vm = new VMWriter(File.Create(this.filepath.Replace(".jack", ".vm")));
                Nx().ExpectSymbol('{');
                this.CompileClassVarDecs();
                this.CompileSubroutineDecs();
                Nx().ExpectSymbol('}');
                xml += "</class>\n";
                //Console.WriteLine(xml);
                this.vm.Close();

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
            var kindToken = Nx(); //keyword static | field
            var stKind = kindToken.ParseKeyWord() == JackTokenizer.JackKeywordTypes.STATIC ? SymbolTable.Kind.STATIC : SymbolTable.Kind.FIELD;
            var typeToken = Nx();
            typeToken.ExpectType();
            var identifierToken = Nx();
            identifierToken.ExpectIdentifier();
            this.symbolTable.Define(identifierToken.Identifier(), typeToken.Identifier(), stKind);
            var nx = Nx();
            nx.ExpectSymbol(new char[] { ',', ';' });
            while (nx.ParseChar() == ',')
            {
                identifierToken = Nx();
                identifierToken.ExpectIdentifier();
                nx = Nx();
                this.symbolTable.Define(identifierToken.Identifier(), typeToken.Identifier(), stKind);
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
            var typeOfSubroutineToken = Nx(); //keyword 'constructor' | 'function' | 'method'

            Nx().ExpectType(acceptVoid: true);
            var subroutineNameToken = Nx();
            subroutineNameToken.ExpectIdentifier();
            var subroutineName = subroutineNameToken.Identifier();
            this.symbolTable.StartSubroutine();
            if (typeOfSubroutineToken.ParseKeyWord() == JackTokenizer.JackKeywordTypes.METHOD)
            {
                this.symbolTable.Define("this", this.className, SymbolTable.Kind.ARG);
            }
            Nx().ExpectSymbol('(');
            this.CompileParameterList(subroutineName);
            Nx().ExpectSymbol(')');
            this.CompileSubroutineBody(subroutineName, typeOfSubroutineToken.ParseKeyWord());
            xml += "</subroutineDec>\n";
        }

        private void CompileParameterList(string subroutineName)
        {
            xml += "<parameterList>\n";
            while (Nx(increment: false).ExpectType(throwException: false))
            {
                var typeToken = Nx(); // type
                var varNameToken = Nx();
                varNameToken.ExpectIdentifier();
                symbolTable.Define(varNameToken.Identifier(), typeToken.value, SymbolTable.Kind.ARG);
                if (Nx(increment: false).ExpectSymbol(',', throwException: false))
                {
                    Nx(); // ','
                }
            }
            xml += "</parameterList>\n";
        }
        private void CompileSubroutineBody(string subroutineName, JackTokenizer.JackKeywordTypes subroutineKind)
        {
            xml += "<subroutineBody>\n";
            // '{' varDec* statements '}'
            Nx().ExpectSymbol('{');
            this.CompileVarDecs(subroutineName, subroutineKind);

            switch (subroutineKind) {
                case JackTokenizer.JackKeywordTypes.CONSTRUCTOR:
                    this.vm.WritePush(VMWriter.Segment.CONST, this.symbolTable.VarCount(SymbolTable.Kind.FIELD));
                    this.vm.WriteCall("Memory.alloc", 1);
                    this.vm.WritePop(VMWriter.Segment.POINTER, 0);
                    break;

                case JackTokenizer.JackKeywordTypes.METHOD:
                    this.vm.WritePush(VMWriter.Segment.ARG, 0);
		            this.vm.WritePop(VMWriter.Segment.POINTER, 0);
                    break;
            }

            this.CompileStatements(subroutineName);
            Nx().ExpectSymbol('}');
            xml += "</subroutineBody>\n";
        }

        private void CompileVarDecs(string subroutineName, JackTokenizer.JackKeywordTypes subroutineKind)
        {
            while (Nx(increment: false).ExpectKeyword(JackTokenizer.JackKeywordTypes.VAR, throwException: false))
            {
                this.CompileVarDec(subroutineName);
            }

            if (subroutineKind == JackTokenizer.JackKeywordTypes.CONSTRUCTOR)
            {
                
            }
            this.vm.WriteFunction($"{this.className}.{subroutineName}", this.symbolTable.VarCount(SymbolTable.Kind.VAR));
        }

        private void CompileVarDec(string subroutineName)
        {
            xml += "<varDec>\n";
            Nx(); // 'var'
            var typeToken = Nx();
            typeToken.ExpectType();
            var identifierToken = Nx();
            identifierToken.ExpectIdentifier();
            this.symbolTable.Define(identifierToken.Identifier(), typeToken.value, SymbolTable.Kind.VAR);
            var nx = Nx();
            nx.ExpectSymbol(new char[] { ',', ';' });
            while (nx.ParseChar() == ',')
            {
                identifierToken = Nx();
                identifierToken.ExpectIdentifier();
                this.symbolTable.Define(identifierToken.Identifier(), typeToken.value, SymbolTable.Kind.VAR);
                nx = Nx();
                nx.ExpectSymbol(new char[] { ',', ';' });
            }
            xml += "</varDec>\n";
        }
        private void CompileStatements(string subroutineName)
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
                this.CompileStatement(subroutineName);
            }
            xml += "</statements>\n";
        }

        private void CompileStatement(string subroutineName)
        {
            var nx = Nx(increment: false);
            switch (nx.ParseKeyWord())
            {
                case JackTokenizer.JackKeywordTypes.LET:
                    this.CompileLetStatement(subroutineName);
                    break;
                case JackTokenizer.JackKeywordTypes.IF:
                    this.CompileIfStatement(subroutineName);
                    break;
                case JackTokenizer.JackKeywordTypes.WHILE:
                    this.CompileWhileStatement(subroutineName);
                    break;
                case JackTokenizer.JackKeywordTypes.DO:
                    this.CompileDoStatement(subroutineName);
                    break;
                case JackTokenizer.JackKeywordTypes.RETURN:
                    this.CompileReturnStatement(subroutineName);
                    break;
            }
        }

        private void CompileLetStatement(string subroutineName)
        {
            xml += "<letStatement>\n";
            // 'let' varName ( '[' expression ']' )? '=' expression ';'
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.LET);
            var varNameToken = Nx(true, 1);
            var varName = varNameToken.Identifier();
            var nx = Nx();
            var arrayDetected = false;
            if (nx.ExpectSymbol('[', throwException: false))
            {
                this.CompileExpression();
                Nx().ExpectSymbol(']');
                this.vm.WritePush(this.kind2segmentMapper[this.symbolTable.KindOf(varName)], this.symbolTable.IndexOf(varName));
                this.vm.WriteArithmetic(VMWriter.Command.ADD);
                nx = Nx();
                arrayDetected = true;
            }
            nx.ExpectSymbol('=');
            this.CompileExpression();
            Nx().ExpectSymbol(';');
            if (arrayDetected) {
                this.vm.WritePop(VMWriter.Segment.TEMP, 0);
                this.vm.WritePop(VMWriter.Segment.POINTER, 1);
                this.vm.WritePush(VMWriter.Segment.TEMP, 0);
                this.vm.WritePop(VMWriter.Segment.THAT, 0);
            }
            else {
                this.vm.WritePop(this.kind2segmentMapper[this.symbolTable.KindOf(varName)], this.symbolTable.IndexOf(varName));
            }
            xml += "</letStatement>\n";
        }

        private void CompileIfStatement(string subroutineName)
        {
            // 'if' '(' expression ')' '{' statements '}' ( 'else' '{' statements '}' )?
            xml += "<ifStatement>\n";
            int ifCount = this.ifLabelsCount++;
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.IF);
            Nx().ExpectSymbol('(');
            this.CompileExpression();
            Nx().ExpectSymbol(')');
            this.vm.WriteIf($"IF_TRUE{ifCount}");
            this.vm.WriteGoto($"IF_FALSE{ifCount}");
            this.vm.WriteLabel($"IF_TRUE{ifCount}");
            Nx().ExpectSymbol('{');
            this.CompileStatements(subroutineName);
            Nx().ExpectSymbol('}');
            //else part
            var nx = Nx(increment: false);
            if (nx.ExpectKeyword(JackTokenizer.JackKeywordTypes.ELSE, throwException: false))
            {
                this.vm.WriteGoto($"IF_END{ifCount}");
                this.vm.WriteLabel($"IF_FALSE{ifCount}");
                Nx(); // 'else'
                Nx().ExpectSymbol('{');
                this.CompileStatements(subroutineName);
                Nx().ExpectSymbol('}');
                this.vm.WriteLabel($"IF_END{ifCount}");
            }
            else
            {
                this.vm.WriteLabel($"IF_FALSE{ifCount}");
            }
            xml += "</ifStatement>\n";
        }

        private void CompileWhileStatement(string subroutineName)
        {
            // 'while' '(' expression ')' '{' statements '}'
            int whileCount = this.whileLabelsCount++;
            this.vm.WriteLabel($"WHILE_EXP{whileCount}");
            xml += "<whileStatement>\n";
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.WHILE);
            Nx().ExpectSymbol('(');
            this.CompileExpression();
            this.vm.WriteArithmetic(VMWriter.Command.NOT);
            this.vm.WriteIf($"WHILE_END{whileCount}");
            Nx().ExpectSymbol(')');
            Nx().ExpectSymbol('{');
            this.CompileStatements(subroutineName);
            this.vm.WriteGoto($"WHILE_EXP{whileCount}");
            Nx().ExpectSymbol('}');
            this.vm.WriteLabel($"WHILE_END{whileCount}");
            xml += "</whileStatement>\n";
        }
        private void CompileDoStatement(string subroutineName)
        {
            // 'do' subroutineCall ';'
            xml += "<doStatement>\n";
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.DO);
            this.CompileSubroutineCall();
            this.vm.WritePop(VMWriter.Segment.TEMP, 0);
            Nx().ExpectSymbol(';');
            xml += "</doStatement>\n";
        }

        private void CompileReturnStatement(string subroutineName)
        {
            xml += "<returnStatement>\n";
            // 'return' expression? ';'
            Nx().ExpectKeyword(JackTokenizer.JackKeywordTypes.RETURN);
            var nx = Nx(increment: false);
            if (nx.ExpectSymbol(';', throwException: false)) //caso seja return;
            {
                Nx(); // ';'
                this.vm.WritePush(VMWriter.Segment.CONST, 0);
                this.vm.WriteReturn();
            }
            else //caso seja return xxx;
            {
                this.CompileExpression();
                Nx().ExpectSymbol(';');
                this.vm.WriteReturn();
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
            while (Nx(increment: false).ExpectSymbol(this.opSymbols, throwException: false))
            {
                var opToken = Nx();// op
                this.CompileTerm();
                this.CompileOp(opToken);
            }
            xml += "</expression>\n";
        }

        private void CompileTerm()
        {
            /* term: varName | varName '[' expression ']' | subroutineCall | 
                integerContasnt | stringConstant | keywordConstant | '(' expression ')' | unaryOp term
             */
            xml += "<term>\n";
            var nx = Nx(false);
            if (
                nx.TokenType() == JackTokenizer.JackTokenType.intConst ||
                nx.TokenType() == JackTokenizer.JackTokenType.identifier ||
                nx.TokenType() == JackTokenizer.JackTokenType.stringConst ||
                (nx.TokenType() == JackTokenizer.JackTokenType.keyword && keywordConstants.Contains(nx.ParseKeyWord())) ||
                (nx.TokenType() == JackTokenizer.JackTokenType.symbol && nx.ExpectSymbol(new char[] { '(', '~', '-' }, false))
            )
            {
                if (nx.TokenType() == JackTokenizer.JackTokenType.keyword && keywordConstants.Contains(nx.ParseKeyWord())) { //TRUE, FALSE, NULL, THIS
                    nx = Nx();
                    var keyCons = nx.ParseKeyWord();
                    this.vm.WriteComment($"keywordConst {keyCons.ToString()}");
                    switch (keyCons) {
                        case JackTokenizer.JackKeywordTypes.NULL:
                        case JackTokenizer.JackKeywordTypes.FALSE:
                            this.vm.WritePush(VMWriter.Segment.CONST, 0);
                            break;
                        case JackTokenizer.JackKeywordTypes.TRUE:
                            this.vm.WritePush(VMWriter.Segment.CONST, 1);
                            this.vm.WriteArithmetic(VMWriter.Command.NEG);
                            break;
                        case JackTokenizer.JackKeywordTypes.THIS:
                            this.vm.WritePush(VMWriter.Segment.POINTER, 0);
                            break;
                    }
                    this.vm.WriteComment("END keywordConst");
                }

                else if (nx.TokenType() == JackTokenizer.JackTokenType.identifier) // varName | subroutineCall
                {

                    if (Nx(false, 2).ExpectSymbol('[', false)) // array
                    {
                        var arrayName = nx.Identifier();
                        Nx(); // identifier
                        Nx(); // [
                        this.CompileExpression();
                        Nx().ExpectSymbol(']');
                        
                        this.vm.WritePush(this.kind2segmentMapper[this.symbolTable.KindOf(arrayName)], this.symbolTable.IndexOf(arrayName));
                        this.vm.WriteArithmetic(VMWriter.Command.ADD);
                        this.vm.WritePop(VMWriter.Segment.POINTER, 1);
			            this.vm.WritePush(VMWriter.Segment.THAT, 0);
                    }
                    else if (Nx(false, 2).ExpectSymbol(new char[] { '(', '.' }, false)) //subroutineCall
                    {
                        this.CompileSubroutineCall();
                    }
                    else //varName 
                    {
                        var token = Nx(); //pure identifier
                        var varName = token.Identifier();
                        this.vm.WritePush(this.kind2segmentMapper[this.symbolTable.KindOf(varName)], this.symbolTable.IndexOf(varName));
                    }
                }
                else if (nx.TokenType() == JackTokenizer.JackTokenType.symbol)
                {
                    Nx(); 
                    if (nx.ExpectSymbol('(', false)) // '(' expression ')'
                    {
                        this.CompileExpression();
                        Nx().ExpectSymbol(')');
                    }
                    else //unareOp term
                    {
                        this.CompileTerm();
                        this.vm.WriteArithmetic(nx.ParseChar() == '~' ? VMWriter.Command.NOT : VMWriter.Command.NEG);
                    }
                }
                else //const  (int or string)
                {
                    var token = Nx();
                    if (token.type == JackTokenizer.JackTokenType.intConst)
                    {
                        this.vm.WritePush(VMWriter.Segment.CONST, token.ParseInt());
                    }
                    else if (token.type == JackTokenizer.JackTokenType.stringConst) {
                        var str = token.ParseStringVal();
                        this.vm.WritePush(VMWriter.Segment.CONST, str.Length);
                        this.vm.WriteComment($"stringConst: \"{str}\" - len: {str.Length}");
                        this.vm.WriteCall("String.new", 1);
                        
                        for (int i = 0; i < str.Length; i++) {
                            var c = str[i];
                            this.vm.WritePush(VMWriter.Segment.CONST, (int)c);
                            this.vm.WriteCall("String.appendChar", 2);
                        }
                        this.vm.WriteComment("END stringConst");

                    }
                }

            }
            else
            {
                throw new Exception("Expected constInt or identifier but found '" + nx.value + "'");
            }
            xml += "</term>\n";

        }

        private void CompileSubroutineCall()
        {

            var identifierToken = Nx(true, 1);
            identifierToken.ExpectIdentifier();
            var vm_FunctionName = identifierToken.Identifier();
            int vm_nArgs;
            var nx = Nx();
            nx.ExpectSymbol(new char[] { '(', '.' });

            if (nx.ParseChar() == '(') //metodo do objeto atual
            {
                this.vm.WritePush(VMWriter.Segment.POINTER, 0);
                vm_nArgs = this.CompileExpressionList() + 1;
                Nx().ExpectSymbol(')');
                vm_FunctionName = this.className + "." + vm_FunctionName;
            }
            else //chamada de function de outra classe ou instancia de objeto
            {
                var classOrObjectName = identifierToken.Identifier();
                identifierToken = Nx();
                identifierToken.ExpectIdentifier();

                int indexOfVar = this.symbolTable.IndexOf(classOrObjectName);
                if (indexOfVar == -1) { //class function
                    vm_FunctionName += $".{identifierToken.Identifier()}";
                    vm_nArgs = 0;
                }
                else { //object method
                    Console.WriteLine("aqui e agora");
                    this.vm.WritePush(this.kind2segmentMapper[this.symbolTable.KindOf(classOrObjectName)], this.symbolTable.IndexOf(classOrObjectName)); 
                    var classNameOfObject = this.symbolTable.TypeOf(classOrObjectName);
                    vm_FunctionName = $"{classNameOfObject}.{identifierToken.Identifier()}";
                    vm_nArgs = 1;
                }
                Nx().ExpectSymbol('(');
                vm_nArgs += this.CompileExpressionList();
                Nx().ExpectSymbol(')');
            }

            this.vm.WriteCall(vm_FunctionName, vm_nArgs);
        }

        private int CompileExpressionList()
        {
            var nItems = 0;
            xml += "<expressionList>\n";
            while (true)
            {
                var nx = Nx(increment: false);
                if (nx.ExpectSymbol(')', throwException: false))
                {
                    break;
                }
                nItems++;
                this.CompileExpression();
                nx = Nx(increment: false);
                if (nx.ExpectSymbol(',', throwException: false))
                {
                    nx = Nx();
                }
            }
            xml += "</expressionList>\n";
            return nItems;
        }

        private void CompileOp(JackTokenizer.ClassifiedJackToken token)
        {
            var sym = token.ParseChar();
            switch (sym)
            {
                case '+':
                    this.vm.WriteArithmetic(VMWriter.Command.ADD);
                    break;
                case '-':
                    this.vm.WriteArithmetic(VMWriter.Command.SUB);
                    break;
                case '*':
                    this.vm.WriteCall("Math.multiply", 2);
                    break;
                case '/':
                    this.vm.WriteCall("Math.divide", 2);
                    break;
                case '>':
                    this.vm.WriteArithmetic(VMWriter.Command.GT);
                    break;
                case '<':
                    this.vm.WriteArithmetic(VMWriter.Command.LT);
                    break;
                case '=':
                    this.vm.WriteArithmetic(VMWriter.Command.EQ);
                    break;
                case '&':
                    this.vm.WriteArithmetic(VMWriter.Command.AND);
                    break;
                case '|':
                    this.vm.WriteArithmetic(VMWriter.Command.OR);
                    break;
            }
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

        public static string toXml(this JackTokenizer.ClassifiedJackToken token, SymbolTable symbolTable = null)
        {
            string xml = "";
            JackTokenizer.JackTokenType tokenClass = token.TokenType();
            xml += $"<{tokenClass.ToString()}{token.getIdentifierCategory(symbolTable)}>";
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
            xml += "</" + tokenClass.ToString() + ">\n";
            return xml;
        }

        public static string getIdentifierCategory(this JackTokenizer.ClassifiedJackToken token, SymbolTable symbolTable = null)
        {
            if (token.TokenType() == JackTokenizer.JackTokenType.identifier && symbolTable != null)
            {
                var category = symbolTable.KindOf(token.Identifier());
                if (category != null)
                {
                    return " category=\"" + category.ToString().ToLower() + "\"";
                }
            }
            return "";
        }
    }
}