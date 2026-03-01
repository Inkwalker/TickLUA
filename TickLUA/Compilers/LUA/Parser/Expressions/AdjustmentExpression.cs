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

        public override void CompileRead(FunctionBuilder builder, RegisterContext target_register)
        {
            expr.CompileRead(builder, target_register);
        }
    }
}
