using TickLUA.Compilers.LUA.Parser.Expressions;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class FunctionCallStatement: Statement
    {
        private FunctionCallExpression expr;

        public FunctionCallStatement(FunctionCallExpression expr)
        {
            this.expr = expr;
        }

        public override void Compile(FunctionBuilder builder)
        {
            var context_func = expr.CompileReadAuto(builder);
            builder.FreeRegisters(context_func);
        }
    }
}
