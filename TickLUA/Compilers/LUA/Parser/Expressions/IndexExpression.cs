using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class IndexExpression : LValueExpression
    {
        private Expression variable;
        private Expression index;

        public IndexExpression(Expression variable, string index)
        {
            this.variable = variable;
            this.index = new LiteralExpression(index);
        }

        public IndexExpression(Expression variable, Expression index)
        {
            this.variable = variable;
            this.index = index;

            SourceRange = new SourceRange(variable.SourceRange.from, index.SourceRange.to);
        }

        //public static IndexExpression Env(string index)
        //{
        //    var variable = new SymbolExpression(LuaObject.ENV);
        //    return new IndexExpression(variable, index);
        //}

        public override void CompileRead(FunctionBuilder builder, byte reg_result)
        {
            byte reg_table = builder.AllocateRegisters(1);
            byte reg_key   = builder.AllocateRegisters(1);
            variable.CompileRead(builder, reg_table);
            index.CompileRead(builder, reg_key);

            builder.AddInstruction(Instruction.GET_TABLE(reg_result, reg_table, reg_key), (ushort)SourceRange.from.line);
            
            builder.DeallocateRegisters(reg_table);
            builder.DeallocateRegisters(reg_key);
        }

        public override void CompileWrite(FunctionBuilder builder, byte reg_value)
        {
            byte reg_table = builder.AllocateRegisters(1);
            byte reg_key = builder.AllocateRegisters(1);
            variable.CompileRead(builder, reg_table);
            index.CompileRead(builder, reg_key);

            builder.AddInstruction(Instruction.SET_TABLE(reg_table, reg_key, reg_value), (ushort)SourceRange.from.line);

            builder.DeallocateRegisters(reg_table);
            builder.DeallocateRegisters(reg_key);
        }
    }
}
