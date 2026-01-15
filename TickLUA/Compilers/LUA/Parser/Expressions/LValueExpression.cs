namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    /// <summary>
    /// Base class for l-value expressions. (write operations)
    /// </summary>
    internal abstract class LValueExpression : Expression
    {
        public abstract void CompileWrite(FunctionBuilder builder, byte reg_value);
    }
}
