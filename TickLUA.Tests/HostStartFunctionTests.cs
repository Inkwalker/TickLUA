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
        public void StartFunction_ReturnValuesAreObservable()
        {
            var vm = Utils.Run(@"
                function f()
                    return 42
                end
                return 'main'", 1000);

            var call = vm.StartFunction("f");
            Utils.Run(vm, 1000);

            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(call.Result.Length, Is.EqualTo(1));
            Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(42));
            Assert.IsNull(call.Error);

            // The main chunk's result is untouched by the host call.
            Utils.AssertStringResult(vm, "main");
        }

        [Test]
        public void StartFunction_ReturnsMultipleValues()
        {
            var vm = Utils.Run(@"
                function f()
                    return 1, 'two', true
                end", 1000);

            var call = vm.StartFunction("f");
            Utils.Run(vm, 1000);

            Assert.That(call.Result.Length, Is.EqualTo(3));
            Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(1));
            Assert.That(((StringObject)call.Result[1]).Value, Is.EqualTo("two"));
            Assert.IsTrue((bool)(BooleanObject)call.Result[2]);
        }

        [Test]
        public void StartFunction_NoReturnValuesGivesEmptyResult()
        {
            var vm = Utils.Run(@"
                function f()
                end", 1000);

            var call = vm.StartFunction("f");
            Utils.Run(vm, 1000);

            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(call.Result, Is.Empty);
        }

        [Test]
        public void StartFunction_IsRunningUntilTicked()
        {
            var vm = Utils.Run(@"
                function f()
                    return 1
                end", 1000);

            var call = vm.StartFunction("f");

            // The body has not executed yet: only the root frame was pushed.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Running));
            Assert.IsFalse(call.IsFinished);
            Assert.That(call.Result, Is.Empty);

            Utils.Run(vm, 1000);
            Assert.IsTrue(call.IsFinished);
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

            var call = vm.StartFunction("boom");

            var ex = Assert.Throws<RuntimeException>(() =>
            {
                for (int i = 0; i < 1000 && !vm.IsFinished; i++)
                    vm.Tick();
            });
            StringAssert.Contains("exploded", ex.Message);

            // The handle is a second view of the same failure, not a
            // replacement for the rethrow.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Faulted));
            Assert.That(call.Error, Is.SameAs(ex));
            StringAssert.Contains("exploded", ((StringObject)call.Error.ErrorValue).Value);
            Assert.That(call.Error.LuaTraceback, Is.Not.Empty);
            Assert.That(call.Result, Is.Empty);
        }

        [Test]
        public void StartFunction_ErrorFromNativeBodyFaultsTheCall()
        {
            var vm = Utils.Run("return 1", 1000);
            vm.Globals["host_fn"] = new NativeFunctionObject("host_fn",
                args => throw new RuntimeException("native boom"));

            // A plain native body runs synchronously, so the error surfaces out
            // of StartFunction itself rather than out of a later Tick.
            LuaCall? call = null;
            var ex = Assert.Throws<RuntimeException>(
                () => vm.TryStartFunction("host_fn", out call));

            StringAssert.Contains("native boom", ex.Message);
            Assert.IsNotNull(call);
            Assert.That(call!.Status, Is.EqualTo(LuaCallStatus.Faulted));
            Assert.That(call.Error, Is.SameAs(ex));
        }

        [Test]
        public void StartFunction_ErrorInsidePcallIsContained()
        {
            var vm = Utils.Run(@"
                function safe()
                    ok = pcall(function() error('caught') end)
                end", 1000);

            var call = vm.StartFunction("safe");
            Utils.Run(vm, 1000);

            Assert.IsFalse((bool)(BooleanObject)vm.Globals["ok"]);

            // pcall handled it, so the call itself never failed.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.IsNull(call.Error);
        }

        [Test]
        public void StartFunction_YieldPausesTheCall()
        {
            var vm = Utils.Run(@"
                stage = 'before'
                function stops()
                    stage = 'yielded'
                    coroutine.yield(1)
                    stage = 'after'
                end", 1000);

            var call = vm.StartFunction("stops");
            Utils.Run(vm, 1000);

            // The yield hands control back to the host, carrying its values.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Paused));
            Assert.IsFalse(call.IsFinished);
            Assert.That(call.Result.Length, Is.EqualTo(1));
            Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(1));
            Assert.That(((StringObject)vm.Globals["stage"]).Value, Is.EqualTo("yielded"));

            // A paused call does not hold the VM: main is idle either way.
            Assert.IsTrue(vm.IsFinished);

            call.Resume();
            Utils.Run(vm, 1000);

            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(((StringObject)vm.Globals["stage"]).Value, Is.EqualTo("after"));
        }

        [Test]
        public void Resume_TakesEffectOnTheNextTick()
        {
            var vm = Utils.Run(@"
                stage = 'before'
                function stops()
                    coroutine.yield()
                    stage = 'after'
                end", 1000);

            var call = vm.StartFunction("stops");
            Utils.Run(vm, 1000);
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Paused));

            call.Resume();

            // Queued only — nothing has advanced yet.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Running));
            Assert.That(((StringObject)vm.Globals["stage"]).Value, Is.EqualTo("before"));

            Utils.Run(vm, 1000);
            Assert.That(((StringObject)vm.Globals["stage"]).Value, Is.EqualTo("after"));
        }

        [Test]
        public void Resume_ArgumentsBecomeTheYieldResults()
        {
            var vm = Utils.Run(@"
                function echo()
                    local a, b = coroutine.yield('ask')
                    return a, b
                end", 1000);

            var call = vm.StartFunction("echo");
            Utils.Run(vm, 1000);
            Assert.That(((StringObject)call.Result[0]).Value, Is.EqualTo("ask"));

            call.Resume(new NumberObject(7), new StringObject("eight"));
            Utils.Run(vm, 1000);

            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(7));
            Assert.That(((StringObject)call.Result[1]).Value, Is.EqualTo("eight"));
        }

        [Test]
        public void Resume_DrivesSuccessiveYields()
        {
            var vm = Utils.Run(@"
                function counter()
                    for i = 1, 3 do
                        coroutine.yield(i)
                    end
                    return 'done'
                end", 1000);

            var call = vm.StartFunction("counter");

            for (int expected = 1; expected <= 3; expected++)
            {
                Utils.Run(vm, 1000);
                Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Paused));
                Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(expected));
                call.Resume();
            }

            Utils.Run(vm, 1000);
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(((StringObject)call.Result[0]).Value, Is.EqualTo("done"));
        }

        [Test]
        public void Resume_OnNonPausedCallIsRejected()
        {
            var vm = Utils.Run(@"
                function f()
                    return 1
                end", 1000);

            var call = vm.StartFunction("f");
            Utils.Run(vm, 1000);
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));

            Assert.Throws<InvalidOperationException>(() => call.Resume());
            Assert.IsFalse(call.TryResume());
        }

        [Test]
        public void StartFunction_PlainNativeBodyRunsSynchronously()
        {
            var vm = Utils.Run("return 1", 1000);

            bool called = false;
            vm.Globals["host_fn"] = new NativeFunctionObject("host_fn", args =>
            {
                called = true;
                return new LuaObject[] { new StringObject("from native") };
            });

            var call = vm.StartFunction("host_fn");

            Assert.IsTrue(called);
            Assert.IsTrue(vm.IsFinished);

            // No tick needed: the handle is already settled on return.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(((StringObject)call.Result[0]).Value, Is.EqualTo("from native"));
        }

        [Test]
        public void TryStartFunction_OutOverloadSuppliesTheHandle()
        {
            var vm = Utils.Run(@"
                function f()
                    return 5
                end", 1000);

            LuaCall call;
            Assert.IsTrue(vm.TryStartFunction("f", out call));
            Assert.IsNotNull(call);

            Utils.Run(vm, 1000);
            Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(5));
        }

        [Test]
        public void TryStartFunction_OutOverloadYieldsNullHandleOnFailure()
        {
            var vm = Utils.Run("value = 5", 1000);

            LuaCall call;
            Assert.IsFalse(vm.TryStartFunction("nope", out call));
            Assert.IsNull(call);

            Assert.IsFalse(vm.TryStartFunction("value", out call));
            Assert.IsNull(call);
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
            var vm = Utils.Load(func);

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
        public void Cancel_StopsAPausedCall()
        {
            var vm = Utils.Run(@"
                stage = 'before'
                function stops()
                    coroutine.yield()
                    stage = 'after'
                end", 1000);

            var call = vm.StartFunction("stops");
            Utils.Run(vm, 1000);
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Paused));

            call.Cancel();
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Cancelled));
            Assert.IsTrue(call.IsFinished);

            Utils.Run(vm, 1000);

            // The rest of the body never ran, and the VM is still usable.
            Assert.That(((StringObject)vm.Globals["stage"]).Value, Is.EqualTo("before"));
            var again = vm.StartFunction("stops");
            Utils.Run(vm, 1000);
            Assert.That(again.Status, Is.EqualTo(LuaCallStatus.Paused));
        }

        [Test]
        public void Cancel_StopsACallThatIsMidExecution()
        {
            var func = LuaCompiler.Compile(@"
                function spin()
                    for i = 1, 100 do
                        spun = i
                    end
                end
                total = 0
                for i = 1, 10 do
                    total = total + 1
                end
                return total");
            var vm = Utils.Load(func);

            // Park main mid-loop, then start a call so it becomes current.
            int guard = 0;
            while (!(vm.Globals["spin"] is ClosureObject))
            {
                vm.Tick();
                if (++guard > 1000)
                    Assert.Fail("spin was never defined");
            }

            var call = vm.StartFunction("spin");

            // Let the body get partway into its loop.
            guard = 0;
            while (!(vm.Globals["spun"] is NumberObject))
            {
                vm.Tick();
                if (++guard > 1000)
                    Assert.Fail("spin never ran");
            }

            var spun_at_cancel = ((NumberObject)vm.Globals["spun"]).Value;
            call.Cancel();
            Utils.Run(vm, 1000);

            // The body stopped where it was, and the parked main finished.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Cancelled));
            Assert.That(((NumberObject)vm.Globals["spun"]).Value, Is.EqualTo(spun_at_cancel));
            Utils.AssertIntegerResult(vm, 10);
        }

        [Test]
        public void Cancel_KillsCoroutinesTheCallResumed()
        {
            // The child is mid-run when the cancel lands, so it sits between the
            // cancelled call and the VM's current coroutine: killing only the
            // call would leave the child pointing at a dead resumer.
            var vm = Utils.Run(@"
                function outer()
                    local child = coroutine.create(function()
                        for i = 1, 1000 do
                            child_spun = i
                        end
                        child_finished = true
                    end)
                    coroutine.resume(child)
                    outer_finished = true
                end", 1000);

            var call = vm.StartFunction("outer");

            int guard = 0;
            while (!(vm.Globals["child_spun"] is NumberObject))
            {
                vm.Tick();
                if (++guard > 1000)
                    Assert.Fail("the child coroutine never ran");
            }

            var spun_at_cancel = ((NumberObject)vm.Globals["child_spun"]).Value;
            call.Cancel();
            Utils.Run(vm, 1000);

            // Neither the call nor the coroutine it resumed runs again, and the
            // VM keeps ticking cleanly afterwards.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Cancelled));
            Assert.That(((NumberObject)vm.Globals["child_spun"]).Value, Is.EqualTo(spun_at_cancel));
            Assert.IsTrue(LuaObject.NullOrNil(vm.Globals["child_finished"]));
            Assert.IsTrue(LuaObject.NullOrNil(vm.Globals["outer_finished"]));
            Assert.IsTrue(vm.IsFinished);
        }

        [Test]
        public void Cancel_IsIdempotentAndIgnoredAfterCompletion()
        {
            var vm = Utils.Run(@"
                function f()
                    return 1
                end", 1000);

            var call = vm.StartFunction("f");
            Utils.Run(vm, 1000);
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));

            call.Cancel();
            call.Cancel();

            // A finished call keeps its outcome.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(1));
            Utils.Run(vm, 1000);
        }

        [Test]
        public void Cancel_AfterResumeWinsAndRunsNothing()
        {
            var vm = Utils.Run(@"
                stage = 'before'
                function stops()
                    coroutine.yield()
                    stage = 'after'
                end", 1000);

            var call = vm.StartFunction("stops");
            Utils.Run(vm, 1000);

            call.Resume();
            call.Cancel();
            Utils.Run(vm, 1000);

            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Cancelled));
            Assert.That(((StringObject)vm.Globals["stage"]).Value, Is.EqualTo("before"));
        }

        [Test]
        public void Cancel_RejectsALaterResume()
        {
            var vm = Utils.Run(@"
                function stops()
                    coroutine.yield()
                end", 1000);

            var call = vm.StartFunction("stops");
            Utils.Run(vm, 1000);
            call.Cancel();

            Assert.Throws<InvalidOperationException>(() => call.Resume());
            Assert.IsFalse(call.TryResume());
        }

        [Test]
        public void Cancel_LeavesTheCoroutineDeadForScripts()
        {
            var vm = Utils.Run(@"
                function stops()
                    captured = coroutine.running()
                    coroutine.yield()
                end
                function probe()
                    status = coroutine.status(captured)
                    resumed, err = coroutine.resume(captured)
                end", 1000);

            var call = vm.StartFunction("stops");
            Utils.Run(vm, 1000);
            call.Cancel();

            vm.StartFunction("probe");
            Utils.Run(vm, 1000);

            Assert.That(((StringObject)vm.Globals["status"]).Value, Is.EqualTo("dead"));
            Assert.IsFalse((bool)(BooleanObject)vm.Globals["resumed"]);
            StringAssert.Contains("dead", ((StringObject)vm.Globals["err"]).Value);
        }

        [Test]
        public void StartFunction_HandleExposesItsVm()
        {
            var vm = Utils.Run(@"
                function stops()
                    coroutine.yield()
                end", 1000);

            var call = vm.StartFunction("stops");

            // The handle carries its VM, so it can be driven on its own.
            Assert.That(call.VM, Is.SameAs(vm));

            while (!call.VM.IsFinished)
                call.VM.Tick();
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Paused));

            call.Dispose();
            Assert.That(call.VM, Is.SameAs(vm), "the VM stays reachable after disposal");
        }

        [Test]
        public void Dispose_CancelsAnUnfinishedCall()
        {
            var vm = Utils.Run(@"
                stage = 'before'
                function stops()
                    coroutine.yield()
                    stage = 'after'
                end", 1000);

            var call = vm.StartFunction("stops");
            Utils.Run(vm, 1000);
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Paused));

            call.Dispose();

            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Cancelled));
            Utils.Run(vm, 1000);
            Assert.That(((StringObject)vm.Globals["stage"]).Value, Is.EqualTo("before"));
            Assert.IsTrue(vm.IsFinished);
        }

        [Test]
        public void Dispose_KeepsTheOutcomeOfAFinishedCall()
        {
            var vm = Utils.Run(@"
                function f()
                    return 42
                end", 1000);

            var call = vm.StartFunction("f");
            Utils.Run(vm, 1000);

            call.Dispose();

            // Disposing after the fact drops the handle, not the answer.
            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
            Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(42));
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var vm = Utils.Run(@"
                function stops()
                    coroutine.yield()
                end", 1000);

            var call = vm.StartFunction("stops");
            Utils.Run(vm, 1000);

            call.Dispose();
            call.Dispose();

            Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Cancelled));
            Utils.Run(vm, 1000);
            Assert.IsTrue(vm.IsFinished);
        }

        [Test]
        public void Dispose_RejectsALaterResume()
        {
            var vm = Utils.Run(@"
                function stops()
                    coroutine.yield()
                end", 1000);

            var call = vm.StartFunction("stops");
            Utils.Run(vm, 1000);
            call.Dispose();

            Assert.Throws<ObjectDisposedException>(() => call.Resume());
            Assert.IsFalse(call.TryResume());
        }

        [Test]
        public void Dispose_DrivenToCompletionInsideAUsingBlock()
        {
            var vm = Utils.Run(@"
                function f()
                    return 7
                end", 1000);

            // The tick loop belongs inside the block: leaving it early would
            // cancel a call still in flight.
            using (var call = vm.StartFunction("f"))
            {
                Utils.Run(vm, 1000);
                Assert.That(call.Status, Is.EqualTo(LuaCallStatus.Completed));
                Assert.That(((NumberObject)call.Result[0]).Value, Is.EqualTo(7));
            }

            Assert.IsTrue(vm.IsFinished);
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
