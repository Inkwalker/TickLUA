using TickLUA.Compilers.LUA;

namespace TickLUA_Tests
{
    internal class MemoryLimitTests
    {
        private static TickVM Compile(string source, TickVMOptions options)
        {
            return new TickVM(LuaCompiler.Compile(source), options);
        }

        private static RuntimeException AssertThrowsOutOfMemory(TickVM vm)
        {
            var ex = Assert.Throws<RuntimeException>(() =>
            {
                for (int i = 0; i < 1_000_000 && !vm.IsFinished; i++)
                    vm.Tick();
            });
            Assert.That(ex.Message, Is.EqualTo("not enough memory"));
            return ex;
        }

        [Test]
        public void Default_MemoryUnlimited()
        {
            // No limit: the ledger is inactive and large allocations just work.
            string source = @"
                local t = {}
                for i = 1, 2000 do t[i] = 'value' .. i end
                return #t";

            var vm = Utils.Run(source, 200_000);
            Utils.AssertIntegerResult(vm, 2000);
            Assert.That(vm.EstimatedMemoryBytes, Is.EqualTo(0));
        }

        [Test]
        public void Limit_UnderLimitFine()
        {
            string source = @"
                local t = {}
                for i = 1, 100 do t[i] = i end
                return #t";

            var vm = Compile(source, new TickVMOptions { MaxMemoryBytes = 256 * 1024 });
            Utils.Run(vm, 100_000);
            Utils.AssertIntegerResult(vm, 100);
            // 100 entries at 32 bytes each, at minimum.
            Assert.That(vm.EstimatedMemoryBytes, Is.GreaterThanOrEqualTo(3200));
        }

        [Test]
        public void Limit_TableGrowthThrows()
        {
            string source = @"
                local t = {}
                local i = 1
                while true do
                    t[i] = i
                    i = i + 1
                end";

            var vm = Compile(source, new TickVMOptions { MaxMemoryBytes = 16 * 1024 });
            AssertThrowsOutOfMemory(vm);
        }

        [Test]
        public void Limit_StringGrowthThrows()
        {
            // s doubles every iteration; the CONCAT precheck must stop it at
            // the limit instead of letting one allocation blow far past it.
            string source = @"
                local s = 'x'
                while true do s = s .. s end";

            var vm = Compile(source, new TickVMOptions { MaxMemoryBytes = 64 * 1024 });
            AssertThrowsOutOfMemory(vm);
        }

        [Test]
        public void Limit_PcallCatchesOutOfMemory()
        {
            string source = @"
                local ok, err = pcall(function()
                    local t = {}
                    local i = 1
                    while true do
                        t[i] = 'entry' .. i
                        i = i + 1
                    end
                end)
                return ok, err";

            var vm = Compile(source, new TickVMOptions { MaxMemoryBytes = 16 * 1024 });
            Utils.Run(vm, 1_000_000);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "not enough memory", 1);
        }

        [Test]
        public void Limit_ScanReclaimsDroppedData()
        {
            // Each cycle builds ~32 KB of table entries and then drops the
            // table. Cumulative ledger charges far exceed the limit, so this
            // only passes if the correction scan notices the dropped cycles
            // are unreachable and rebaselines.
            string source = @"
                for cycle = 1, 10 do
                    local t = {}
                    for i = 1, 1000 do t[i] = i end
                end
                return 42";

            var vm = Compile(source, new TickVMOptions { MaxMemoryBytes = 64 * 1024 });
            Utils.Run(vm, 1_000_000);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void Limit_StringBytesAreCounted()
        {
            // Anchoring a ~10 KB string must move the estimate by at least
            // its character bytes.
            string source = @"
                local s = 'aaaaaaaaaaaaaaaa'
                for i = 1, 9 do s = s .. s end -- 16 * 2^9 = 8192 chars
                local t = { s }
                return #s";

            var vm = Compile(source, new TickVMOptions { MaxMemoryBytes = 1024 * 1024 });
            Utils.Run(vm, 100_000);
            Utils.AssertIntegerResult(vm, 8192);
            Assert.That(vm.EstimatedMemoryBytes, Is.GreaterThanOrEqualTo(2 * 8192));
        }

        [Test]
        public void Limit_CoroutineMemoryCounted()
        {
            // Growth inside a coroutine is charged like any other: the body
            // dies with "not enough memory" and resume reports it.
            string source = @"
                local co = coroutine.create(function()
                    local t = {}
                    local i = 1
                    while true do
                        t[i] = i
                        i = i + 1
                    end
                end)
                local ok, err = coroutine.resume(co)
                return ok, err";

            var vm = Compile(source, new TickVMOptions { MaxMemoryBytes = 16 * 1024 });
            Utils.Run(vm, 1_000_000);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "not enough memory", 1);
        }

        [Test]
        public void Limit_MustBePositive()
        {
            var func = LuaCompiler.Compile("return 1");
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new TickVM(func, new TickVMOptions { MaxMemoryBytes = 0 }));
        }
    }
}
