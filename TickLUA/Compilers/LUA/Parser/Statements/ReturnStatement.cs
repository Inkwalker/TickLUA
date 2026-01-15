using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class ReturnStatement : Statement
    {
        private List<Expression> values;

        private int line;

        public ReturnStatement(LuaLexer lexer)
        {
            line = lexer.Current.Line;
            AssertTokenNext(lexer, TokenType.Return);

            values = new List<Expression>();
            while (!lexer.Current.IsEndOfBlock() && lexer.Current.Type != TokenType.Semicolon)
            {
                var e = Expression.Create(lexer);

                values.Add(e);

                if (lexer.Current.Type == TokenType.Coma) lexer.Next();
            }
        }

        public override void Compile(FunctionBuilder builder)
        {
            //if (!context.Symbols.IsInsideFunction())
                //throw new CompilationException($"Return statement outside function at line {line}");

            //if (values.Count > 0)
            //{
            //    for (int i = 0; i < values.Count; i++)
            //    {
            //        values[i].Compile(context, code);
            //    }

            //    code += Instruction.MakeTuple(values.Count);
            //}
            //else if (values.Count == 1)
            byte reg_result = values[0].CompileRead(builder);
            //else
            //    code += Instruction.MakeNil();

            builder.AddInstruction(Instruction.RETURN(reg_result, 1));
        }
    }
}
