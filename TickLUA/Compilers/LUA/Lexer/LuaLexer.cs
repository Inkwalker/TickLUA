using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer.Recognizers;

namespace TickLUA.Compilers.LUA.Lexer
{
    internal class LuaLexer
    {
        private SourceCode source;

        public Token Current { get; private set; }

        public LuaLexer(string source)
        {
            this.source = new SourceCode(source);
        }

        public IEnumerable<Token> GetTokens()
        {
            Token token;

            do
            {
                token = ReadNextToken();
                yield return token;
            }
            while (token != null && token.Type != TokenType.EOF);
        }

        public Token Peek() => ReadNextToken(true);
        public void Next() => Current = ReadNextToken(false);

        private Token ReadNextToken(bool peek = false)
        {
            int read_start = source.Position;

            while (!source.EoF)
            {
                int start_pos = source.Position;

                foreach (var recognizer in recognizers)
                {
                    int pos = source.Position;

                    if (recognizer.Read(source, out Token token))
                        if (token != null)
                        {
                            if (peek) source.Revert(read_start);
                            return token;
                        }
                        else
                            break;
                    else
                        source.Revert(pos);

                    if (source.EoF) break;
                }

                if (source.Position == start_pos)
                {
                    throw new System.Exception($"Unknown character at: ln{source.Line}, ch{source.Column}");
                }
            }

            if (peek) source.Revert(read_start);
            return new Token(TokenType.EOF) { Content = "<eof>", Line = source.Line };
        }

        private TokenRecognizer[] recognizers =
        {
            new CommentRecognizer(),
            new WhiteSpaceRecognizer(),
            new SymbolRecognizer("+=", TokenType.OP_Assignment_Add),
            new SymbolRecognizer("-=", TokenType.OP_Assignment_Sub),
            new SymbolRecognizer("*=", TokenType.OP_Assignment_Mul),
            new SymbolRecognizer("//=", TokenType.OP_Assignment_iDiv),
            new SymbolRecognizer("/=", TokenType.OP_Assignment_Div),
            new SymbolRecognizer("%=", TokenType.OP_Assignment_Mod),
            new SymbolRecognizer("^=", TokenType.OP_Assignment_Pow),
            new SymbolRecognizer("==", TokenType.OP_Equals),
            new SymbolRecognizer("!=", TokenType.OP_NotEquals),
            new SymbolRecognizer("~=", TokenType.OP_NotEquals),
            new SymbolRecognizer("<=", TokenType.OP_LessEq),
            new SymbolRecognizer(">=", TokenType.OP_GreaterEq),
            new SymbolRecognizer("..", TokenType.OP_Concat),
            new SymbolRecognizer(",", TokenType.Coma),
            new SymbolRecognizer(".", TokenType.Dot),
            new SymbolRecognizer(";", TokenType.Semicolon),
            new SymbolRecognizer(":", TokenType.Colon),
            new SymbolRecognizer("=", TokenType.OP_Assignment),
            new SymbolRecognizer("+", TokenType.OP_Add),
            new SymbolRecognizer("-", TokenType.OP_Sub),
            new SymbolRecognizer("*", TokenType.OP_Mul),
            new SymbolRecognizer("%", TokenType.OP_Mod),
            new SymbolRecognizer("^", TokenType.OP_Pow),
            new SymbolRecognizer("#", TokenType.OP_Len),
            new SymbolRecognizer("<", TokenType.OP_LessThan),
            new SymbolRecognizer(">", TokenType.OP_GreaterThan),
            new SymbolRecognizer("//", TokenType.OP_iDiv),
            new SymbolRecognizer("/", TokenType.OP_Div),
            new SymbolRecognizer("(", TokenType.BRK_ROUND_Left),
            new SymbolRecognizer(")", TokenType.BRK_ROUND_Right),
            new SymbolRecognizer("[", TokenType.BRK_SQR_Left),
            new SymbolRecognizer("]", TokenType.BRK_SQR_Right),
            new SymbolRecognizer("{", TokenType.BRK_CUR_Left),
            new SymbolRecognizer("}", TokenType.BRK_CUR_Right),
            new KeywordRecognizer("true", TokenType.True),
            new KeywordRecognizer("false", TokenType.False),
            new KeywordRecognizer("nil", TokenType.Nil),
            new KeywordRecognizer("not", TokenType.Not),
            new KeywordRecognizer("and", TokenType.OP_LogicAnd),
            new KeywordRecognizer("or", TokenType.OP_LogicOr),
            new KeywordRecognizer("if", TokenType.If),
            new KeywordRecognizer("then", TokenType.Then),
            new KeywordRecognizer("else", TokenType.Else),
            new KeywordRecognizer("elseif", TokenType.Elseif),
            new KeywordRecognizer("while", TokenType.While),
            new KeywordRecognizer("repeat", TokenType.Repeat),
            new KeywordRecognizer("until", TokenType.Until),
            new KeywordRecognizer("break", TokenType.Break),
            new KeywordRecognizer("continue", TokenType.Continue),
            new KeywordRecognizer("end", TokenType.End),
            new KeywordRecognizer("for", TokenType.For),
            new KeywordRecognizer("in", TokenType.In),
            new KeywordRecognizer("do", TokenType.Do),
            new KeywordRecognizer("function", TokenType.Function),
            new KeywordRecognizer("class", TokenType.Class),
            new KeywordRecognizer("import", TokenType.Import),
            new KeywordRecognizer("as", TokenType.As),
            new KeywordRecognizer("return", TokenType.Return),
            new KeywordRecognizer("local", TokenType.Local),
            new NumberRecognizer(),
            new StringRecognizer(),
            new NameRecoginzer()
        };
    }
}
