using System;
using System.Diagnostics;
using TickLUA.Compilers.LUA;
using TickLUA_Tests.Instructions;

namespace TickLUA_Tests.LUA
{
    internal class Loops
    {
        [Test]
        public void WhileLoop()
        {
            string source = @"
                local a = 0

                while a < 10 do
                    a = a + 1
                end

                return a";

            var vm = Utils.Run(source, 150);
            Utils.AssertIntegerResult(vm, 10, 0);
        }

        [Test]
        public void RepeatLoop()
        {
            string source = @"
                local a = 0

                repeat
                    a = a + 1
                until a > 10

                return a";

            var vm = Utils.Run(source, 150);
            Utils.AssertIntegerResult(vm, 11, 0);
        }

        [Test]
        public void NumericForLoop_Positive()
        {
            string source = @"
                local a = 0

                for i = 1, 10, 1 do
                    a = a + i
                end

                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 55, 0);
        }

        [Test]
        public void NumericForLoop_Negative()
        {
            string source = @"
                local a = 0

                for i = 10, 1, -1 do
                    a = a + i
                end

                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 55, 0);
        }

        [Test]
        public void NumericForLoop_Default()
        {
            string source = @"
                local a = 0

                for i = 1, 10 do
                    a = a + i
                end

                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 55, 0);
        }

        [Test]
        public void NumericForLoop_Speedtest()
        {
            string source = @"
                local a = 0

                for i = 1, 100000 do
                    a = i
                end

                return a";

            var luaFunction = LuaCompiler.Compile(source);

            var vm = new TickVM(luaFunction);

            var sw = Stopwatch.StartNew();

            while (!vm.IsFinished)
            {
                vm.Tick();
            }

            sw.Stop();

            Console.WriteLine("Elapsed time: " + sw.ElapsedMilliseconds.ToString() + "ms");

            // non boxed : 22 ms
            // boxes : 21 ms
            
        }
    }
}
