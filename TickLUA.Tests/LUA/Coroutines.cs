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
    }
}
