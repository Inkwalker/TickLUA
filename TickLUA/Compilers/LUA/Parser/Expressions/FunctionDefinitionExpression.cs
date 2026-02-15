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
        private BlockStatement body;

        public FunctionDefinitionExpression(IEnumerable<string> parameters, BlockStatement body, SourceRange range)
        {
            this.body = body;
            this.parameters = new List<string>(parameters);

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
                AssertToken(lexer.Current, TokenType.Name);
                parameters.Add(lexer.Current.Content);
                lexer.Next();

                if (lexer.Current.Type == TokenType.Coma) lexer.Next();
            }
            lexer.Next();

            body = new BlockStatement(lexer);

            AssertTokenNext(lexer, TokenType.End);

            var end_pos = lexer.Current.Position;
            SourceRange = new SourceRange(start_pos, end_pos);
        }

        public override void CompileRead(FunctionBuilder builder, byte reg_result)
        {
            var nested_builder = builder.CreateNestedFunction(out int func_index);

            foreach (var p in parameters)
            {
                nested_builder.AllocateVariable(p);
            }

            //Compile function body
            body.Compile(nested_builder);

            //Return. This section will execute only if there is no return statements before.
            nested_builder.AddInstruction(Instruction.RETURN(0, 0), (ushort)SourceRange.to.line);

            builder.AddInstruction(Instruction.CLOSURE(reg_result, (ushort)func_index), (ushort)SourceRange.from.line);
        }
    }
}
