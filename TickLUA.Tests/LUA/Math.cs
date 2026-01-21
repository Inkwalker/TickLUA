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
    }
}
