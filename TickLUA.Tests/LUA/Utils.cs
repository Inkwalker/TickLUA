using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;

namespace TickLUA_Tests.LUA
{
    internal static class Utils
    {
        public static TickVM Run(string code, int tick_limit)
        {
            var luaFunction = LuaCompiler.Compile(code);
            
            return Run(luaFunction, tick_limit);
        }

        public static TickVM Run(LuaFunction func, int tick_limit)
        {
            var vm = new TickVM(func);

            int ticks = 0;
            while (!vm.IsFinished)
            {
                vm.Tick();
                ticks++;

                if (ticks > tick_limit)
                {
                    Assert.Fail($"VM did not finish execution within {tick_limit} ticks.");
                }
            }

            return vm;
        }

        public static void AssertIntegerResult(TickVM vm, int expected_value, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length > result_index);
            Assert.IsInstanceOf<NumberObject>(vm.ExecutionResult[result_index]);
            var answer = (NumberObject)vm.ExecutionResult[result_index];
            Assert.That(answer.Value, Is.EqualTo(expected_value));
        }

        public static void AssertFloatResult(TickVM vm, float expected_value, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length > result_index);
            Assert.IsInstanceOf<NumberObject>(vm.ExecutionResult[result_index]);
            var answer = (NumberObject)vm.ExecutionResult[result_index];

            Assert.That(answer.Value, Is.EqualTo(expected_value));
        }

        public static void AssertBoolResult(TickVM vm, bool expected_value, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length > result_index);
            Assert.IsInstanceOf<BooleanObject>(vm.ExecutionResult[result_index]);
            var answer = (BooleanObject)vm.ExecutionResult[result_index];

            Assert.True((bool)answer == expected_value);
        }

        public static void AssertNilResult(TickVM vm, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length > result_index);
            Assert.IsInstanceOf<NilObject>(vm.ExecutionResult[result_index]);
        }
    }
}
