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
    }
}
