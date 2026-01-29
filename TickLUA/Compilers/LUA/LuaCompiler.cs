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


            var builder = new FunctionBuilder();

            var chunk = new BlockStatement(lexer);

            chunk.Compile(builder);

            return builder.Finish();
        }
    }
}
