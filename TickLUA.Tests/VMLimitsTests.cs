using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;

namespace TickLUA_Tests
{
    internal class VMLimitsTests
    {
        // Non-tail recursion: "1 +" keeps every frame alive to depth n.
        private const string RecursiveSource = @"
            local function f(n)
                if n == 0 then return 0 end
                return 1 + f(n - 1)
            end";

        private static TickVM Compile(string source, TickVMOptions options)
        {
            return Utils.Load(LuaCompiler.Compile(source), options);
        }

        [Test]
        public void Default_CallStackUnlimited()
        {
            // No options at all: recursion far deeper than any sane limit finishes.
            string source = RecursiveSource + @"
                return f(500)";

            var vm = Utils.Run(source, 100_000);
            Utils.AssertIntegerResult(vm, 500);
        }

        [Test]
        public void Limit_UnderIsFine()
        {
            string source = RecursiveSource + @"
                return f(5)";

            var vm = Compile(source, new TickVMOptions { MaxCallStackDepth = 10 });
            Utils.Run(vm, 1000);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Limit_ExceededThrows()
        {
            string source = RecursiveSource + @"
                return f(100)";

            var vm = Compile(source, new TickVMOptions { MaxCallStackDepth = 10 });
            var ex = Assert.Throws<RuntimeException>(() =>
            {
                for (int i = 0; i < 100_000 && !vm.IsFinished; i++)
                    vm.Tick();
            });
            Assert.That(ex.Message, Is.EqualTo("stack overflow"));
        }

        [Test]
        public void Limit_PcallCatchesOverflow()
        {
            string source = RecursiveSource + @"
                local ok, err = pcall(f, 100)
                return ok, err";

            var vm = Compile(source, new TickVMOptions { MaxCallStackDepth = 10 });
            Utils.Run(vm, 100_000);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "stack overflow", 1);
        }

        [Test]
        public void Limit_TailCallsDoNotGrowStack()
        {
            // Proper tail calls replace their frame, so depth stays at
            // main + f no matter how many iterations run.
            string source = @"
                local function f(n)
                    if n == 0 then return 'done' end
                    return f(n - 1)
                end
                return f(100)";

            var vm = Compile(source, new TickVMOptions { MaxCallStackDepth = 3 });
            Utils.Run(vm, 100_000);
            Utils.AssertStringResult(vm, "done");
        }

        [Test]
        public void Limit_CoroutineOverflowReportedByResume()
        {
            // The body overflows inside the coroutine; resume reports it as
            // (false, err) and the main coroutine keeps running.
            string source = RecursiveSource + @"
                local co = coroutine.create(function() return f(100) end)
                local ok, err = coroutine.resume(co)
                return ok, err";

            var vm = Compile(source, new TickVMOptions { MaxCallStackDepth = 10 });
            Utils.Run(vm, 100_000);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "stack overflow", 1);
        }

        [Test]
        public void Limit_AppliesPerCoroutine()
        {
            // Main recurses close to the limit, then runs a coroutine that
            // does the same. Each stack is measured on its own, so both fit;
            // a shared budget would overflow.
            string source = @"
                local function work(n)
                    if n == 0 then
                        local co = coroutine.create(function()
                            local function g(m)
                                if m == 0 then return 'ok' end
                                return '' .. g(m - 1)
                            end
                            return g(15)
                        end)
                        local _, res = coroutine.resume(co)
                        return res
                    end
                    return 'x' .. work(n - 1)
                end
                return work(15)";

            var vm = Compile(source, new TickVMOptions { MaxCallStackDepth = 20 });
            Utils.Run(vm, 100_000);
            Utils.AssertStringResult(vm, new string('x', 15) + "ok");
        }

        [Test]
        public void Limit_MustBePositive()
        {
            // Options are validated by the constructor, independently of any chunk.
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new TickVM(new TickVMOptions { MaxCallStackDepth = 0 }));
        }
    }
}
