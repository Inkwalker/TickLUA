using TickLUA.VM;
using TickLUA.VM.Objects;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class IndexExpression : LValueExpression
    {
        private Expression variable;
        private Expression index;

        public IndexExpression(Expression variable, string index)
        {
            this.variable = variable;
            this.index = new LiteralExpression(index, variable.SourceRange);

            SourceRange = variable.SourceRange;
        }

        public IndexExpression(Expression variable, string index, SourceRange source_range)
            : this(variable, index)
        {
            SourceRange = source_range;
        }

        public IndexExpression(Expression variable, Expression index)
        {
            this.variable = variable;
            this.index = index;

            SourceRange = new SourceRange(variable.SourceRange.from, index.SourceRange.to);
        }

        /// <summary>
        /// Access to a global variable, translated to _ENV.index per Lua 5.2+ semantics.
        /// </summary>
        public static IndexExpression Env(string index, SourceRange source_range)
        {
            var variable = new SymbolExpression(LuaObject.ENV, source_range);
            return new IndexExpression(variable, index, source_range);
        }

        public override void CompileRead(FunctionBuilder builder, RegisterContext target_register)
        {
            var context_table = variable.CompileReadAuto(builder);
            var context_key   = index.CompileReadAuto(builder);

            builder.AddInstruction(Instruction.GET_TABLE(target_register.index, context_table.index, context_key.index), (ushort)SourceRange.from.line);
            
            builder.FreeRegisters(context_key);
            builder.FreeRegisters(context_table);
        }

        public override void CompileWrite(FunctionBuilder builder, RegisterContext value_register)
        {
            var context_table = variable.CompileReadAuto(builder);
            var context_key   = index.CompileReadAuto(builder);

            builder.AddInstruction(Instruction.SET_TABLE(context_table.index, context_key.index, value_register.index), (ushort)SourceRange.from.line);

            builder.FreeRegisters(context_key);
            builder.FreeRegisters(context_table);
        }

        public override byte PreallocateRegister(FunctionBuilder builder)
        {
            throw new CompilationException("Cannot preallocate register for index expression", SourceRange.from);
        }
    }
}
