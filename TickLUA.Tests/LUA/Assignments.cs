using TickLUA.Compilers;

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
        public void Assignment_Add()
        {
            string source = @"
                local a = 5
                a += 5
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 10);
        }

        [Test]
        public void Assignment_Sub()
        {
            string source = @"
                local a = 15
                a -= 5
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 10);
        }

        [Test]
        public void Assignment_Mul()
        {
            string source = @"
                local a = 5
                a *= 5
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 25);
        }

        [Test]
        public void Assignment_Div()
        {
            string source = @"
                local a = 15
                a /= 5
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertFloatResult(vm, 3);
        }

        [Test]
        public void Assignment_iDiv()
        {
            string source = @"
                local a = 32
                a //= 5
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertFloatResult(vm, 6);
        }

        [Test]
        public void Assignment_Mod()
        {
            string source = @"
                local a = 32
                a %= 5
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertFloatResult(vm, 2);
        }

        [Test]
        public void Assignment_Pow()
        {
            string source = @"
                local a = 2
                a ^= 5
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertFloatResult(vm, 32);
        }

        [Test]
        public void MultiAssignment()
        {
            string source = @"
                local a, b = 5, 10
                return a, b";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5, 0);
            Utils.AssertIntegerResult(vm, 10, 1);
        }

        [Test]
        public void MultiAssignment_Nil()
        {
            string source = @"
                local a, b = 5
                return a, b";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5, 0);
            Utils.AssertNilResult(vm, 1);
        }

        [Test]
        public void MultiAssignment_Overflow()
        {
            string source = @"
                local a = 5, 10, 15
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5, 0);
        }

        [Test]
        public void MultiAssignment_Invalid()
        {
            string source = @"
                local a, b += 5
                return a, b";

            Assert.Catch<CompilationException>(() => Utils.Run(source, 100));
        }
    }
}
