using System;
using TickLUA.Compilers.LUA;
using TickLUA.VM.Debugging;
using TickLUA.VM.Serialization;

namespace TickLUA_Tests
{
    internal class TickerTests
    {
        private static (TickVM vm, Ticker ticker) Create(string source)
        {
            var func = LuaCompiler.Compile(source);
            var vm = new TickVM(func);
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
            var vm = new TickVM(stripped);
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
    }
}
