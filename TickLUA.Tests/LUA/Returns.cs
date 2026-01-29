namespace TickLUA_Tests.LUA
{
    internal class Returns
    {
        [Test]
        public void ReturnNothing()
        {
            string source = @"
                return";

            var vm = Utils.Run(source, 100);

            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 0);
        }

        [Test]
        public void ReturnOne()
        {
            string source = @"
                return 5";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 1);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void ReturnMulti()
        {
            string source = @"
                return 1, 2, 3";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 3);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void ReturnMultiCopy()
        {
            string source = @"
                local a = 1
                local b = 2
                local c = 3
                return c, b, a";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 3);
            Utils.AssertIntegerResult(vm, 3, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 1, 2);
        }
    }
}
