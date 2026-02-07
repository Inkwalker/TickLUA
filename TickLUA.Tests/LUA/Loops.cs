namespace TickLUA_Tests.LUA
{
    internal class Loops
    {
        [Test]
        public void WhileLoop()
        {
            string source = @"
                local a = 0

                while a < 10 do
                    a = a + 1
                end

                return a";

            var vm = Utils.Run(source, 150);
            Utils.AssertIntegerResult(vm, 10, 0);
        }

        [Test]
        public void RepeatLoop()
        {
            string source = @"
                local a = 0

                repeat
                    a = a + 1
                until a > 10

                return a";

            var vm = Utils.Run(source, 150);
            Utils.AssertIntegerResult(vm, 11, 0);
        }
    }
}
