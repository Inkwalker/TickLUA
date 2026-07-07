using TickLUA.Compilers;
using TickLUA.Compilers.LUA;

namespace TickLUA_Tests.LUA
{
    internal class Varargs
    {
        [Test]
        public void Forwarded_ToAnotherCall()
        {
            // '...' in the last argument position sends all values into the caller.
            string source = @"
                local function add3(a, b, c)
                    return a + b + c
                end
                local function f(...)
                    return add3(...)
                end
                return f(1, 2, 3)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 6);
        }

        [Test]
        public void TableConstructor()
        {
            string source = @"
                local function pack(...)
                    return {...}
                end
                local t = pack(10, 20, 30)
                return #t, t[1], t[3]";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 3, 0);
            Utils.AssertIntegerResult(vm, 10, 1);
            Utils.AssertIntegerResult(vm, 30, 2);
        }

        [Test]
        public void TableConstructor_AfterFixedElements()
        {
            string source = @"
                local function pack(...)
                    return {0, ...}
                end
                local t = pack(10, 20)
                return #t, t[1], t[2], t[3]";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 3, 0);
            Utils.AssertIntegerResult(vm, 0, 1);
            Utils.AssertIntegerResult(vm, 10, 2);
            Utils.AssertIntegerResult(vm, 20, 3);
        }

        [Test]
        public void LocalAssignment_NilPadded()
        {
            string source = @"
                local function f(...)
                    local a, b = ...
                    return a, b
                end
                return f(1)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertNilResult(vm, 1);
        }

        [Test]
        public void NonTrailing_TruncatesToOne_InReturn()
        {
            string source = @"
                local function f(...)
                    return ..., 99
                end
                return f(1, 2)";

            var vm = Utils.Run(source, 100);
            Assert.That(vm.ExecutionResult.Length, Is.EqualTo(2));
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 99, 1);
        }

        [Test]
        public void NonTrailing_TruncatesToOne_InCallArgs()
        {
            string source = @"
                local function add(a, b)
                    return a + b
                end
                local function f(...)
                    return add(..., 10)
                end
                return f(1, 2)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 11);
        }

        [Test]
        public void Empty_ReturnsNoResults()
        {
            string source = @"
                local function f(...)
                    return ...
                end
                return f()";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.That(vm.ExecutionResult.Length, Is.EqualTo(0));
        }

        [Test]
        public void MixedWithFixedParams()
        {
            string source = @"
                local function f(a, ...)
                    return a, ...
                end
                return f(1, 2, 3)";

            var vm = Utils.Run(source, 100);
            Assert.That(vm.ExecutionResult.Length, Is.EqualTo(3));
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void FewerArgsThanFixedParams()
        {
            // Missing fixed params are nil, varargs are empty.
            string source = @"
                local function f(a, b, ...)
                    local c = ...
                    return a, b, c
                end
                return f(1)";

            var vm = Utils.Run(source, 100);
            Assert.That(vm.ExecutionResult.Length, Is.EqualTo(3));
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertNilResult(vm, 1);
            Utils.AssertNilResult(vm, 2);
        }

        [Test]
        public void Parenthesized_TruncatesToOne()
        {
            string source = @"
                local function f(...)
                    return (...)
                end
                return f(1, 2, 3)";

            var vm = Utils.Run(source, 100);
            Assert.That(vm.ExecutionResult.Length, Is.EqualTo(1));
            Utils.AssertIntegerResult(vm, 1);
        }

        [Test]
        public void TopLevel_ExpandsToNothing()
        {
            // The main chunk is a vararg function invoked with no arguments.
            string source = @"return ...";

            var vm = Utils.Run(source, 100);
            Assert.NotNull(vm.ExecutionResult);
            Assert.That(vm.ExecutionResult.Length, Is.EqualTo(0));
        }

        [Test]
        public void LambdaExpression_AcceptsVarargs()
        {
            string source = @"
                local f = function(...)
                    return ...
                end
                return f(7, 8)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 7, 0);
            Utils.AssertIntegerResult(vm, 8, 1);
        }

        [Test]
        public void Error_OutsideVarargFunction()
        {
            string source = @"
                local function f(a)
                    return ...
                end";

            Assert.Throws<CompilationException>(() => LuaCompiler.Compile(source));
        }

        [Test]
        public void Error_NotLastParameter()
        {
            string source = @"
                local function f(..., a)
                    return a
                end";

            Assert.Throws<CompilationException>(() => LuaCompiler.Compile(source));
        }
    }
}
