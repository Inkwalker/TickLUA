using TickLUA.VM.Objects;

namespace TickLUA_Tests.LUA
{
    internal class NativeFunctions
    {
        private static NativeFunctionObject Add => new NativeFunctionObject("add", args =>
        {
            float result = args.CheckNumber(0) + args.CheckNumber(1);
            return new LuaObject[] { new NumberObject(result) };
        });

        private static NativeFunctionObject Pair => new NativeFunctionObject("pair", args =>
        {
            return new LuaObject[] { new NumberObject(1), new NumberObject(2) };
        });

        private static NativeFunctionObject Sum => new NativeFunctionObject("sum", args =>
        {
            float total = 0;
            for (int i = 0; i < args.Count; i++)
                total += args.CheckNumber(i);
            return new LuaObject[] { new NumberObject(total) };
        });

        [Test]
        public void Injected_ValueViaVarargs()
        {
            string source = @"return ...";

            var vm = Utils.Run(source, 100, LuaObject.From(42));
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void Call_FixedArgs()
        {
            string source = @"
                local add = ...
                return add(2, 3)";

            var vm = Utils.Run(source, 100, Add);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Call_WrongType_Throws()
        {
            string source = @"
                local add = ...
                return add(2, 'x')";

            var ex = Assert.Throws<RuntimeException>(() => Utils.Run(source, 100, Add));
            Assert.That(ex.Message, Is.EqualTo("bad argument #2 to 'add' (number expected, got string)"));
        }

        [Test]
        public void Call_MissingArg_GotNoValue()
        {
            string source = @"
                local add = ...
                return add(2)";

            var ex = Assert.Throws<RuntimeException>(() => Utils.Run(source, 100, Add));
            Assert.That(ex.Message, Does.Contain("got no value"));
        }

        [Test]
        public void MultiReturn_IntoLocals()
        {
            string source = @"
                local f = ...
                local a, b = f()
                return a, b";

            var vm = Utils.Run(source, 100, Pair);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
        }

        [Test]
        public void MultiReturn_AllResults_InReturn()
        {
            string source = @"
                local f = ...
                return f()";

            var vm = Utils.Run(source, 100, Pair);
            Assert.That(vm.ExecutionResult.Length, Is.EqualTo(2));
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
        }

        [Test]
        public void MultiReturn_TruncatedToOne_WhenNotLast()
        {
            string source = @"
                local f = ...
                return f(), 99";

            var vm = Utils.Run(source, 100, Pair);
            Assert.That(vm.ExecutionResult.Length, Is.EqualTo(2));
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 99, 1);
        }

        [Test]
        public void NoResults_AsStatement_NullReturn()
        {
            bool called = false;
            var native = new NativeFunctionObject("f", args =>
            {
                called = true;
                return null;
            });

            string source = @"
                local f = ...
                f()
                return 7";

            var vm = Utils.Run(source, 100, native);
            Utils.AssertIntegerResult(vm, 7);
            Assert.IsTrue(called);
        }

        [Test]
        public void NoResults_AsStatement_EmptyReturn()
        {
            bool called = false;
            var native = new NativeFunctionObject("f", args =>
            {
                called = true;
                return LuaObject.NoResults;
            });

            string source = @"
                local f = ...
                f()
                return 7";

            var vm = Utils.Run(source, 100, native);
            Utils.AssertIntegerResult(vm, 7);
            Assert.IsTrue(called);
        }

        [Test]
        public void NoResults_NilPadded_InAssignment()
        {
            var native = new NativeFunctionObject("f", args => null);

            string source = @"
                local f = ...
                local a = f()
                return a";

            var vm = Utils.Run(source, 100, native);
            Utils.AssertNilResult(vm);
        }

        [Test]
        public void VariableArgs_ForwardedFromLuaCall()
        {
            string source = @"
                local sum = ...
                local function g()
                    return 1, 2, 3
                end
                return sum(g())";

            var vm = Utils.Run(source, 100, Sum);
            Utils.AssertIntegerResult(vm, 6);
        }

        [Test]
        public void VariableArgs_NativeFeedsNative()
        {
            string source = @"
                local sum, pair = ...
                return sum(pair())";

            var vm = Utils.Run(source, 100, Sum, Pair);
            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void VariableArgs_ViaVarargForwarding()
        {
            string source = @"
                local sum = ...
                local function f(...)
                    return sum(...)
                end
                return f(1, 2, 3)";

            var vm = Utils.Run(source, 100, Sum);
            Utils.AssertIntegerResult(vm, 6);
        }

        [Test]
        public void MultiReturn_InTableConstructor()
        {
            string source = @"
                local pair = ...
                local t = {pair()}
                return #t";

            var vm = Utils.Run(source, 100, Pair);
            Utils.AssertIntegerResult(vm, 2);
        }

        [Test]
        public void Native_IsTruthy()
        {
            string source = @"
                local f = ...
                if f then
                    return 1
                end
                return 0";

            var vm = Utils.Run(source, 100, Pair);
            Utils.AssertIntegerResult(vm, 1);
        }

        [Test]
        public void Native_ResultFeedsArithmetic()
        {
            string source = @"
                local add = ...
                return add(2, 3) * 10";

            var vm = Utils.Run(source, 100, Add);
            Utils.AssertIntegerResult(vm, 50);
        }
    }
}
