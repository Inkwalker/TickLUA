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

            // The reader has to be installed before the chunk starts, which is
            // exactly what splitting Load out of the constructor allows.
            var vm = new TickVM();
            vm.ModuleReader = module_reader;
            vm.Load(luaFunction, vm_args);

            return Run(vm, tick_limit);
        }

        public static TickVM Run(LuaFunction func, int tick_limit, params LuaObject[] vm_args)
        {
            return Run(Load(func, vm_args), tick_limit);
        }

        /// <summary>Builds a VM and starts <paramref name="func"/> as its main chunk.</summary>
        public static TickVM Load(LuaFunction func, params LuaObject[] vm_args)
        {
            return Load(func, (TickVMOptions?)null, vm_args);
        }

        /// <summary><see cref="Load(LuaFunction, LuaObject[])"/> with VM options.</summary>
        public static TickVM Load(LuaFunction func, TickVMOptions? options, params LuaObject[] vm_args)
        {
            var vm = new TickVM(options);
            vm.Load(func, vm_args);
            return vm;
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
