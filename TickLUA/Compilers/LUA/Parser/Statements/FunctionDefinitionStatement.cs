using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class FunctionDefinitionStatement : Statement
    {
        private LValueExpression variable;
        private List<string> parameters;
        private BlockStatement body;
        private bool local;

        public string FunctionName { get; private set; }

        public FunctionDefinitionStatement(bool local, LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;

            parameters = new List<string>();
            this.local = local;

            AssertTokenNext(lexer, TokenType.Function);

            var add_self = ParseVar(lexer);
            if (add_self) parameters.Add("self");

            ParseParams(lexer);

            body = new BlockStatement(lexer);

            var end_pos = lexer.Current.Position;
            SourceRange = new SourceRange(start_pos, end_pos);

            AssertTokenNext(lexer, TokenType.End);
        }

        private bool ParseVar(LuaLexer lexer)
        {
            var add_self = false;
            var chain = new List<string>();
            bool stop = false;
            while (!stop)
            {
                AssertToken(lexer.Current, TokenType.Name);
                chain.Add(lexer.Current.Content);
                lexer.Next();

                AssertToken(lexer.Current, TokenType.Colon, TokenType.Dot, TokenType.BRK_ROUND_Left);

                switch (lexer.Current.Type)
                {
                    case TokenType.BRK_ROUND_Left: stop = true; break;
                    case TokenType.Dot: lexer.Next(); break;
                    case TokenType.Colon:
                        {
                            add_self = true;
                            lexer.Next();
                            AssertToken(lexer.Current, TokenType.Name);
                            chain.Add(lexer.Current.Content);
                            lexer.Next();
                            stop = true;
                        }
                        break;
                }
            }

            // TODO: source range

            variable = new SymbolExpression(chain[0]);

            if (chain.Count > 1)
            {
                for (int i = 1; i < chain.Count; i++)
                {
                    variable = new IndexExpression(variable, chain[i]);
                }
            }

            //set debug name
            FunctionName = string.Join(".", chain);
            return add_self;
        }

        private void ParseParams(LuaLexer lexer)
        {
            AssertTokenNext(lexer, TokenType.BRK_ROUND_Left);

            while (lexer.Current.Type != TokenType.BRK_ROUND_Right)
            {
                AssertToken(lexer.Current, TokenType.Name);
                parameters.Add(lexer.Current.Content);
                lexer.Next();

                if (lexer.Current.Type == TokenType.Coma) lexer.Next();
            }
            lexer.Next();
        }

        public override void Compile(FunctionBuilder builder)
        {
            var expr = new FunctionDefinitionExpression(parameters, body, SourceRange);
            expr.FunctionName = FunctionName;

            if (local)
                variable.PreallocateRegister(builder);

            var context_temp = builder.AllocateRegistersContext(1);

            expr.CompileRead(builder, context_temp);
            variable.CompileWrite(builder, context_temp);

            builder.FreeRegisters(context_temp);
        }
    }
}
