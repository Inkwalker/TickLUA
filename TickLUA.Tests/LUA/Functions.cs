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
    }
}
