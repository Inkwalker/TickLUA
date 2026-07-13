using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;
using TickLUA.VM.Tools;

namespace TickLUA_Tests
{
    internal static class Utils
    {
        public static TickVM Run(string code, int tick_limit, params LuaObject[] vm_args)
        {
            var luaFunction = LuaCompiler.Compile(code);

            BytecodePrinter.ConsoleWrite(luaFunction, true);

            return Run(luaFunction, tick_limit, vm_args);
        }

        public static TickVM Run(string code, int tick_limit, ModuleReaderDelegate module_reader, params LuaObject[] vm_args)
        {
            var luaFunction = LuaCompiler.Compile(code);

            BytecodePrinter.ConsoleWrite(luaFunction, true);

            var vm = new TickVM(luaFunction, vm_args);
            vm.ModuleReader = module_reader;

            return Run(vm, tick_limit);
        }

        public static TickVM Run(LuaFunction func, int tick_limit, params LuaObject[] vm_args)
        {
            return Run(new TickVM(func, vm_args), tick_limit);
        }

        public static TickVM Run(TickVM vm, int tick_limit)
        {
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
            Assert.That(vm.ExecutionResult.Length, Is.GreaterThan(result_index), "Not enough results");
            Assert.IsInstanceOf<NumberObject>(vm.ExecutionResult[result_index]);
            var answer = (NumberObject)vm.ExecutionResult[result_index];
            Assert.That(answer.Value, Is.EqualTo(expected_value));
        }

        public static void AssertFloatResult(TickVM vm, float expected_value, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.That(vm.ExecutionResult.Length, Is.GreaterThan(result_index), "Not enough results");
            Assert.IsInstanceOf<NumberObject>(vm.ExecutionResult[result_index]);
            var answer = (NumberObject)vm.ExecutionResult[result_index];

            Assert.That(answer.Value, Is.EqualTo(expected_value));
        }

        public static void AssertBoolResult(TickVM vm, bool expected_value, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.That(vm.ExecutionResult.Length, Is.GreaterThan(result_index), "Not enough results");
            Assert.IsInstanceOf<BooleanObject>(vm.ExecutionResult[result_index]);
            var answer = (BooleanObject)vm.ExecutionResult[result_index];

            Assert.True((bool)answer == expected_value);
        }

        public static void AssertNilResult(TickVM vm, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.That(vm.ExecutionResult.Length, Is.GreaterThan(result_index), "Not enough results");
            Assert.IsInstanceOf<NilObject>(vm.ExecutionResult[result_index]);
        }

        public static void AssertStringResult(TickVM vm, string expected_value, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.That(vm.ExecutionResult.Length, Is.GreaterThan(result_index), "Not enough results");
            Assert.IsInstanceOf<StringObject>(vm.ExecutionResult[result_index]);
            var answer = (StringObject)vm.ExecutionResult[result_index];

            Assert.That(answer.Value , Is.EqualTo(expected_value));
        }

        public static TableObject AssertTableResult(TickVM vm, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.That(vm.ExecutionResult.Length, Is.GreaterThan(result_index), "Not enough results");
            Assert.IsInstanceOf<TableObject>(vm.ExecutionResult[result_index]);

            var table = (TableObject)vm.ExecutionResult[result_index];

            return table;
        }

        public static void AssertTableResult(TickVM vm, LuaObject key, LuaObject value, int result_index = 0)
        {
            var table = AssertTableResult(vm, result_index);

            Assert.IsTrue(table.Contains(key));
            Assert.That(table[key], Is.EqualTo(value));
        }

        public static ClosureObject AssertClosureResult(TickVM vm, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.That(vm.ExecutionResult.Length, Is.GreaterThan(result_index), "Not enough results");
            Assert.IsInstanceOf<ClosureObject>(vm.ExecutionResult[result_index]);

            var closure = (ClosureObject)vm.ExecutionResult[result_index];

            return closure;
        }
    }
}
