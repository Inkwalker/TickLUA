namespace TickLUA_Tests.LUA
{
    internal class Math
    {
        [Test]
        public void Addition()
        {
            var source =
                @"local x = 5
                  local y = 10
                  local r1 = x + y
                  local r2 = y + x
                  return r1, r2";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 15, 0);
            Utils.AssertIntegerResult(vm, 15, 1);
        }

        [Test]
        public void Subtraction()
        {
            var source =
                @"local x = 5
                  local y = 10
                  local r1 = x - y
                  local r2 = y - x
                  return r1, r2";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, -5, 0);
            Utils.AssertIntegerResult(vm, 5, 1);
        }

        [Test]
        public void Multiplication()
        {
            var source =
                @"local x = 5
                  local y = 10
                  local r1 = x * y
                  local r2 = y * x
                  return r1, r2";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 50, 0);
            Utils.AssertIntegerResult(vm, 50, 1);
        }

        [Test]
        public void Division()
        {
            var source =
                @"local x = 32
                  local y = 5
                  local r1 = x / y
                  local r2 = y / x
                  return r1, r2";

            var vm = Utils.Run(source, 100);
            Utils.AssertFloatResult(vm, 6.4f, 0);
            Utils.AssertFloatResult(vm, 0.15625f, 1);
        }

        [Test]
        public void IntegerDivision()
        {
            var source =
                @"local x = 32
                  local y = 5
                  local r1 = x // y
                  local r2 = y // x
                  return r1, r2";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 6, 0);
            Utils.AssertIntegerResult(vm, 0, 1);
        }

        [Test]
        public void Modulus()
        {
            var source =
                @"local x = 32
                  local y = 5
                  local r1 = x % y
                  local r2 = y % x
                  return r1, r2";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2, 0);
            Utils.AssertIntegerResult(vm, 5, 1);
        }

        [Test]
        public void Power()
        {
            var source =
                @"local x = 2
                  local y = 5
                  local r1 = x ^ y
                  local r2 = y ^ x
                  return r1, r2";

            var vm = Utils.Run(source, 100);
            Utils.AssertFloatResult(vm, 32, 0);
            Utils.AssertFloatResult(vm, 25, 1);
        }

        [Test]
        public void UnaryMinus()
        {
            var source =
                @"local x = 2
                  local y = 5.5
                  local r1 = -x
                  local r2 = -y
                  return r1, r2";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, -2, 0);
            Utils.AssertFloatResult(vm, -5.5f, 1);
        }

        [Test]
        public void Concatenation()
        {
            var source =
                @"local x = 'hello '
                  local y = 'world'
                  local r1 = x..y
                  local r2 = y..x
                  return r1, r2";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "hello world", 0);
            Utils.AssertStringResult(vm, "worldhello ", 1);
        }

        [Test]
        public void Precedence_MulOverAdd()
        {
            // '*' binds tighter than '+': 2 + 3 * 4 == 14, not 20
            var vm = Utils.Run("return 2 + 3 * 4", 100);
            Utils.AssertFloatResult(vm, 14);
        }

        [Test]
        public void Precedence_Parentheses()
        {
            // parentheses override precedence: (2 + 3) * 4 == 20
            var vm = Utils.Run("return (2 + 3) * 4", 100);
            Utils.AssertFloatResult(vm, 20);
        }

        [Test]
        public void Precedence_AddSubLeftAssociative()
        {
            // '+'/'-' are left associative: 10 - 3 - 2 == (10 - 3) - 2 == 5
            var vm = Utils.Run("return 10 - 3 - 2", 100);
            Utils.AssertFloatResult(vm, 5);
        }

        [Test]
        public void Precedence_PowRightAssociative()
        {
            // '^' is right associative: 2 ^ 2 ^ 3 == 2 ^ (2 ^ 3) == 2 ^ 8 == 256
            var vm = Utils.Run("return 2 ^ 2 ^ 3", 100);
            Utils.AssertFloatResult(vm, 256);
        }

        [Test]
        public void Precedence_UnaryMinusUnderPow()
        {
            // '^' binds tighter than unary minus: -2 ^ 2 == -(2 ^ 2) == -4
            var vm = Utils.Run("return -2 ^ 2", 100);
            Utils.AssertFloatResult(vm, -4);
        }

        [Test]
        public void StringCoercion_Addition()
        {
            // A string that looks like a number is coerced in arithmetic.
            // See https://www.lua.org/manual/5.4/manual.html#3.4.3
            var vm = Utils.Run("return '10' + 5", 100);
            Utils.AssertFloatResult(vm, 15);
        }

        [Test]
        public void StringCoercion_BothOperands()
        {
            var vm = Utils.Run("return '3' * '4'", 100);
            Utils.AssertFloatResult(vm, 12);
        }
    }
}
