namespace TickLUA_Tests.LUA
{
    internal class Coroutines
    {
        [Test]
        public void Resume_ReturnsYieldedValue()
        {
            string source = @"
                local co = coroutine.create(function()
                    coroutine.yield(1)
                    return 2
                end)
                local ok, v = coroutine.resume(co)
                return v";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1);
        }

        [Test]
        public void Resume_SecondResumeContinuesAfterYield()
        {
            string source = @"
                local co = coroutine.create(function()
                    coroutine.yield(1)
                    return 2
                end)
                coroutine.resume(co)
                local ok, v = coroutine.resume(co)
                return v";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2);
        }

        [Test]
        public void Resume_PassesArgumentsIntoBody()
        {
            string source = @"
                local co = coroutine.create(function(a)
                    return a * 2
                end)
                local ok, v = coroutine.resume(co, 21)
                return v";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void Status_DeadAfterCompletion()
        {
            string source = @"
                local co = coroutine.create(function() return 1 end)
                coroutine.resume(co)
                return coroutine.status(co)";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "dead");
        }

        [Test]
        public void Yield_PassesMultipleValues()
        {
            string source = @"
                local co = coroutine.create(function()
                    coroutine.yield(1, 2)
                end)
                local ok, a, b = coroutine.resume(co)
                return a, b";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1);
            Utils.AssertIntegerResult(vm, 2, 1);
        }

        [Test]
        public void Yield_ResumeArgumentsBecomeYieldResults()
        {
            string source = @"
                local co = coroutine.create(function()
                    local x = coroutine.yield(1)
                    return x + 1
                end)
                coroutine.resume(co)
                local ok, v = coroutine.resume(co, 41)
                return v";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void Resume_BodyErrorReturnsFalseAndMessage()
        {
            string source = @"
                local co = coroutine.create(function()
                    error('boom')
                end)
                local ok, err = coroutine.resume(co)
                return ok, err, coroutine.status(co)";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
            Utils.AssertStringResult(vm, "boom", 1);
            Utils.AssertStringResult(vm, "dead", 2);
        }

        [Test]
        public void Resume_DeadCoroutineReturnsFalse()
        {
            string source = @"
                local co = coroutine.create(function() return 1 end)
                coroutine.resume(co)
                local ok, err = coroutine.resume(co)
                return ok, err";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
            Utils.AssertStringResult(vm, "cannot resume dead coroutine", 1);
        }

        [Test]
        public void Resume_SelfResumeReturnsFalse()
        {
            string source = @"
                local co
                co = coroutine.create(function()
                    return coroutine.resume(co)
                end)
                local _, ok, err = coroutine.resume(co)
                return ok, err";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
            Utils.AssertStringResult(vm, "cannot resume non-suspended coroutine", 1);
        }

        [Test]
        public void Status_SuspendedBeforeStartAndAfterYield()
        {
            string source = @"
                local co = coroutine.create(function()
                    coroutine.yield()
                end)
                local before = coroutine.status(co)
                coroutine.resume(co)
                return before, coroutine.status(co)";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "suspended");
            Utils.AssertStringResult(vm, "suspended", 1);
        }

        [Test]
        public void Status_RunningInsideBody()
        {
            string source = @"
                local co
                co = coroutine.create(function()
                    return coroutine.status(co)
                end)
                local ok, s = coroutine.resume(co)
                return s";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "running");
        }

        [Test]
        public void Status_NormalForResumerOfNestedCoroutine()
        {
            string source = @"
                local outer
                local inner = coroutine.create(function()
                    return coroutine.status(outer)
                end)
                outer = coroutine.create(function()
                    local ok, s = coroutine.resume(inner)
                    return s
                end)
                local ok, s = coroutine.resume(outer)
                return s";

            var vm = Utils.Run(source, 200);
            Utils.AssertStringResult(vm, "normal");
        }

        [Test]
        public void Wrap_ReturnsRawValues()
        {
            string source = @"
                local gen = coroutine.wrap(function()
                    coroutine.yield(10)
                    return 20
                end)
                local a = gen()
                local b = gen()
                return a, b";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 10);
            Utils.AssertIntegerResult(vm, 20, 1);
        }

        [Test]
        public void Wrap_BodyErrorPropagatesToPcall()
        {
            string source = @"
                local gen = coroutine.wrap(function()
                    error('boom')
                end)
                local ok, err = pcall(gen)
                return ok, err";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
            Utils.AssertStringResult(vm, "boom", 1);
        }

        [Test]
        public void Wrap_CallingDeadCoroutineRaises()
        {
            string source = @"
                local gen = coroutine.wrap(function() return 1 end)
                gen()
                local ok, err = pcall(gen)
                return ok, err";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
            Utils.AssertStringResult(vm, "cannot resume dead coroutine", 1);
        }

        [Test]
        public void Nested_InnerYieldsToOuter_OuterYieldsToMain()
        {
            string source = @"
                local inner = coroutine.create(function()
                    coroutine.yield(1)
                end)
                local outer = coroutine.create(function()
                    local ok, v = coroutine.resume(inner)
                    coroutine.yield(v + 10)
                end)
                local ok, v = coroutine.resume(outer)
                return v";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 11);
        }

        [Test]
        public void Yield_InsidePcalledClosure()
        {
            string source = @"
                local co = coroutine.create(function()
                    local ok = pcall(function() coroutine.yield(1) end)
                    return ok
                end)
                local _, v1 = coroutine.resume(co)
                local _, v2 = coroutine.resume(co)
                return v1, v2";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 1);
            Utils.AssertBoolResult(vm, true, 1);
        }

        [Test]
        public void Yield_AsDirectPcallTarget()
        {
            string source = @"
                local co = coroutine.create(function()
                    local ok, x = pcall(coroutine.yield, 1)
                    return ok, x
                end)
                local _, y = coroutine.resume(co)
                local _, ok, x = coroutine.resume(co, 99)
                return y, ok, x";

            var vm = Utils.Run(source, 200);
            Utils.AssertIntegerResult(vm, 1);
            Utils.AssertBoolResult(vm, true, 1);
            Utils.AssertIntegerResult(vm, 99, 2);
        }

        [Test]
        public void Yield_FromMainChunkIsAnError()
        {
            string source = @"
                local ok, err = pcall(coroutine.yield)
                return ok, err";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
            Utils.AssertStringResult(vm, "attempt to yield from outside a coroutine", 1);
        }

        [Test]
        public void Resume_AsTailcall()
        {
            string source = @"
                local co = coroutine.create(function() return 7 end)
                local function f()
                    return coroutine.resume(co)
                end
                return f()";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true);
            Utils.AssertIntegerResult(vm, 7, 1);
        }

        [Test]
        public void Wrap_AsGenericForIterator()
        {
            string source = @"
                local gen = coroutine.wrap(function()
                    for i = 1, 3 do
                        coroutine.yield(i)
                    end
                end)
                local sum = 0
                for v in gen do
                    sum = sum + v
                end
                return sum";

            var vm = Utils.Run(source, 400);
            Utils.AssertIntegerResult(vm, 6);
        }

        [Test]
        public void Running_And_IsYieldable()
        {
            string source = @"
                local co = coroutine.create(function()
                    local c, is_main = coroutine.running()
                    coroutine.yield(coroutine.isyieldable(), is_main)
                end)
                local main_co, main_is_main = coroutine.running()
                local ok, yieldable, is_main = coroutine.resume(co)
                return main_is_main, coroutine.isyieldable(), yieldable, is_main";

            var vm = Utils.Run(source, 200);
            Utils.AssertBoolResult(vm, true);       // running() in main reports main
            Utils.AssertBoolResult(vm, false, 1);   // main is not yieldable
            Utils.AssertBoolResult(vm, true, 2);    // body is yieldable
            Utils.AssertBoolResult(vm, false, 3);   // running() in body is not main
        }

        [Test]
        public void Resume_VarargBodyForwardsArguments()
        {
            string source = @"
                local co = coroutine.create(function(...) return ... end)
                local ok, a, b, c = coroutine.resume(co, 1, 2, 3)
                return a, b, c";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void SuspendedCoroutineDoesNotKeepVmAlive()
        {
            string source = @"
                local co = coroutine.create(function()
                    coroutine.yield()
                    return 'never reached'
                end)
                coroutine.resume(co)
                return coroutine.status(co)";

            var vm = Utils.Run(source, 100);
            Assert.IsTrue(vm.IsFinished);
            Utils.AssertStringResult(vm, "suspended");
        }
    }
}
