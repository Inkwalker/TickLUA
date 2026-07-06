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
        public void Constructor_TrailingCallWithArgsExpands()
        {
            // The trailing call takes arguments; its func slot sits at the top of the
            // array block so the args land where CALL reads them, and all results expand:
            // {mk(10)} == {10, 11, 12}.
            string source = @"
            local function mk(n)
                return n, n + 1, n + 2
            end
            local a = {mk(10)}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(10));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(11));
            Utils.AssertTableResult(vm, new NumberObject(3), new NumberObject(12));
        }

        [Test]
        public void Constructor_KeyedFieldBeforeTrailingCall()
        {
            // A keyed field interspersed before the trailing call must not disturb the
            // array slots: {5, x = 99, f()} == {5, 1, 2, 3} plus x = 99.
            string source = @"
            local function f()
                return 1, 2, 3
            end
            local a = {5, x = 99, f()}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(5));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(1));
            Utils.AssertTableResult(vm, new NumberObject(3), new NumberObject(2));
            Utils.AssertTableResult(vm, new NumberObject(4), new NumberObject(3));
            Utils.AssertTableResult(vm, new StringObject("x"), new NumberObject(99));
        }

        [Test]
        public void Constructor_TwoCallsOnlyLastExpands()
        {
            // Non-last call truncates to one; the last call expands:
            // {f(), f()} == {1, 1, 2, 3}.
            string source = @"
            local function f()
                return 1, 2, 3
            end
            local a = {f(), f()}
            return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(1));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(1));
            Utils.AssertTableResult(vm, new NumberObject(3), new NumberObject(2));
            Utils.AssertTableResult(vm, new NumberObject(4), new NumberObject(3));
        }

        [Test]
        public void Constructor_TrailingCallNoResults()
        {
            // A trailing call that returns nothing contributes no elements:
            // {7, nothing()} == {7}.
            string source = @"
            local function nothing()
                return
            end
            local a = {7, nothing()}
            return #a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1);
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
