using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Statements;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA
{
    public static class LuaCompiler
    {
        public static LuaFunction Compile(string source, string module = "main")
        {
            LuaLexer lexer = new LuaLexer(source);
            lexer.Next();


            var builder = new FunctionBuilder("main");

            // The main chunk is a vararg function (per Lua semantics), so top-level
            // '...' compiles and expands to zero values.
            builder.HasVarargs = true;

            var chunk = new RootStatement(lexer);

            chunk.Compile(builder);

            return builder.Finish();
        }
    }
}
