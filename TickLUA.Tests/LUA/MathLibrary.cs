using TickLUA.VM.Objects;

namespace TickLUA_Tests.LUA
{
    /// <summary>
    /// The math library as scripts see it. Values are floats, so comparisons of
    /// irrational results go through a tolerance rather than exact equality.
    /// </summary>
    internal class MathLibrary
    {
        private const float Tolerance = 1e-5f;

        private static void AssertApproxResult(TickVM vm, double expected, int result_index = 0)
        {
            Assert.NotNull(vm.ExecutionResult);
            Assert.That(vm.ExecutionResult.Length, Is.GreaterThan(result_index), "Not enough results");
            Assert.IsInstanceOf<NumberObject>(vm.ExecutionResult[result_index]);
            var answer = (NumberObject)vm.ExecutionResult[result_index];

            Assert.That(answer.Value, Is.EqualTo((float)expected).Within(Tolerance));
        }

        [Test]
        public void Abs()
        {
            var vm = Utils.Run("return math.abs(-5), math.abs(5), math.abs(-2.5)", 100);
            Utils.AssertIntegerResult(vm, 5, 0);
            Utils.AssertIntegerResult(vm, 5, 1);
            Utils.AssertFloatResult(vm, 2.5f, 2);
        }

        [Test]
        public void FloorAndCeil()
        {
            var vm = Utils.Run(
                "return math.floor(3.7), math.ceil(3.2), math.floor(-3.2), math.ceil(-3.7)", 100);
            Utils.AssertIntegerResult(vm, 3, 0);
            Utils.AssertIntegerResult(vm, 4, 1);
            Utils.AssertIntegerResult(vm, -4, 2);
            Utils.AssertIntegerResult(vm, -3, 3);
        }

        [Test]
        public void SqrtAndPowAndExp()
        {
            var vm = Utils.Run("return math.sqrt(16), math.pow(2, 10), math.exp(0)", 100);
            Utils.AssertIntegerResult(vm, 4, 0);
            Utils.AssertIntegerResult(vm, 1024, 1);
            Utils.AssertIntegerResult(vm, 1, 2);
        }

        [Test]
        public void Logarithms()
        {
            var vm = Utils.Run("return math.log(math.exp(2)), math.log10(1000)", 100);
            AssertApproxResult(vm, 2.0, 0);
            AssertApproxResult(vm, 3.0, 1);
        }

        [Test]
        public void Trigonometry()
        {
            var vm = Utils.Run(
                "return math.sin(0), math.cos(0), math.tan(0), math.sin(math.pi / 2)", 100);
            AssertApproxResult(vm, 0.0, 0);
            AssertApproxResult(vm, 1.0, 1);
            AssertApproxResult(vm, 0.0, 2);
            AssertApproxResult(vm, 1.0, 3);
        }

        [Test]
        public void InverseTrigonometry()
        {
            var vm = Utils.Run(
                "return math.asin(1), math.acos(1), math.atan(1), math.atan2(1, 1)", 100);
            AssertApproxResult(vm, System.Math.PI / 2, 0);
            AssertApproxResult(vm, 0.0, 1);
            AssertApproxResult(vm, System.Math.PI / 4, 2);
            AssertApproxResult(vm, System.Math.PI / 4, 3);
        }

        [Test]
        public void Atan2_UsesBothSignsToPickTheQuadrant()
        {
            // The pair (-1,-1) is what separates atan2 from atan(y/x): the ratio
            // is the same as (1,1), only the quadrant differs.
            var vm = Utils.Run("return math.atan2(-1, -1)", 100);
            AssertApproxResult(vm, -3 * System.Math.PI / 4);
        }

        [Test]
        public void DegAndRad()
        {
            var vm = Utils.Run("return math.deg(math.pi), math.rad(180)", 100);
            AssertApproxResult(vm, 180.0, 0);
            AssertApproxResult(vm, System.Math.PI, 1);
        }

        [Test]
        public void MaxAndMin()
        {
            var vm = Utils.Run(
                "return math.max(3), math.max(1, 7, 3), math.min(1, 7, 3), math.min(-1, -7)", 100);
            Utils.AssertIntegerResult(vm, 3, 0);
            Utils.AssertIntegerResult(vm, 7, 1);
            Utils.AssertIntegerResult(vm, 1, 2);
            Utils.AssertIntegerResult(vm, -7, 3);
        }

        [Test]
        public void Modf_SplitsKeepingTheSign()
        {
            var vm = Utils.Run("return math.modf(3.7)", 100);
            Utils.AssertIntegerResult(vm, 3, 0);
            AssertApproxResult(vm, 0.7, 1);

            vm = Utils.Run("return math.modf(-3.7)", 100);
            Utils.AssertIntegerResult(vm, -3, 0);
            AssertApproxResult(vm, -0.7, 1);
        }

        [Test]
        public void Modf_OfInfinityHasNoFractionalPart()
        {
            var vm = Utils.Run("local i, f = math.modf(math.huge) return f, i == math.huge", 100);
            Utils.AssertIntegerResult(vm, 0, 0);
            Utils.AssertBoolResult(vm, true, 1);
        }

        [Test]
        public void Constants()
        {
            var vm = Utils.Run("return math.pi, math.huge, -math.huge < 0, math.huge > 1e30", 100);
            AssertApproxResult(vm, System.Math.PI, 0);
            Utils.AssertFloatResult(vm, float.PositiveInfinity, 1);
            Utils.AssertBoolResult(vm, true, 2);
            Utils.AssertBoolResult(vm, true, 3);
        }

        [Test]
        public void Random_NoArgumentsStaysInTheUnitInterval()
        {
            var source = @"
                for i = 1, 100 do
                    local r = math.random()
                    if r < 0 or r >= 1 then return false end
                end
                return true";

            Utils.AssertBoolResult(Utils.Run(source, 10000), true);
        }

        [Test]
        public void Random_OneArgumentIsAnIntegerInOneToM()
        {
            var source = @"
                for i = 1, 200 do
                    local r = math.random(6)
                    if r < 1 or r > 6 or math.floor(r) ~= r then return false end
                end
                return true";

            Utils.AssertBoolResult(Utils.Run(source, 20000), true);
        }

        [Test]
        public void Random_TwoArgumentsAreAnIntegerInTheRange()
        {
            var source = @"
                for i = 1, 200 do
                    local r = math.random(-3, 3)
                    if r < -3 or r > 3 or math.floor(r) ~= r then return false end
                end
                return true";

            Utils.AssertBoolResult(Utils.Run(source, 20000), true);
        }

        [Test]
        public void Random_EmptyIntervalIsAnError()
        {
            var vm = Utils.Run("local ok, err = pcall(math.random, 3, 1) return ok, err", 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "bad argument #2 to 'random' (interval is empty)", 1);
        }

        [Test]
        public void Randomseed_SameSeedReplaysTheSameSequence()
        {
            var source = @"
                math.randomseed(42)
                local a, b = math.random(1000), math.random(1000)
                math.randomseed(42)
                local c, d = math.random(1000), math.random(1000)
                return a == c and b == d";

            Utils.AssertBoolResult(Utils.Run(source, 1000), true);
        }

        [Test]
        public void Randomseed_DifferentSeedsDiverge()
        {
            var source = @"
                math.randomseed(1)
                local a = {}
                for i = 1, 10 do a[i] = math.random(1000000) end
                math.randomseed(2)
                for i = 1, 10 do
                    if a[i] ~= math.random(1000000) then return true end
                end
                return false";

            Utils.AssertBoolResult(Utils.Run(source, 10000), true);
        }

        [Test]
        public void Random_StreamsAreIndependentPerVM()
        {
            // Two VMs are two worlds: reseeding one must not shift the other,
            // and both start from the same default seed.
            var first = Utils.Run("math.randomseed(7) return math.random(1000000)", 100);
            var second = Utils.Run("return math.random(1000000)", 100);
            var third = Utils.Run("return math.random(1000000)", 100);

            var b = (NumberObject)second.ExecutionResult[0];
            var c = (NumberObject)third.ExecutionResult[0];

            Assert.NotNull(first.ExecutionResult);
            Assert.That(b.Value, Is.EqualTo(c.Value));
        }

        [Test]
        public void BadArgument_ReportsTheFunctionName()
        {
            var vm = Utils.Run("local ok, err = pcall(math.sqrt, 'x') return ok, err", 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "bad argument #1 to 'sqrt' (number expected, got string)", 1);
        }

        [Test]
        public void MissingArgument_IsAnError()
        {
            var vm = Utils.Run("local ok, err = pcall(math.max) return ok, err", 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "bad argument #1 to 'max' (number expected, got no value)", 1);
        }
    }
}
