using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;

namespace TickLUA_Tests
{
    internal class TestsLUA
    {
        [Test]
        public void Assignment()
        {
            string source = @"
                local a = 5
                local b = 10
                local c = a + b
                return c";

            var luaFunction = LuaCompiler.Compile(source);
            var vm = new TickVM(luaFunction);

            int ticks = 0;
            while (!vm.IsFinished)
            {
                vm.Tick();
                ticks++;

                if (ticks > 100)
                {
                    Assert.Fail("VM did not finish execution within 100 ticks.");
                }
            }

            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 1);
            Assert.IsInstanceOf<IntegerObject>(vm.ExecutionResult[0]);

            var answer = vm.ExecutionResult[0] as IntegerObject;

            Assert.AreEqual(answer.Value, 15);
        }

        [Test]
        public void AssignmentAdd()
        {
            string source = @"
                local a = 5
                local b = 10
                b += a
                return b";

            var luaFunction = LuaCompiler.Compile(source);
            var vm = new TickVM(luaFunction);

            int ticks = 0;
            while (!vm.IsFinished)
            {
                vm.Tick();
                ticks++;

                if (ticks > 100)
                {
                    Assert.Fail("VM did not finish execution within 100 ticks.");
                }
            }

            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 1);
            Assert.IsInstanceOf<IntegerObject>(vm.ExecutionResult[0]);

            var answer = vm.ExecutionResult[0] as IntegerObject;

            Assert.AreEqual(answer.Value, 15);
        }
    }
}
