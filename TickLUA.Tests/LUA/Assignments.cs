namespace TickLUA_Tests.LUA
{
    internal class Assignments
    {
        [Test]
        public void Assignment()
        {
            string source = @"
                local a = 5
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Assignment_Local()
        {
            string source = @"
                local a = 5
                a = 10
                a = 15
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 15);
        }

        [Test]
        public void AssignmentAdd()
        {
            string source = @"
                local a = 5
                local b = 10
                b += a
                return b";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 15);
        }
    }
}
