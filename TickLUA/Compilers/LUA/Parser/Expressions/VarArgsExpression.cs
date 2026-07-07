using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    /// <summary>
    /// The '...' expression. Reads the extra arguments of the enclosing vararg function.
    /// Expands to all values in trailing multi-value positions, truncates to the
    /// requested count (nil-padded) elsewhere.
    /// </summary>
    internal class VarArgsExpression : Expression
    {
        public override bool IsMultiValue => true;

        public VarArgsExpression(LuaLexer lexer)
        {
            var token = lexer.Current;
            SourceRange = new SourceRange(token.Position, token.Position);
            lexer.Next();
        }

        public override void CompileRead(FunctionBuilder builder, RegisterContext target_register)
        {
            if (!builder.HasVarargs)
                throw new CompilationException("cannot use '...' outside a vararg function", SourceRange.from);

            builder.AddInstruction(Instruction.VARARG(target_register.index, target_register.count), (ushort)SourceRange.from.line);
        }
    }
}
