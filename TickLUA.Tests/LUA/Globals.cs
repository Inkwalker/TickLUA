using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;

namespace TickLUA_Tests.LUA
{
    internal class Globals
    {
        [Test]
        public void Global_AssignAndRead()
        {
            string source = @"
                x = 10
                return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 10);
        }

        [Test]
        public void Global_UnassignedIsNil()
        {
            // Reading a global that was never assigned yields nil, not an error.
            string source = @"return undefined_global";

            var vm = Utils.Run(source, 100);
            Utils.AssertNilResult(vm);
        }

        [Test]
        public void Global_Function()
        {
            string source = @"
                function add(a, b)
                    return a + b
                end
                return add(2, 3)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Global_VisibleAcrossScopes()
        {
            string source = @"
                counter = 0
                local function bump()
                    counter = counter + 1
                end
                bump()
                bump()
                return counter";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2);
        }

        [Test]
        public void Global_InNestedFunctions()
        {
            string source = @"
                x = 1
                local function outer()
                    local function inner()
                        x = x + 10
                    end
                    inner()
                end
                outer()
                return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 11);
        }

        [Test]
        public void Env_ExplicitWriteReadAsGlobal()
        {
            string source = @"
                _ENV.x = 5
                return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Env_GlobalReadableThroughEnv()
        {
            string source = @"
                x = 5
                return _ENV.x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Env_IsTheGlobalsTable()
        {
            string source = @"return _ENV";

            var vm = Utils.Run(source, 100);
            var table = Utils.AssertTableResult(vm);
            Assert.AreSame(vm.Globals, table);
        }

        [Test]
        public void Global_HostRegistered()
        {
            // Hosts can register globals on vm.Globals before ticking.
            string source = @"return add(2, 3)";

            var func = LuaCompiler.Compile(source);
            var vm = new TickVM(func);
            vm.Globals["add"] = new NativeFunctionObject("add", args =>
            {
                float result = args.CheckNumber(0) + args.CheckNumber(1);
                return new LuaObject[] { new NumberObject(result) };
            });

            int ticks = 0;
            while (!vm.IsFinished)
            {
                vm.Tick();
                if (++ticks > 100) Assert.Fail("VM did not finish execution within 100 ticks.");
            }

            Utils.AssertIntegerResult(vm, 5);
        }
    }
}
