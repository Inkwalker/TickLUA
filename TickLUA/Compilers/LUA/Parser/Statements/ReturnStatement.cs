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

            // return f(...) / return obj:m(...) — proper tail call: the caller
            // replaces this frame. The RETURN after the TAILCALL only runs for
            // callers that cannot replace the frame (VM-aware natives, __call).
            // A parenthesized call parses as AdjustmentExpression, so it stays
            // on the truncating general path, per Lua semantics.
            if (values.Count == 1 && values[0] is FunctionCallExpression tail_call)
            {
                byte reg = builder.AllocateRegisters(1);
                tail_call.CompileTailCall(builder, reg);
                builder.AddInstruction(Instruction.RETURN(reg, -1), line);
                builder.FreeRegisters(1);
                return;
            }

            // A multi-value expression (function call or '...') in the last position
            // expands to all of its values.
            // Note: a parenthesized one parses as AdjustmentExpression and stays truncated to one value.
            bool is_multi_return = values[values.Count - 1].IsMultiValue;

            // Allocate the return slots one at a time rather than as a single block up
            // front. A non-trailing call places its arguments in the registers directly
            // above its func slot (FunctionCallExpression allocates them from the current
            // register top, then frees them), so its func slot must be the current top
            // when it is compiled. Pre-reserving every slot would push those argument
            // registers past the remaining return slots and CALL would read the wrong
            // registers. Because the argument block is freed after each call, the return
            // slots still come out contiguous for the RETURN below.
            byte reg_start = 0;

            for (int i = 0; i < values.Count; i++)
            {
                byte reg = builder.AllocateRegisters(1);
                if (i == 0) reg_start = reg;

                bool expand = is_multi_return && i == values.Count - 1;
                var context = new Expression.RegisterContext(reg, expand ? -1 : 1);
                values[i].CompileRead(builder, context);
            }

            builder.AddInstruction(Instruction.RETURN(reg_start, is_multi_return ? -1 : values.Count), line);
            builder.FreeRegisters(values.Count);
        }
    }
}
