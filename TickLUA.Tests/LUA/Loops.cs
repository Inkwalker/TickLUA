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
        public void Break_WhileLoop()
        {
            string source = @"
                local i = 0
                while true do
                    i = i + 1
                    if i == 5 then
                        break
                    end
                end
                return i";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 5, 0);
        }

        [Test]
        public void Break_NumericForLoop()
        {
            string source = @"
                local sum = 0
                for i = 1, 10 do
                    if i > 3 then
                        break
                    end
                    sum = sum + i
                end
                return sum";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 6, 0);
        }

        [Test]
        public void GenericForIn_StatelessIterator()
        {
            string source = @"
                local function iter(max, i)
                    i = i + 1
                    if i <= max then
                        return i
                    end
                end

                local sum = 0
                for i in iter, 3, 0 do
                    sum = sum + i
                end
                return sum";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 6, 0);
        }

        [Test]
        public void GenericForIn_Ipairs()
        {
            string source = @"
                local t = {10, 20, 30}
                local sum = 0
                for i, v in ipairs(t) do
                    sum = sum + v
                end
                return sum";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 60, 0);
        }

        [Test]
        public void NumericForLoop_EmptyRange()
        {
            // When the start already passes the limit for a positive step,
            // the body must not execute at all.
            string source = @"
                local a = 5

                for i = 1, 0 do
                    a = 99
                end

                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5, 0);
        }

        [Test]
        public void NumericForLoop_FloatStep()
        {
            // A fractional step is allowed: 1, 1.5, 2 -> three iterations.
            string source = @"
                local a = 0

                for i = 1, 2, 0.5 do
                    a = a + 1
                end

                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 3, 0);
        }

        [Test]
        public void RepeatLoop_UntilSeesBodyLocals()
        {
            string source = @"
                local n = 0

                repeat
                    n = n + 1
                    local done = n >= 3
                until done

                return n";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 3, 0);
        }

        [Test]
        public void WhileLoop_FalseNeverRuns()
        {
            // A while loop whose condition starts false never enters the body.
            string source = @"
                local a = 5

                while false do
                    a = 99
                end

                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5, 0);
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
