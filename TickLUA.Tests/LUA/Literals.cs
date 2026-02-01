using TickLUA.Compilers;

namespace TickLUA_Tests.LUA
{
    internal class Literals
    {
        [Test]
        public void Integers()
        {
            var source =
                @"local x = 2
                  local y = 5
                  return x, y";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2, 0);
            Utils.AssertIntegerResult(vm, 5, 1);
        }

        [Test]
        public void HexIntegers()
        {
            var source =
                @"local x = 0x2
                  local y = 0xFF
                  return x, y";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2, 0);
            Utils.AssertIntegerResult(vm, 255, 1);
        }

        [Test]
        public void Floats()
        {
            var source =
                @"local x = 2.5
                  local y = 5.5
                  return x, y";

            var vm = Utils.Run(source, 100);
            Utils.AssertFloatResult(vm, 2.5f, 0);
            Utils.AssertFloatResult(vm, 5.5f, 1);
        }

        [Test]
        public void HexFloats()
        {
            var source =
                @"local x = 0x1.fp10
                  local y = 0x0.1E
                  return x, y";

            var vm = Utils.Run(source, 100);
            Utils.AssertFloatResult(vm, 1984, 0);
            Utils.AssertFloatResult(vm, 0.1171875f, 1);
        }

        [Test]
        public void Exponents()
        {
            var source =
                @"local x = 0x1.fp-2
                  local y = 2.5e-1
                  local z = 3.5e+2
                  return x, y, z";

            var vm = Utils.Run(source, 100);
            Utils.AssertFloatResult(vm, 0.484375f, 0);
            Utils.AssertFloatResult(vm, 0.25f, 1);
            Utils.AssertFloatResult(vm, 350f, 2);
        }

        [Test]
        public void InvalidLiterals()
        {
            var source =
                @"local x = 0x1.fe-2
                  local y = 2.5p-1
                  local z = 3.5.4";

            Assert.Catch<CompilationException>(() => Utils.Run(source, 100), null);
        }

        [Test]
        public void Booleans()
        {
            var source =
                @"local x = true
                  local y = false
                  return x, y";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, false, 1);
        }

        [Test]
        public void Nil()
        {
            var source =
                @"local x = nil
                  return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertNilResult(vm);
        }
    }
}
