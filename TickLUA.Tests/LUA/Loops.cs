using System.Diagnostics;
using TickLUA.Compilers;
using TickLUA.Compilers.LUA;

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
        public void Break_RepeatLoop()
        {
            string source = @"
                local i = 0
                repeat
                    i = i + 1
                    if i == 4 then
                        break
                    end
                until false
                return i";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 4, 0);
        }

        [Test]
        public void Break_NestedLoops()
        {
            // break only exits the innermost loop
            string source = @"
                local count = 0
                for i = 1, 3 do
                    while true do
                        count = count + 1
                        break
                    end
                end
                return count";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 3, 0);
        }

        [Test]
        public void Break_ClosureCapturesLocal()
        {
            // A body local captured before the break must keep its value
            // after the loop exits early (break-path CLOSE).
            string source = @"
                local f = 0
                local i = 0
                while true do
                    i = i + 1
                    local j = i * 10
                    if i == 2 then
                        f = function()
                            return j
                        end
                        break
                    end
                end
                return f()";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 20, 0);
        }

        [Test]
        public void Break_OutsideLoop_Throws()
        {
            string source = @"
                local a = 1
                break
                return a";

            Assert.Throws<CompilationException>(() => LuaCompiler.Compile(source));
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
        public void GenericForIn_Pairs()
        {
            string source = @"
                local t = {a = 1, b = 2, c = 3}
                local sum = 0
                for k, v in pairs(t) do
                    sum = sum + v
                end
                return sum";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 6, 0);
        }

        [Test]
        public void GenericForIn_Next()
        {
            // next called directly: first pair of a one-element array,
            // then nil after the last key.
            string source = @"
                local t = {42}
                local k, v = next(t)
                local k2 = next(t, k)
                return v, k2";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42, 0);
            Utils.AssertNilResult(vm, 1);
        }

        [Test]
        public void GenericForIn_Break()
        {
            string source = @"
                local t = {5, 6, 7}
                local sum = 0
                for i, v in ipairs(t) do
                    sum = sum + v
                    if i >= 2 then
                        break
                    end
                end
                return sum";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 11, 0);
        }

        [Test]
        public void GenericForIn_MultiVarStatelessIterator()
        {
            string source = @"
                local function iter(max, i)
                    i = i + 1
                    if i <= max then
                        return i, i * 2
                    end
                end

                local sum = 0
                for i, d in iter, 3, 0 do
                    sum = sum + d
                end
                return sum";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 12, 0);
        }

        [Test]
        public void GenericForIn_IpairsStopsAtNilHole()
        {
            // ipairs iterates 1..n until the first nil value.
            string source = @"
                local t = {1, 2, 3}
                t[2] = nil
                local sum = 0
                for i, v in ipairs(t) do
                    sum = sum + v
                end
                return sum";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 1, 0);
        }

        [Test]
        public void GenericForIn_ClosureCapturesFreshLoopVar()
        {
            // Each iteration has fresh loop variables; closures capturing them
            // must keep their own value (exercises the CLOSE path).
            string source = @"
                local t = {10, 20, 30}
                local funcs = {}
                for i, v in ipairs(t) do
                    funcs[i] = function()
                        return v
                    end
                end
                return funcs[1](), funcs[2](), funcs[3]()";

            var vm = Utils.Run(source, 300);
            Utils.AssertIntegerResult(vm, 10, 0);
            Utils.AssertIntegerResult(vm, 20, 1);
            Utils.AssertIntegerResult(vm, 30, 2);
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
        public void RepeatLoop_ClosureCapturesFreshLocal()
        {
            // Each repeat iteration has a fresh body local; closures capturing it must
            // keep their own value, and the until-condition still sees that local.
            string source = @"
                local funcs = {}
                local i = 0
                repeat
                    i = i + 1
                    local j = i
                    funcs[i] = function()
                        return j
                    end
                until i >= 3

                return funcs[1](), funcs[2](), funcs[3]()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
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
