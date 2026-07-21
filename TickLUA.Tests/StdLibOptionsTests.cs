using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;

namespace TickLUA_Tests
{
    /// <summary>
    /// Optional standard libraries. A disabled library leaves no global behind
    /// at all, so a script can test for it the usual way (<c>if math then</c>).
    /// </summary>
    internal class StdLibOptionsTests
    {
        private static TickVM Run(string source, TickVMOptions options, int tick_limit = 1000)
        {
            var vm = Utils.Load(LuaCompiler.Compile(source), options);
            return Utils.Run(vm, tick_limit);
        }

        [Test]
        public void Default_BothLibrariesArePresent()
        {
            var vm = Utils.Run("return math ~= nil, coroutine ~= nil", 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, true, 1);
        }

        [Test]
        public void EmptyOptions_BothLibrariesArePresent()
        {
            // The defaults live on the options object too, not only on the
            // null-options path.
            var vm = Run("return math ~= nil, coroutine ~= nil", new TickVMOptions());
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, true, 1);
        }

        [Test]
        public void MathDisabled_RemovesOnlyTheMathGlobal()
        {
            var vm = Run("return math, coroutine ~= nil",
                new TickVMOptions { EnableMathLibrary = false });

            Utils.AssertNilResult(vm, 0);
            Utils.AssertBoolResult(vm, true, 1);
        }

        [Test]
        public void MathDisabled_ArithmeticStillWorks()
        {
            // Operators are instructions, not library functions.
            var vm = Run("return 2 ^ 10 + 5 % 3", new TickVMOptions { EnableMathLibrary = false });
            Utils.AssertIntegerResult(vm, 1026);
        }

        [Test]
        public void CoroutineDisabled_RemovesOnlyTheCoroutineGlobal()
        {
            var vm = Run("return coroutine, math ~= nil",
                new TickVMOptions { EnableCoroutineLibrary = false });

            Utils.AssertNilResult(vm, 0);
            Utils.AssertBoolResult(vm, true, 1);
        }

        [Test]
        public void CoroutineDisabled_TheChunkStillRunsOnItsOwnCall()
        {
            // The VM runs every chunk on a coroutine internally; turning the
            // library off takes away the script-facing API, nothing else.
            var vm = Run(@"
                local function f(n) return n * 2 end
                return f(21)", new TickVMOptions { EnableCoroutineLibrary = false });

            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void CoroutineDisabled_CallingIntoItIsANilIndexError()
        {
            var source = @"
                local ok, err = pcall(function() return coroutine.create(function() end) end)
                return ok";

            var vm = Run(source, new TickVMOptions { EnableCoroutineLibrary = false });
            Utils.AssertBoolResult(vm, false);
        }

        [Test]
        public void BothDisabled_BaseLibraryIsUntouched()
        {
            var source = @"
                local t = { 10, 20 }
                local sum = 0
                for _, v in ipairs(t) do sum = sum + v end
                return math, coroutine, sum, pcall ~= nil";

            var vm = Run(source, new TickVMOptions
            {
                EnableMathLibrary = false,
                EnableCoroutineLibrary = false,
            });

            Utils.AssertNilResult(vm, 0);
            Utils.AssertNilResult(vm, 1);
            Utils.AssertIntegerResult(vm, 30, 2);
            Utils.AssertBoolResult(vm, true, 3);
        }

        [Test]
        public void DisabledLibrary_IsAbsentFromGlobalsForTheHostToo()
        {
            var vm = Utils.Load(LuaCompiler.Compile("return 1"),
                new TickVMOptions { EnableMathLibrary = false });

            Assert.IsInstanceOf<NilObject>(vm.Globals["math"]);
            Assert.IsInstanceOf<TableObject>(vm.Globals["coroutine"]);
        }
    }
}
