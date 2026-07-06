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
        public void Def_String()
        {
            string source = @"
            local a = {x = 1, y = 5}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new StringObject("x"), new NumberObject(1));
            Utils.AssertTableResult(vm, new StringObject("y"), new NumberObject(5));
        }

        [Test]
        public void Def_Mixed()
        {
            string source = @"
            local x = 10
            local a = {5, [x] = 1, 6, y = 11}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(5));
            Utils.AssertTableResult(vm, new NumberObject(10), new NumberObject(1));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(6));
            Utils.AssertTableResult(vm, new StringObject("y"), new NumberObject(11));
        }

        [Test]
        public void Index_Set_Expression()
        {
            string source = @"
            local a = {}
            a[5] = 42
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(5), new NumberObject(42));
        }

        [Test]
        public void Index_Get_Expression()
        {
            string source = @"
            local a = {10, 15, 20}
            local x = a[2]
            return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 15);
        }

        [Test]
        public void Index_Set_String()
        {
            string source = @"
            local a = {}
            a.x = 42
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new StringObject("x"), new NumberObject(42));
        }

        [Test]
        public void Index_Get_String()
        {
            string source = @"
            local a = {x = 10, y = 15, z = 20}
            local y = a.y
            return y";

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

        [Test]
        public void NilAssignment_RemovesEntry()
        {
            string source = @"
                local a = {1, 2, 3}
                a[3] = nil
                return #a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2);
        }

        [Test]
        public void NilAssignment_KeyReadsAsNil()
        {
            string source = @"
                local a = {}
                a.x = 5
                a.x = nil
                return a.x";

            var vm = Utils.Run(source, 100);
            Utils.AssertNilResult(vm);
        }

        [Test]
        public void Constructor_TrailingCallExpandsIntoList()
        {
            // A call as the last constructor field spreads all its results into
            // consecutive array slots: {f()} == {1, 2, 3}.
            string source = @"
            local function f()
                return 1, 2, 3
            end
            local a = {f()}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(1));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(2));
            Utils.AssertTableResult(vm, new NumberObject(3), new NumberObject(3));
        }

        [Test]
        public void Constructor_TrailingCallAfterFixedField()
        {
            // Fixed fields keep their slots; the trailing call fills the rest:
            // {10, f()} == {10, 1, 2, 3}.
            string source = @"
            local function f()
                return 1, 2, 3
            end
            local a = {10, f()}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(10));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(1));
            Utils.AssertTableResult(vm, new NumberObject(3), new NumberObject(2));
            Utils.AssertTableResult(vm, new NumberObject(4), new NumberObject(3));
        }

        [Test]
        public void Constructor_NonTrailingCallTruncatesToOne()
        {
            // A call that is not the last field contributes exactly one value:
            // {f(), 10} == {1, 10}.
            string source = @"
            local function f()
                return 1, 2, 3
            end
            local a = {f(), 10}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(1));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(10));
        }

        [Test]
        public void Constructor_TrailingCallLength()
        {
            // The expanded results count toward the table length: #{f()} == 3.
            string source = @"
            local function f()
                return 1, 2, 3
            end
            local a = {f()}
            return #a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void NestedTables()
        {
            string source = @"
            local a = {
                inner = { 1, 2, 3}
            }
            local x = a.inner[2]
            return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2);
        }
    }
}
