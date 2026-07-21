using TickLUA.Compilers.LUA;

namespace TickLUA_Tests.LUA
{
    internal class Functions
    {
        [Test]
        public void FunctionDef()
        {
            string source = @"
                local function a()
                end
                return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertClosureResult(vm);
        }

        [Test]
        public void FunctionCall()
        {
            string source = @"
                local function a()
                    return 42
                end
                return a()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void FunctionCall_Multi()
        {
            string source = @"
                local function a()
                    return 42, 43
                end
                return a()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42, 0);
            Utils.AssertIntegerResult(vm, 43, 1);
        }

        [Test]
        public void FunctionCall_WithParam()
        {
            string source = @"
                local function inc(n)
                    return n + 1
                end
                return inc(41)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void FunctionCall_MultipleParams()
        {
            string source = @"
                local function add(a, b)
                    return a + b
                end
                return add(40, 2)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void CallArg_TrailingCallExpandsIntoParams()
        {
            // A call in the last argument position spreads all its results into the
            // callee's parameters: add(pair()) == add(2, 3).
            string source = @"
                local function pair()
                    return 2, 3
                end
                local function add(a, b)
                    return a + b
                end
                return add(pair())";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void CallArg_TrailingCallAfterFixedArg()
        {
            // Fixed args keep their positions; the trailing call fills the rest.
            // add3 reads all three params so the expanded result is observable:
            // add3(10, pair()) == add3(10, 2, 3) == 15.
            string source = @"
                local function pair()
                    return 2, 3
                end
                local function add3(a, b, c)
                    return a + b + c
                end
                return add3(10, pair())";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 15);
        }

        [Test]
        public void CallArg_NonTrailingCallTruncatesToOne()
        {
            // A call that is not the last argument contributes exactly one value:
            // add(pair(), 10) == add(2, 10).
            string source = @"
                local function pair()
                    return 2, 3
                end
                local function add(a, b)
                    return a + b
                end
                return add(pair(), 10)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 12);
        }

        [Test]
        public void CallArg_ParenthesizedCallTruncatesToOne()
        {
            // Parentheses adjust a call to a single value even in trailing position:
            // add((pair()), 10) == add(2, 10).
            string source = @"
                local function pair()
                    return 2, 3
                end
                local function add(a, b)
                    return a + b
                end
                return add((pair()), 10)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 12);
        }

        [Test]
        public void ParamMutation_DoesNotAffectCaller()
        {
            // Arguments are copied by value; mutating a parameter must not change the
            // caller's variable.
            string source = @"
                local function f(a)
                    a = a + 100
                    return a
                end
                local x = 5
                f(x)
                return x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void ClosureOverParam()
        {
            // A closure capturing a parameter must keep its own independent state,
            // unaffected by the caller reusing registers after the call returns.
            string source = @"
                local function counter(n)
                    return function()
                        n = n + 1
                        return n
                    end
                end
                local c = counter(10)
                return c()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 11);
        }

        [Test]
        public void ClosureForLoop()
        {
            // for loops in lua create a new external value each iteration.
            // all closures must have their own value captured
            string source = @"
                local funcs = {}

                for i = 1, 3 do
                    funcs[i] = function()
                        return i
                    end
                end

                return funcs[1](), funcs[2](), funcs[3]()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void ClosureWhileLoop()
        {
            // Each while iteration declares a fresh local; every closure must capture
            // its own copy.
            string source = @"
                local funcs = {}
                local i = 0
                while i < 3 do
                    i = i + 1
                    local j = i
                    funcs[i] = function()
                        return j
                    end
                end
                return funcs[1](), funcs[2](), funcs[3]()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void MethodDefinitionAndCall()
        {
            string source = @"
                local t = { value = 40 }
                function t:add(n)
                    return self.value + n
                end
                return t:add(2)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void MethodCall_AsStatement()
        {
            string source = @"
                local t = { x = 0 }
                function t:set(n)
                    self.x = n
                end
                t:set(42)
                return t.x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void MethodCall_ObjectEvaluatedOnce()
        {
            // 'get():add(2)' must evaluate the object expression exactly once,
            // reusing it as the implicit self argument.
            string source = @"
                local calls = 0
                local t = { value = 40 }
                function t:add(n)
                    return self.value + n
                end
                local function get()
                    calls = calls + 1
                    return t
                end
                local r = get():add(2)
                return r, calls";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42, 0);
            Utils.AssertIntegerResult(vm, 1, 1);
        }

        [Test]
        public void MethodCall_Chained()
        {
            string source = @"
                local t = { child = { value = 41 } }
                function t:get()
                    return self.child
                end
                function t.child:inc()
                    return self.value + 1
                end
                return t:get():inc()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void MethodCall_TrailingCallExpandsIntoParams()
        {
            // A call in the last argument position of a method call spreads its
            // results after the fixed args: t:add3(10, pair()) == t:add3(10, 2, 3).
            string source = @"
                local t = { value = 10 }
                function t:add3(a, b, c)
                    return self.value + a + b + c
                end
                local function pair()
                    return 2, 3
                end
                return t:add3(10, pair())";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 25);
        }

        [Test]
        public void MethodCall_MultipleResults()
        {
            string source = @"
                local t = { value = 1 }
                function t:pair()
                    return self.value, self.value + 1
                end
                return t:pair()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
        }

        [Test]
        public void Varargs_Forwarded()
        {
            string source = @"
                local function f(...)
                    return ...
                end
                return f(1, 2, 3)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void Varargs_Count()
        {
            // No select() builtin yet (needs globals + native functions), so count
            // via a table constructor instead; equivalent for non-nil arguments.
            string source = @"
                local function count(...)
                    local t = {...}
                    return #t
                end
                return count(10, 20, 30, 40)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 4);
        }

        [Test]
        public void ProperTailCall_DeepRecursion()
        {
            string source = @"
                local function count(n)
                    if n == 0 then
                        return 'done'
                    end
                    return count(n - 1)
                end
                return count(100000)";

            var vm = Utils.Run(source, 2000000);
            Utils.AssertStringResult(vm, "done");
        }

        [Test]
        public void ProperTailCall_ConstantStackDepth()
        {
            // Each 'return count(n - 1)' must replace the frame, not stack on it.
            string source = @"
                local function count(n)
                    if n == 0 then
                        return 'done'
                    end
                    return count(n - 1)
                end
                return count(1000)";

            var lua_function = LuaCompiler.Compile(source);
            var vm = Utils.Load(lua_function);

            int max_depth = 0;
            int ticks = 0;
            while (!vm.IsFinished)
            {
                vm.Tick();
                max_depth = System.Math.Max(max_depth, vm.CallStackDepth);

                if (++ticks > 100000)
                    Assert.Fail("VM did not finish execution within 100000 ticks.");
            }

            Utils.AssertStringResult(vm, "done");
            Assert.That(max_depth, Is.LessThanOrEqualTo(2), "tail calls must not grow the call stack");
        }

        [Test]
        public void ProperTailCall_MutualRecursion()
        {
            string source = @"
                local is_odd
                local function is_even(n)
                    if n == 0 then return true end
                    return is_odd(n - 1)
                end
                is_odd = function(n)
                    if n == 0 then return false end
                    return is_even(n - 1)
                end
                return is_even(100000)";

            var vm = Utils.Run(source, 2000000);
            Utils.AssertBoolResult(vm, true);
        }

        [Test]
        public void TailCall_Method()
        {
            string source = @"
                local obj = { value = 10 }
                obj.get = function(self, extra)
                    return self.value + extra
                end
                local function f()
                    return obj:get(5)
                end
                return f()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 15);
        }

        [Test]
        public void TailCall_UnderPcall_Success()
        {
            // The tail-called frame inherits the pcall wrapper's sink, so the
            // results still get the pcall 'true' prepended.
            string source = @"
                local function inner() return 1, 2 end
                local ok, a, b = pcall(function() return inner() end)
                return ok, a, b";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertIntegerResult(vm, 1, 1);
            Utils.AssertIntegerResult(vm, 2, 2);
        }

        [Test]
        public void TailCall_UnderPcall_Error()
        {
            // The tail-called frame inherits the pcall wrapper's error sink, so
            // the pcall boundary survives the frame replacement.
            string source = @"
                local function boom() error('kaboom') end
                local ok, err = pcall(function() return boom() end)
                return ok, err";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "kaboom", 1);
        }

        [Test]
        public void TailCall_ToNativeFunction()
        {
            // assert is a plain native: the tail call runs it synchronously and
            // delivers its results through the replaced frame's sink.
            string source = @"
                local function f()
                    return assert(42)
                end
                return f()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void TailCall_ToVmAwareNative()
        {
            // pcall is a VM-aware native: TAILCALL cannot replace the frame and
            // falls back to CALL semantics, finished by the trailing RETURN.
            string source = @"
                local function risky() return 7 end
                local function wrap() return pcall(risky) end
                return wrap()";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertIntegerResult(vm, 7, 1);
        }

        [Test]
        public void TailCall_ToCallableTable()
        {
            // A __call callee cannot replace the frame either; the fallback path
            // dispatches the metamethod and the trailing RETURN finishes up.
            string source = @"
                local t = setmetatable({}, { __call = function(self, x) return x * 2 end })
                local function f() return t(21) end
                return f()";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void TailCall_WithVarargs()
        {
            string source = @"
                local function sum(a, b, c) return a + b + c end
                local function pass(...) return sum(...) end
                return pass(1, 2, 3)";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 6);
        }
    }
}
