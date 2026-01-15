namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class AdjustmentExpression : Expression
    {
        private Expression expr;

        public AdjustmentExpression(Expression expr)
        {
            this.expr = expr;
        }

        public override byte CompileRead(FunctionBuilder builder)
        {
            ResultRegister = expr.CompileRead(builder);

            return (byte)ResultRegister;
        }

        public override void ReleaseRegisters(FunctionBuilder builder)
        {
            ResultRegister = -1;
            expr.ReleaseRegisters(builder);
        }
    }
}
