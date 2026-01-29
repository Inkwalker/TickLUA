namespace TickLUA_Tests.LUA
{
    internal class Logic
    {
        [Test]
        public void Less()
        {
            var source =
                @"local x = 2
                  local y = 5
                  local r1 = x < y
                  local r2 = y < x
                  local r3 = x < x
                  return r1, r2, r3";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, false, 1);
            Utils.AssertBoolResult(vm, false, 2);
        }

        [Test]
        public void LessOrEqual()
        {
            var source =
                @"local x = 2
                  local y = 5
                  local r1 = x <= y
                  local r2 = y <= x
                  local r3 = x <= x
                  return r1, r2, r3";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, false, 1);
            Utils.AssertBoolResult(vm, true, 2);
        }

        [Test]
        public void Greater()
        {
            var source =
                @"local x = 2
                  local y = 5
                  local r1 = x > y
                  local r2 = y > x
                  local r3 = x > x
                  return r1, r2, r3";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertBoolResult(vm, true, 1);
            Utils.AssertBoolResult(vm, false, 2);
        }

        [Test]
        public void GreaterOrEqual()
        {
            var source =
                @"local x = 2
                  local y = 5
                  local r1 = x >= y
                  local r2 = y >= x
                  local r3 = x >= x
                  return r1, r2, r3";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertBoolResult(vm, true, 1);
            Utils.AssertBoolResult(vm, true, 2);
        }

        [Test]
        public void Equal()
        {
            var source =
                @"local x = 2
                  local y = 5
                  local r1 = x == y
                  local r2 = y == x
                  local r3 = x == x
                  return r1, r2, r3";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertBoolResult(vm, false, 1);
            Utils.AssertBoolResult(vm, true, 2);
        }

        [Test]
        public void NotEqual()
        {
            var source =
                @"local x = 2
                  local y = 5
                  local r1 = x != y
                  local r2 = y != x
                  local r3 = x != x
                  return r1, r2, r3";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, true, 1);
            Utils.AssertBoolResult(vm, false, 2);
        }

        [Test]
        public void Not()
        {
            var source =
                @"local x = true
                  local y = false
                  local a = 5
                  local b = nil
                  local r1 = not x
                  local r2 = not y
                  local r3 = not a
                  local r4 = not b
                  return r1, r2, r3, r4";
            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertBoolResult(vm, true, 1);
            Utils.AssertBoolResult(vm, false, 2);
            Utils.AssertBoolResult(vm, true, 3);
        }

        [Test]
        public void And()
        {
            var source =
                @"local x = true
                  local y = false
                  local r1 = x and y
                  local r2 = y and x
                  local r3 = x and x
                  local r4 = y and y
                  return r1, r2, r3, r4";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertBoolResult(vm, false, 1);
            Utils.AssertBoolResult(vm, true, 2);
            Utils.AssertBoolResult(vm, false, 3);
        }

        [Test]
        public void Or()
        {
            var source =
                @"local x = true
                  local y = false
                  local r1 = x or y
                  local r2 = y or x
                  local r3 = x or x
                  local r4 = y or y
                  return r1, r2, r3, r4";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, true, 1);
            Utils.AssertBoolResult(vm, true, 2);
            Utils.AssertBoolResult(vm, false, 3);
        }

        [Test]
        public void TernaryOperator_Max()
        {
            var source =
                @"local x = 5
                  local y = 10
                  local r1 = (x > y) and x or y
                  return r1";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 10, 0);
        }

        [Test]
        public void TernaryOperator_NilCheck()
        {
            var source =
                @"local x = nil
                  local y = 10
                  local r1 = x or y
                  return r1";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 10, 0);
        }
    }
}
