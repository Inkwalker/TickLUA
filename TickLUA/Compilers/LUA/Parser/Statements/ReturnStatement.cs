using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class ReturnStatement : Statement
    {
        private List<Expression> values;

        public ReturnStatement(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.Return);

            values = new List<Expression>();
            while (!lexer.Current.IsEndOfBlock() && lexer.Current.Type != TokenType.Semicolon)
            {
                var e = Expression.Create(lexer);

                values.Add(e);

                if (lexer.Current.Type == TokenType.Coma) lexer.Next();
            }
            var end_pos = lexer.Current.Position;

            SourceRange = new SourceRange(start_pos, end_pos);
        }

        public override void Compile(FunctionBuilder builder)
        {
            ushort line = (ushort)SourceRange.from.line;

            if (values.Count == 0)
            {
                // Return nothing
                builder.AddInstruction(Instruction.RETURN(0, 0), line);
                return;
            }

            // A multi-value expression (function call or '...') in the last position
            // expands to all of its values.
            // Note: a parenthesized one parses as AdjustmentExpression and stays truncated to one value.
            bool is_multi_return = values[values.Count - 1].IsMultiValue;

            byte reg_start = builder.AllocateRegisters(values.Count);

            for (int i = 0; i < values.Count; i++)
            {
                byte reg = (byte)(reg_start + i);
                bool expand = is_multi_return && i == values.Count - 1;
                var context = new Expression.RegisterContext(reg, expand ? -1 : 1);
                values[i].CompileRead(builder, context);
            }

            builder.AddInstruction(Instruction.RETURN(reg_start, is_multi_return ? -1 : values.Count), line);
        }
    }
}
