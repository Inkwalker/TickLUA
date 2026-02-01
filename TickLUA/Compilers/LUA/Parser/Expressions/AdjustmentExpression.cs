namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    internal class AdjustmentExpression : Expression
    {
        private Expression expr;

        public AdjustmentExpression(Expression expr)
        {
            this.expr = expr;

            SourceRange = expr.SourceRange;
        }

        public override void CompileRead(FunctionBuilder builder, byte reg_result)
        {
            expr.CompileRead(builder, reg_result);
        }
    }
}
