using System;
using TickLUA.Compilers.LUA;
using TickLUA.VM.Debugging;
using TickLUA.VM.Objects;
using TickLUA.VM.Serialization;

namespace TickLUA_Tests
{
    internal class TickerTests
    {
        private static (TickVM vm, Ticker ticker) Create(string source)
        {
            var func = LuaCompiler.Compile(source);
            var vm = Utils.Load(func);
            return (vm, new Ticker(vm));
        }

        [Test]
        public void Tick_AdvancesOneInstructionByDefault()
        {
            var (vm, ticker) = Create("local a = 1\nreturn a");

            Assert.That(ticker.Tick(), Is.EqualTo(TickerResult.Advanced));
            // A multi-instruction chunk is not finished after a single tick.
            Assert.IsFalse(ticker.IsFinished);
        }

        [Test]
        public void Tick_BatchRunsToFinish()
        {
            var (vm, ticker) = Create("local a = 1\nlocal b = 2\nreturn a + b");

            Assert.That(ticker.Tick(1000), Is.EqualTo(TickerResult.Finished));
            Assert.IsTrue(ticker.IsFinished);
            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void Tick_StopsAtRequestedCount()
        {
            var (vm, ticker) = Create("while true do end");

            Assert.That(ticker.Tick(500), Is.EqualTo(TickerResult.Advanced));
            Assert.IsFalse(ticker.IsFinished);
        }

        [Test]
        public void Tick_AfterFinishReturnsFinished()
        {
            var (vm, ticker) = Create("return 1");

            Assert.That(ticker.Tick(1000), Is.EqualTo(TickerResult.Finished));
            Assert.That(ticker.Tick(), Is.EqualTo(TickerResult.Finished));
        }

        [Test]
        public void Tick_RejectsNonPositiveCount()
        {
            var (vm, ticker) = Create("return 1");

            Assert.Throws<ArgumentOutOfRangeException>(() => ticker.Tick(0));
        }

        [Test]
        public void TickLine_WalksLineSequence()
        {
            var (vm, ticker) = Create("local a = 1\nlocal b = 2\nlocal c = a + b\nreturn c");

            Assert.That(ticker.CurrentLine, Is.EqualTo(1));
            Assert.That(ticker.TickLine(), Is.EqualTo(TickerResult.Advanced));
            Assert.That(ticker.CurrentLine, Is.EqualTo(2));
            Assert.That(ticker.TickLine(), Is.EqualTo(TickerResult.Advanced));
            Assert.That(ticker.CurrentLine, Is.EqualTo(3));
            Assert.That(ticker.TickLine(), Is.EqualTo(TickerResult.Advanced));
            Assert.That(ticker.CurrentLine, Is.EqualTo(4));
            Assert.That(ticker.TickLine(), Is.EqualTo(TickerResult.Finished));

            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void TickLine_StopsEachIterationOfOneLineLoop()
        {
            var (vm, ticker) = Create("local i = 0\nwhile i < 3 do i = i + 1 end\nreturn i");

            // Each call makes progress (backward jump on the same line counts
            // as a step), so a bounded number of calls must finish the chunk.
            TickerResult result;
            int guard = 0;
            while ((result = ticker.TickLine()) == TickerResult.Advanced)
                Assert.That(++guard, Is.LessThan(100));

            Assert.That(result, Is.EqualTo(TickerResult.Finished));
            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void TickLine_WorksOnStrippedBytecode()
        {
            // Line info survives stripping, so line stepping works where a
            // DebugSession refuses to attach.
            var func = LuaCompiler.Compile("local a = 1\nlocal b = 2\nreturn a + b");
            var stripped = BytecodeSerializer.Deserialize(
                BytecodeSerializer.Serialize(func, stripDebugInfo: true));
            var vm = Utils.Load(stripped);
            Assert.Throws<InvalidOperationException>(() => new DebugSession(vm));

            var ticker = new Ticker(vm);
            Assert.That(ticker.CurrentLine, Is.EqualTo(1));
            Assert.That(ticker.TickLine(), Is.EqualTo(TickerResult.Advanced));
            Assert.That(ticker.CurrentLine, Is.EqualTo(2));
        }

        [Test]
        public void TickToEnd_Finishes()
        {
            var (vm, ticker) = Create("local sum = 0\nfor i = 1, 10 do\nsum = sum + i\nend\nreturn sum");

            Assert.That(ticker.TickToEnd(), Is.EqualTo(TickerResult.Finished));
            Utils.AssertIntegerResult(vm, 55);
        }

        [Test]
        public void TickToEnd_LimitStopsInfiniteLoop()
        {
            var (vm, ticker) = Create("while true do end");

            Assert.That(ticker.TickToEnd(1000), Is.EqualTo(TickerResult.LimitReached));
            Assert.IsFalse(ticker.IsFinished);

            // The VM is paused, not broken: continuing is possible.
            Assert.That(ticker.TickToEnd(1000), Is.EqualTo(TickerResult.LimitReached));
        }

        [Test]
        public void RunFunction_RunsAGlobalToCompletion()
        {
            var (vm, ticker) = Create(@"
                function add(a, b)
                    return a + b
                end");
            ticker.TickToEnd();

            var call = ticker.RunFunction("add", new NumberObject(3), new NumberObject(4));

            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(7));
        }

        [Test]
        public void RunFunction_ResumesYieldsAutomatically()
        {
            var (vm, ticker) = Create(@"
                function work()
                    local total = 0
                    for i = 1, 4 do
                        total = total + i
                        coroutine.yield(i)
                    end
                    return total
                end");
            ticker.TickToEnd();

            var call = ticker.RunFunction("work");

            // Every yield was resumed without the host lifting a finger.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(10));
        }

        [Test]
        public void RunFunction_ResumesWithoutArguments()
        {
            var (vm, ticker) = Create(@"
                function probe()
                    local got = coroutine.yield()
                    return got == nil
                end");
            ticker.TickToEnd();

            var call = ticker.RunFunction("probe");

            Assert.IsTrue((bool)(BooleanObject)call.Result[0]);
        }

        [Test]
        public void RunFunction_LimitLeavesTheCallResumable()
        {
            var (vm, ticker) = Create(@"
                function spin()
                    while true do
                        coroutine.yield()
                    end
                end");
            ticker.TickToEnd();

            var call = ticker.RunFunction("spin", 100);

            Assert.IsFalse(call.IsFinished);

            // Not broken, just out of budget — the call keeps going.
            Assert.That(ticker.TickCallToEnd(call, 100), Is.EqualTo(TickerResult.LimitReached));

            call.Cancel();
            Assert.That(ticker.TickCallToEnd(call, 100), Is.EqualTo(TickerResult.Finished));
        }

        [Test]
        public void RunFunction_StopsAtTheCallNotTheVm()
        {
            var func = LuaCompiler.Compile(@"
                function bump()
                    bumped = true
                end
                total = 0
                for i = 1, 10 do
                    total = total + 1
                end
                return total");
            var vm = Utils.Load(func);
            var ticker = new Ticker(vm);

            // Park main mid-loop.
            int guard = 0;
            while (!(vm.Globals["bump"] is ClosureObject))
            {
                vm.Tick();
                if (++guard > 1000)
                    Assert.Fail("bump was never defined");
            }

            var call = ticker.RunFunction("bump");

            // The call finished; main is still parked for the host to drive on.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.IsFalse(ticker.IsFinished);

            Assert.That(ticker.TickToEnd(), Is.EqualTo(TickerResult.Finished));
            Utils.AssertIntegerResult(vm, 10);
        }

        [Test]
        public void RunFunction_ErrorPropagates()
        {
            var (vm, ticker) = Create(@"
                function boom()
                    error('exploded')
                end");
            ticker.TickToEnd();

            var ex = Assert.Throws<RuntimeException>(() => ticker.RunFunction("boom"));
            StringAssert.Contains("exploded", ex.Message);
        }

        [Test]
        public void RunFunction_UnknownGlobalThrows()
        {
            var (vm, ticker) = Create("return 1");
            ticker.TickToEnd();

            Assert.Throws<ArgumentException>(() => ticker.RunFunction("nope"));
        }

        [Test]
        public void TickCallToEnd_RejectsACallFromAnotherVm()
        {
            var (vm, ticker) = Create(@"
                function stops()
                    coroutine.yield()
                end");
            ticker.TickToEnd();

            var (other_vm, _) = Create(@"
                function stops()
                    coroutine.yield()
                end");
            Utils.Run(other_vm, 1000);
            var foreign = other_vm.StartFunction("stops");

            Assert.Throws<ArgumentException>(() => ticker.TickCallToEnd(foreign));
            Assert.Throws<ArgumentNullException>(() => ticker.TickCallToEnd(null));
        }
    }
}
