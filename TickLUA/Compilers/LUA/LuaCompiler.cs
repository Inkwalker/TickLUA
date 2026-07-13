using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Statements;
using TickLUA.VM;
using TickLUA.VM.Objects;

namespace TickLUA.Compilers.LUA
{
    public static class LuaCompiler
    {
        public static LuaFunction Compile(string source, string module = "main")
        {
            LuaLexer lexer = new LuaLexer(source);
            lexer.Next();


            var builder = new FunctionBuilder(module);

            // The main chunk is a vararg function (per Lua semantics), so top-level
            // '...' compiles and expands to zero values.
            builder.HasVarargs = true;

            // _ENV is upvalue #0 of the main chunk (Lua 5.2+ semantics); free names
            // compile as _ENV.name. The VM supplies the cell holding the globals table.
            builder.AddUpvalue(LuaObject.ENV);

            var chunk = new RootStatement(lexer);

            chunk.Compile(builder);

            return builder.Finish();
        }
    }
}
