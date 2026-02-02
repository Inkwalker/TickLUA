using TickLUA.VM.Objects;

namespace TickLUA_Tests.LUA
{
    internal class Table
    {
        [Test]
        public void Def_Empty()
        {
            string source = @"
            local a = {}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm);
        }

        [Test]
        public void Def_List()
        {
            string source = @"
            local a = {2, 3, 4}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(2));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(3));
            Utils.AssertTableResult(vm, new NumberObject(3), new NumberObject(4));
        }

        [Test]
        public void Def_Var()
        {
            string source = @"
            local x = 10
            local y = 11
            local a = {[x] = 1, [y] = 5}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(10), new NumberObject(1));
            Utils.AssertTableResult(vm, new NumberObject(11), new NumberObject(5));
        }

        [Test]
        public void Def_Mixed()
        {
            string source = @"
            local x = 10
            local a = {5, [x] = 1, 6}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(5));
            Utils.AssertTableResult(vm, new NumberObject(10), new NumberObject(1));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(6));
        }

        [Test]
        public void Index_Set()
        {
            string source = @"
            local a = {}
            a[5] = 42
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(5), new NumberObject(42));
        }

        [Test]
        public void Index_Get()
        {
            string source = @"
            local a = {10, 15, 20}
            local x = a[2]
            return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 15);
        }

        [Test]
        public void Len()
        {
            string source = @"
            local a = {10, 15, 20}
            local x = #a
            return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 3);
        }
    }
}
