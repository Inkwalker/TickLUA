namespace TickLUA_Tests.LUA
{
    internal class Blocks
    {
        [Test]
        public void ExecutesBody()
        {
            string source = @"
                local a = 0
                do
                    a = 5
                end
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void InnerLocalShadowsOuter()
        {
            string source = @"
                local a = 1
                do
                    local a = 2
                    a = a + 10
                end
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1);
        }

        [Test]
        public void InnerCanMutateOuter()
        {
            string source = @"
                local a = 1
                do
                    a = a + 41
                end
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }
    }
}
