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
            // select('#', ...) counts the variadic arguments.
            string source = @"
                local function count(...)
                    return select('#', ...)
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
    }
}
