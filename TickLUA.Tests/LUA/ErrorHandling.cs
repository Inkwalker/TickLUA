namespace TickLUA_Tests.LUA
{
    internal class ErrorHandling
    {
        [Test]
        public void Pcall_Success_ReturnsTrue()
        {
            // pcall returns true followed by the call's results when no error occurs.
            string source = @"
                local ok, v = pcall(function() return 42 end)
                return ok, v";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertIntegerResult(vm, 42, 1);
        }

        [Test]
        public void Pcall_Error_ReturnsFalse()
        {
            // pcall returns false plus the error object when the call raises an error.
            string source = @"
                local ok = pcall(function() error('boom') end)
                return ok";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Pcall_Error_ReturnsMessage()
        {
            string source = @"
                local ok, err = pcall(function() error('boom') end)
                return err";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "boom");
        }

        [Test]
        public void Assert_FailureRaises()
        {
            // A failing assert raises an error, caught here by pcall as false.
            string source = @"
                local ok = pcall(function() assert(false, 'nope') end)
                return ok";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void Assert_PassthroughOnTruthy()
        {
            // assert returns its arguments unchanged when the first is truthy.
            string source = @"return assert(7)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 7);
        }
    }
}
