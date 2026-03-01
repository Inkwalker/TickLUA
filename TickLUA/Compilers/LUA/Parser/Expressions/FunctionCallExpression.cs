using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class FunctionCallExpression : Expression
    {
        private Expression function_expr;
        private List<Expression> args;

        public FunctionCallExpression(Expression obj, IEnumerable<Expression> args)
        {
            function_expr = obj;
            this.args = new List<Expression>(args);
        }

        public FunctionCallExpression(Expression obj, LuaLexer lexer)
        {
            var start_pos = lexer.Current;

            function_expr = obj;

            args = new List<Expression>();

            AssertTokenNext(lexer, TokenType.BRK_ROUND_Left);

            while (lexer.Current.Type != TokenType.BRK_ROUND_Right)
            {
                var arg = Expression.Create(lexer);

                args.Add(arg);

                if (lexer.Current.Type == TokenType.Coma) lexer.Next();
            }

            var end_pos = lexer.Current;
            SourceRange = new SourceRange(start_pos.Position, end_pos.Position);

            lexer.Next();
        }

        public override void CompileRead(FunctionBuilder builder, RegisterContext func_register)
        {
            function_expr.CompileRead(builder, func_register);

            int args_start = builder.AllocateRegisters(args.Count);
            for (int i = 0; i < args.Count; i++)
            {
                var context = new RegisterContext { index = (byte)(args_start + i), count = 1 };
                args[i].CompileRead(builder, context);
            }

            builder.AddInstruction(Instruction.CALL(func_register.index, (byte)args.Count, func_register.count), (ushort)SourceRange.from.line);

            builder.FreeRegisters(args.Count);
        }
    }
}
