using System;
using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;
using TickLUA.VM.Serialization;

namespace TickLUA_Tests
{
    /// <summary>
    /// Tests for <see cref="TickVM.Load"/> — the main chunk runs as a call like
    /// any other, so the VM is constructed empty and the chunk is started
    /// separately, reporting through a <see cref="LuaCall"/>.
    /// </summary>
    internal class LoadTests
    {
        [Test]
        public void Load_ReturnsAHandleForTheMainChunk()
        {
            var vm = new TickVM();
            var main = vm.Load(LuaCompiler.Compile("return 42"));

            Assert.That(main.Status, Is.EqualTo(LuaCallStatus.Running));
            Utils.Run(vm, 1000);

            Assert.That(main.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(((NumberObject)main.Result[0]).Value, Is.EqualTo(42));
            Assert.That(vm.MainCall, Is.SameAs(main));
        }

        [Test]
        public void Load_ExecutionResultMirrorsTheHandle()
        {
            var vm = Utils.Load(LuaCompiler.Compile("return 'hi', 2"));
            Utils.Run(vm, 1000);

            Assert.That(vm.ExecutionResult, Is.SameAs(vm.MainCall.Result));
            Utils.AssertStringResult(vm, "hi");
            Utils.AssertIntegerResult(vm, 2, 1);
        }

        [Test]
        public void Load_BeforeLoadingTheVmIsIdleAndResultless()
        {
            var vm = new TickVM();

            Assert.IsNull(vm.MainCall);
            Assert.IsNull(vm.ExecutionResult);
            Assert.IsTrue(vm.IsFinished);

            // Ticking an empty VM is a no-op, not a crash.
            vm.Tick();
            Assert.IsTrue(vm.IsFinished);
        }

        [Test]
        public void Load_TwiceThrows()
        {
            var vm = new TickVM();
            vm.Load(LuaCompiler.Compile("return 1"));

            Assert.Throws<InvalidOperationException>(
                () => vm.Load(LuaCompiler.Compile("return 2")));
        }

        [Test]
        public void Load_RejectsNullBytecode()
        {
            var vm = new TickVM();

            Assert.Throws<ArgumentNullException>(() => vm.Load(null));
        }

        [Test]
        public void Load_RejectsIncompatibleBytecodeVersion()
        {
            var func = LuaCompiler.Compile("return 1");
            func.CompilerVersion = LuaFunction.CurrentCompilerVersion + 1;

            var vm = new TickVM();
            Assert.Throws<BytecodeFormatException>(() => vm.Load(func));
        }

        [Test]
        public void Load_SeesGlobalsRegisteredBeforehand()
        {
            // The whole point of splitting Load out of the constructor: the host
            // can furnish the environment before a single instruction runs.
            var vm = new TickVM();
            vm.Globals["host_value"] = new NumberObject(9);

            vm.Load(LuaCompiler.Compile("return host_value * 2"));
            Utils.Run(vm, 1000);

            Utils.AssertIntegerResult(vm, 18);
        }

        [Test]
        public void Load_PassesArgumentsAsVarargsAndArgTable()
        {
            var vm = Utils.Load(
                LuaCompiler.Compile("local a, b = ...\nreturn a + b, #arg, arg[1]"),
                LuaObject.From(4), LuaObject.From(5));
            Utils.Run(vm, 1000);

            Utils.AssertIntegerResult(vm, 9);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 4, 2);
        }

        [Test]
        public void Load_MainChunkAndHostCallShareGlobals()
        {
            var vm = Utils.Run(@"
                counter = 1
                function bump()
                    counter = counter + 1
                end
                return counter", 1000);

            Utils.AssertIntegerResult(vm, 1);

            vm.StartFunction("bump");
            Utils.Run(vm, 1000);

            Assert.That(((NumberObject)vm.Globals["counter"]).Value, Is.EqualTo(2));
            // The main chunk's own result is untouched by the later call.
            Utils.AssertIntegerResult(vm, 1);
        }

        [Test]
        public void Load_MainChunkErrorFaultsTheHandleAndRethrows()
        {
            var vm = Utils.Load(LuaCompiler.Compile("error('main boom')"));
            var main = vm.MainCall;

            var ex = Assert.Throws<RuntimeException>(() =>
            {
                for (int i = 0; i < 1000 && !vm.IsFinished; i++)
                    vm.Tick();
            });

            StringAssert.Contains("main boom", ex.Message);
            Assert.That(main.Status, Is.EqualTo(LuaCallStatus.Faulted));
            Assert.That(main.Error, Is.SameAs(ex));
        }

        [Test]
        public void Load_MainChunkCanBeCancelled()
        {
            var vm = Utils.Load(LuaCompiler.Compile(@"
                stage = 'before'
                coroutine.yield()
                stage = 'after'"));
            var main = vm.MainCall;

            Utils.Run(vm, 1000);
            Assert.That(main.Status, Is.EqualTo(LuaCallStatus.Paused));

            main.Cancel();
            Utils.Run(vm, 1000);

            Assert.That(main.Status, Is.EqualTo(LuaCallStatus.Cancelled));
            Assert.That(((StringObject)vm.Globals["stage"]).Value, Is.EqualTo("before"));
        }

        [Test]
        public void Load_DebugSessionNeedsAChunk()
        {
            var vm = new TickVM();

            // The session validates debug info against the loaded chunk, so it
            // has nothing to attach to yet.
            var ex = Assert.Throws<InvalidOperationException>(
                () => new TickLUA.VM.Debugging.DebugSession(vm));
            StringAssert.Contains("Load", ex.Message);

            vm.Load(LuaCompiler.Compile("return 1"));
            Assert.DoesNotThrow(() => new TickLUA.VM.Debugging.DebugSession(vm));
        }

        [Test]
        public void Load_TickerDrivesAYieldingMainChunkToTheEnd()
        {
            // TickToEnd auto-resumes a parked chunk rather than mistaking the
            // idle VM for a finished one.
            var vm = Utils.Load(LuaCompiler.Compile(@"
                local total = 0
                for i = 1, 3 do
                    total = total + i
                    coroutine.yield(i)
                end
                return total"));
            var ticker = new Ticker(vm);

            Assert.That(ticker.TickToEnd(), Is.EqualTo(TickerResult.Finished));

            Assert.That(vm.MainCall.Status, Is.EqualTo(LuaCallStatus.Completed));
            Utils.AssertIntegerResult(vm, 6);
        }
    }
}
