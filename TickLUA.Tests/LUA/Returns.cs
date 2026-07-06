namespace TickLUA_Tests.LUA
{
    internal class Returns
    {
        [Test]
        public void ReturnNothing()
        {
            string source = @"
                return";

            var vm = Utils.Run(source, 100);

            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 0);
        }

        [Test]
        public void ReturnOne()
        {
            string source = @"
                return 5";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 1);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void ReturnMulti()
        {
            string source = @"
                return 1, 2, 3";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 3);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void ReturnCall_TrailingCallExpandsAllResults()
        {
            string source = @"
                local function f()
                    return 42, 43
                end
                return f()";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 2);
            Utils.AssertIntegerResult(vm, 42, 0);
            Utils.AssertIntegerResult(vm, 43, 1);
        }

        [Test]
        public void ReturnCall_TrailingCallAfterFixedValues()
        {
            string source = @"
                local function f()
                    return 42, 43
                end
                return 5, f()";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 3);
            Utils.AssertIntegerResult(vm, 5, 0);
            Utils.AssertIntegerResult(vm, 42, 1);
            Utils.AssertIntegerResult(vm, 43, 2);
        }

        [Test]
        public void ReturnCall_NonTrailingCallTruncatesToOne()
        {
            string source = @"
                local function f()
                    return 42, 43
                end
                return f(), 5";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 2);
            Utils.AssertIntegerResult(vm, 42, 0);
            Utils.AssertIntegerResult(vm, 5, 1);
        }

        [Test]
        public void ReturnCall_ParenthesizedCallTruncatesToOne()
        {
            string source = @"
                local function f()
                    return 42, 43
                end
                return (f())";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 1);
            Utils.AssertIntegerResult(vm, 42, 0);
        }

        [Test]
        public void ReturnCall_NonTrailingCallWithArgTruncatesToOne()
        {
            // A call with arguments in a non-last return position must still be
            // truncated to a single value, and its argument registers must not
            // collide with the surrounding return slots: return id(7), 5 == 7, 5.
            string source = @"
                local function id(n)
                    return n
                end
                return id(7), 5";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 2);
            Utils.AssertIntegerResult(vm, 7, 0);
            Utils.AssertIntegerResult(vm, 5, 1);
        }

        [Test]
        public void ReturnCall_TwoNonTrailingCallsWithArgs()
        {
            // Two calls with arguments, both truncated to one value, in fixed
            // positions: return id(7), id(8), 9 == 7, 8, 9.
            string source = @"
                local function id(n)
                    return n
                end
                return id(7), id(8), 9";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 3);
            Utils.AssertIntegerResult(vm, 7, 0);
            Utils.AssertIntegerResult(vm, 8, 1);
            Utils.AssertIntegerResult(vm, 9, 2);
        }

        [Test]
        public void ReturnCall_TrailingCallWithArgAfterNonTrailingCall()
        {
            // Non-trailing call truncates to one; trailing call expands fully:
            // return id(7), pair() == 7, 2, 3.
            string source = @"
                local function id(n)
                    return n
                end
                local function pair()
                    return 2, 3
                end
                return id(7), pair()";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 3);
            Utils.AssertIntegerResult(vm, 7, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void ReturnCall_ChainedThroughTwoFunctions()
        {
            string source = @"
                local function f()
                    return 1, 2, 3
                end
                local function g()
                    return f()
                end
                return g()";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 3);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void Assignment_TrailingCallFillsVariables()
        {
            string source = @"
                local function f()
                    return 42, 43
                end
                local a, b = f()
                return b, a";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 2);
            Utils.AssertIntegerResult(vm, 43, 0);
            Utils.AssertIntegerResult(vm, 42, 1);
        }

        [Test]
        public void Assignment_TrailingCallWithArgsFillsVariables()
        {
            string source = @"
                local function f(n)
                    return n, n + 1
                end
                local a, b = f(10)
                return b, a";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 2);
            Utils.AssertIntegerResult(vm, 11, 0);
            Utils.AssertIntegerResult(vm, 10, 1);
        }

        [Test]
        public void Assignment_TrailingCallPadsWithNil()
        {
            string source = @"
                local function f()
                    return 42
                end
                local a, b, c = f()
                return a, b, c";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 3);
            Utils.AssertIntegerResult(vm, 42, 0);
            Utils.AssertNilResult(vm, 1);
            Utils.AssertNilResult(vm, 2);
        }

        [Test]
        public void Assignment_ExtraValuesAfterCallDiscarded()
        {
            string source = @"
                local function f()
                    return 42, 43
                end
                local a = f()
                return a";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 1);
            Utils.AssertIntegerResult(vm, 42, 0);
        }

        [Test]
        public void ReturnMultiCopy()
        {
            string source = @"
                local a = 1
                local b = 2
                local c = 3
                return c, b, a";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 3);
            Utils.AssertIntegerResult(vm, 3, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 1, 2);
        }
    }
}
