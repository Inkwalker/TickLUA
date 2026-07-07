using TickLUA.VM.Objects;

namespace TickLUA_Tests.LUA
{
    // Host-supplied run arguments are published in the global 'arg' table
    // (indices 1..n, per the Lua spec) and also arrive as the main chunk's
    // varargs.
    internal class Args
    {
        [Test]
        public void Arg_TableHoldsRunArguments()
        {
            string source = @"return arg[1] + arg[2]";

            var vm = Utils.Run(source, 100, LuaObject.From(2), LuaObject.From(3));
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Arg_Length()
        {
            string source = @"return #arg";

            var vm = Utils.Run(source, 100, LuaObject.From(1), LuaObject.From(2), LuaObject.From(3));
            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void Arg_EmptyWithoutRunArguments()
        {
            string source = @"return #arg";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 0);
        }

        [Test]
        public void Arg_AlsoAvailableAsVarargs()
        {
            // The same run arguments arrive both as '...' and in the arg table.
            string source = @"
                local a, b = ...
                return a + b, arg[1], arg[2]";

            var vm = Utils.Run(source, 100, LuaObject.From(2), LuaObject.From(3));
            Utils.AssertIntegerResult(vm, 5, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }
    }
}
