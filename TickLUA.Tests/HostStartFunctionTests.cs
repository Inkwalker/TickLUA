using System;
using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;

namespace TickLUA_Tests
{
    /// <summary>
    /// Tests for <see cref="TickVM.StartFunction"/> — the host-side entry
    /// point for calling script-defined global functions (init/update style
    /// callbacks) after the main chunk has run.
    /// </summary>
    internal class HostStartFunctionTests
    {
        [Test]
        public void StartFunction_RunsGlobalFunction()
        {
            var vm = Utils.Run(@"
                counter = 0
                function update()
                    counter = counter + 1
                end", 1000);

            vm.StartFunction("update");
            Assert.IsFalse(vm.IsFinished);
            Utils.Run(vm, 1000);

            var counter = (NumberObject)vm.Globals["counter"];
            Assert.That(counter.Value, Is.EqualTo(1));
        }

        [Test]
        public void StartFunction_CanBeCalledRepeatedly()
        {
            var vm = Utils.Run(@"
                counter = 0
                function update()
                    counter = counter + 1
                end", 1000);

            for (int i = 0; i < 3; i++)
            {
                vm.StartFunction("update");
                Utils.Run(vm, 1000);
            }

            var counter = (NumberObject)vm.Globals["counter"];
            Assert.That(counter.Value, Is.EqualTo(3));
        }

        [Test]
        public void StartFunction_PassesArguments()
        {
            var vm = Utils.Run(@"
                function add(a, b)
                    sum = a + b
                end", 1000);

            vm.StartFunction("add", new NumberObject(3), new NumberObject(4));
            Utils.Run(vm, 1000);

            var sum = (NumberObject)vm.Globals["sum"];
            Assert.That(sum.Value, Is.EqualTo(7));
        }

        [Test]
        public void StartFunction_MissingArgumentsAreNil()
        {
            var vm = Utils.Run(@"
                function probe(a)
                    got_nil = a == nil
                end", 1000);

            vm.StartFunction("probe");
            Utils.Run(vm, 1000);

            Assert.IsTrue((bool)(BooleanObject)vm.Globals["got_nil"]);
        }

        [Test]
        public void StartFunction_ReturnValuesAreIgnored()
        {
            var vm = Utils.Run(@"
                function f()
                    return 42
                end
                return 'main'", 1000);

            vm.StartFunction("f");
            Utils.Run(vm, 1000);

            // The main chunk's result is untouched by the host call.
            Utils.AssertStringResult(vm, "main");
        }

        [Test]
        public void StartFunction_SeesGlobalsThroughEnv()
        {
            var vm = Utils.Run(@"
                greeting = 'hello'
                function combine()
                    result = greeting .. ' world'
                end", 1000);

            vm.StartFunction("combine");
            Utils.Run(vm, 1000);

            var result = (StringObject)vm.Globals["result"];
            Assert.That(result.Value, Is.EqualTo("hello world"));
        }

        [Test]
        public void StartFunction_UnknownGlobalThrows()
        {
            var vm = Utils.Run("return 1", 1000);

            Assert.Throws<ArgumentException>(() => vm.StartFunction("nope"));
        }

        [Test]
        public void StartFunction_NonFunctionGlobalThrows()
        {
            var vm = Utils.Run("value = 5", 1000);

            Assert.Throws<ArgumentException>(() => vm.StartFunction("value"));
        }

        [Test]
        public void StartFunction_VmAwareNativeThrows()
        {
            var vm = Utils.Run("return 1", 1000);

            // pcall reads fixed caller registers and cannot run on a fresh stack.
            Assert.Throws<ArgumentException>(() => vm.StartFunction("pcall"));
        }

        [Test]
        public void TryStartFunction_ReturnsFalseForMissingGlobal()
        {
            var vm = Utils.Run("return 1", 1000);

            Assert.IsFalse(vm.TryStartFunction("nope"));
            Assert.IsTrue(vm.IsFinished);
        }

        [Test]
        public void TryStartFunction_ReturnsFalseForNonFunction()
        {
            var vm = Utils.Run("value = 5", 1000);

            Assert.IsFalse(vm.TryStartFunction("value"));
            Assert.IsFalse(vm.TryStartFunction("pcall"));
        }

        [Test]
        public void TryStartFunction_StartsExistingFunction()
        {
            var vm = Utils.Run(@"
                counter = 0
                function update()
                    counter = counter + 1
                end", 1000);

            Assert.IsTrue(vm.TryStartFunction("update"));
            Utils.Run(vm, 1000);

            var counter = (NumberObject)vm.Globals["counter"];
            Assert.That(counter.Value, Is.EqualTo(1));
        }

        [Test]
        public void StartFunction_ErrorPropagatesFromTick()
        {
            var vm = Utils.Run(@"
                function boom()
                    error('exploded')
                end", 1000);

            vm.StartFunction("boom");

            var ex = Assert.Throws<RuntimeException>(() =>
            {
                for (int i = 0; i < 1000 && !vm.IsFinished; i++)
                    vm.Tick();
            });
            StringAssert.Contains("exploded", ex.Message);
        }

        [Test]
        public void StartFunction_ErrorInsidePcallIsContained()
        {
            var vm = Utils.Run(@"
                function safe()
                    ok = pcall(function() error('caught') end)
                end", 1000);

            vm.StartFunction("safe");
            Utils.Run(vm, 1000);

            Assert.IsFalse((bool)(BooleanObject)vm.Globals["ok"]);
        }

        [Test]
        public void StartFunction_YieldAbandonsTheCall()
        {
            var vm = Utils.Run(@"
                stage = 'before'
                function stops()
                    stage = 'yielded'
                    coroutine.yield(1)
                    stage = 'after'
                end", 1000);

            vm.StartFunction("stops");
            Utils.Run(vm, 1000);

            // The yield hands control back to the host; the values are
            // discarded and the coroutine is never resumed.
            Assert.IsTrue(vm.IsFinished);
            var stage = (StringObject)vm.Globals["stage"];
            Assert.That(stage.Value, Is.EqualTo("yielded"));

            // The VM stays usable for further host calls.
            vm.StartFunction("stops");
            Utils.Run(vm, 1000);
            Assert.IsTrue(vm.IsFinished);
        }

        [Test]
        public void StartFunction_PlainNativeBodyRunsSynchronously()
        {
            var vm = Utils.Run("return 1", 1000);

            bool called = false;
            vm.Globals["host_fn"] = new NativeFunctionObject("host_fn", args =>
            {
                called = true;
                return null;
            });

            vm.StartFunction("host_fn");

            Assert.IsTrue(called);
            Assert.IsTrue(vm.IsFinished);
        }

        [Test]
        public void StartFunction_WhileMainIsRunningParksMain()
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
            var vm = new TickVM(func);

            // Advance until the function global exists but main is still looping.
            int guard = 0;
            while (!(vm.Globals["bump"] is ClosureObject))
            {
                vm.Tick();
                if (++guard > 1000)
                    Assert.Fail("bump was never defined");
            }
            Assert.IsFalse(vm.IsFinished);

            vm.StartFunction("bump");
            Utils.Run(vm, 1000);

            // The host call ran, then main resumed and finished normally.
            Assert.IsTrue((bool)(BooleanObject)vm.Globals["bumped"]);
            Utils.AssertIntegerResult(vm, 10);
        }

        [Test]
        public void StartFunction_WithTickerDrivesToFinish()
        {
            var vm = Utils.Run(@"
                calls = 0
                function update()
                    calls = calls + 1
                end", 1000);
            var ticker = new Ticker(vm);

            vm.StartFunction("update");
            Assert.That(ticker.TickToEnd(), Is.EqualTo(TickerResult.Finished));

            var calls = (NumberObject)vm.Globals["calls"];
            Assert.That(calls.Value, Is.EqualTo(1));
        }
    }
}
