using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Statements;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class FunctionDefinitionExpression : Expression
    {
        public string FunctionName { get; set; } = "func";

        private List<string> parameters;
        private CompoundStatement body;
        private bool is_varargs;

        public FunctionDefinitionExpression(IEnumerable<string> parameters, CompoundStatement body, SourceRange range, bool isVarargs = false)
        {
            this.body = body;
            this.parameters = new List<string>(parameters);
            is_varargs = isVarargs;

            SourceRange = range;
        }

        public FunctionDefinitionExpression(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;

            AssertTokenNext(lexer, TokenType.Function);
            AssertTokenNext(lexer, TokenType.BRK_ROUND_Left);

            parameters = new List<string>();
            while (lexer.Current.Type != TokenType.BRK_ROUND_Right)
            {
                if (lexer.Current.Type == TokenType.VarArgs)
                {
                    is_varargs = true;
                    lexer.Next();
                    if (lexer.Current.Type != TokenType.BRK_ROUND_Right)
                        throw new CompilationException("'...' must be the last parameter", lexer.Current.Position);
                    break;
                }

                AssertToken(lexer.Current, TokenType.Name);
                parameters.Add(lexer.Current.Content);
                lexer.Next();

                if (lexer.Current.Type == TokenType.Coma) lexer.Next();
            }
            lexer.Next();

            body = new CompoundStatement(lexer);

            var end_pos = lexer.Current.Position;
            SourceRange = new SourceRange(start_pos, end_pos);

            AssertTokenNext(lexer, TokenType.End);
        }

        public override void CompileRead(FunctionBuilder builder, RegisterContext target_register)
        {
            var nested_builder = builder.CreateNestedFunction(FunctionName, out int func_index);

            nested_builder.HasVarargs = is_varargs;
            nested_builder.ParameterCount = parameters.Count;

            //Open a new block to ensure that parameters are scoped correctly.
            nested_builder.BlockStart();

            foreach (var p in parameters)
            {
                nested_builder.AllocateVariable(p);
            }

            //Compile function body
            body.Compile(nested_builder);

            //Return. This section will execute only if there is no return statements before.
            nested_builder.AddInstruction(Instruction.RETURN(0, 0), (ushort)SourceRange.to.line);

            nested_builder.BlockEnd();

            builder.AddInstruction(Instruction.CLOSURE(target_register.index, (ushort)func_index), (ushort)SourceRange.from.line);
        }
    }
}
