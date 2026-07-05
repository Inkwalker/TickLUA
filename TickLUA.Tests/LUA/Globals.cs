namespace TickLUA_Tests.LUA
{
    internal class Globals
    {
        [Test]
        public void Global_AssignAndRead()
        {
            string source = @"
                x = 10
                return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 10);
        }

        [Test]
        public void Global_UnassignedIsNil()
        {
            // Reading a global that was never assigned yields nil, not an error.
            string source = @"return undefined_global";

            var vm = Utils.Run(source, 100);
            Utils.AssertNilResult(vm);
        }

        [Test]
        public void Global_Function()
        {
            string source = @"
                function add(a, b)
                    return a + b
                end
                return add(2, 3)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Global_VisibleAcrossScopes()
        {
            string source = @"
                counter = 0
                local function bump()
                    counter = counter + 1
                end
                bump()
                bump()
                return counter";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2);
        }
    }
}
