using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;

namespace TickLUA_Tests.LUA
{
    internal class Modules
    {
        private static ModuleReaderDelegate Reader(Dictionary<string, string> modules)
            => name => modules.TryGetValue(name, out var source) ? source : null;

        [Test]
        public void Require_ReturnsModuleValue()
        {
            var modules = new Dictionary<string, string>
            {
                ["mod"] = "return { value = 42 }",
            };

            string source = @"
                local m = require('mod')
                return m.value";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void Require_CachesResult_RunsModuleOnce()
        {
            var modules = new Dictionary<string, string>
            {
                ["mod"] = @"
                    count = (count or 0) + 1
                    return { n = count }",
            };

            string source = @"
                local a = require('mod')
                local b = require('mod')
                return rawequal(a, b), count";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertIntegerResult(vm, 1, 1);
        }

        [Test]
        public void Require_NoReturn_YieldsTrue()
        {
            var modules = new Dictionary<string, string>
            {
                ["mod"] = "x = 1",
            };

            string source = @"
                local r = require('mod')
                return r, package.loaded['mod']";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertBoolResult(vm, true, 1);
        }

        [Test]
        public void Require_PassesNameAsVararg()
        {
            var modules = new Dictionary<string, string>
            {
                ["my.module"] = @"
                    local name = ...
                    return name",
            };

            var vm = Utils.Run("return require('my.module')", 1000, Reader(modules));
            Utils.AssertStringResult(vm, "my.module");
        }

        [Test]
        public void Require_Nested()
        {
            var modules = new Dictionary<string, string>
            {
                ["A"] = @"
                    local b = require('B')
                    return { total = b + 5 }",
                ["B"] = "return 10",
            };

            var vm = Utils.Run("return require('A').total", 1000, Reader(modules));
            Utils.AssertIntegerResult(vm, 15);
        }

        [Test]
        public void Require_NotFound_Throws()
        {
            var ex = Assert.Throws<RuntimeException>(
                () => Utils.Run("return require('nope')", 1000, Reader(new Dictionary<string, string>())));
            Assert.That(ex.Message, Is.EqualTo("module 'nope' not found"));
        }

        [Test]
        public void Require_NotFound_CaughtByPcall()
        {
            string source = @"
                local ok, err = pcall(require, 'nope')
                return ok, err";

            var vm = Utils.Run(source, 1000, Reader(new Dictionary<string, string>()));
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "module 'nope' not found", 1);
        }

        [Test]
        public void Require_SuccessThroughPcall()
        {
            var modules = new Dictionary<string, string>
            {
                ["mod"] = "return { value = 42 }",
            };

            string source = @"
                local ok, m = pcall(require, 'mod')
                return ok, m.value";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertIntegerResult(vm, 42, 1);
        }

        [Test]
        public void Require_NoReaderInstalled_Throws()
        {
            var ex = Assert.Throws<RuntimeException>(
                () => Utils.Run("return require('mod')", 1000));
            Assert.That(ex.Message, Is.EqualTo("attempt to load 'mod' (no module reader installed)"));
        }

        [Test]
        public void Require_CompileError_CaughtByPcall()
        {
            var modules = new Dictionary<string, string>
            {
                ["bad"] = "x = = 1",
            };

            string source = @"
                local ok, err = pcall(require, 'bad')
                return ok, err";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertBoolResult(vm, false, 0);

            Assert.IsInstanceOf<StringObject>(vm.ExecutionResult[1]);
            var message = ((StringObject)vm.ExecutionResult[1]).Value;
            Assert.That(message, Does.StartWith("error loading module 'bad':"));
        }

        [Test]
        public void Require_RuntimeErrorInModule_CaughtByPcall_ThenReportsPreviousError()
        {
            var modules = new Dictionary<string, string>
            {
                ["mod"] = "error('boom')",
            };

            string source = @"
                local ok, err = pcall(require, 'mod')
                local ok2, err2 = pcall(require, 'mod')
                return ok, err, ok2, err2";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "boom", 1);
            Utils.AssertBoolResult(vm, false, 2);
            Utils.AssertStringResult(vm, "loop or previous error loading module 'mod'", 3);
        }

        [Test]
        public void Require_Circular_Throws()
        {
            var modules = new Dictionary<string, string>
            {
                ["A"] = @"
                    require('B')
                    return 1",
                ["B"] = @"
                    require('A')
                    return 2",
            };

            var ex = Assert.Throws<RuntimeException>(
                () => Utils.Run("return require('A')", 1000, Reader(modules)));
            Assert.That(ex.Message, Is.EqualTo("loop or previous error loading module 'A'"));
        }

        [Test]
        public void Require_InsideCoroutine()
        {
            var modules = new Dictionary<string, string>
            {
                ["mod"] = "return { value = 7 }",
            };

            string source = @"
                local co = coroutine.create(function()
                    local m = require('mod')
                    coroutine.yield(m.value)
                end)
                local ok, v = coroutine.resume(co)
                return ok, v";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertIntegerResult(vm, 7, 1);
        }

        [Test]
        public void Require_ModuleWritesPackageLoadedItself()
        {
            var modules = new Dictionary<string, string>
            {
                ["mod"] = "package.loaded['mod'] = { x = 1 }",
            };

            string source = @"
                local m = require('mod')
                return m.x";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertIntegerResult(vm, 1);
        }

        [Test]
        public void Require_HostPreloadedModule_SkipsReader()
        {
            var func = LuaCompiler.Compile("return require('pre').x");
            var vm = Utils.Load(func);

            var preloaded = new TableObject();
            preloaded["x"] = LuaObject.From(5);
            vm.LoadedModules["pre"] = preloaded;

            // No ModuleReader installed: the cache hit must never consult it.
            Utils.Run(vm, 1000);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void Require_NonStringArg_Throws()
        {
            var ex = Assert.Throws<RuntimeException>(
                () => Utils.Run("return require(42)", 1000, Reader(new Dictionary<string, string>())));
            Assert.That(ex.Message, Is.EqualTo("bad argument #1 to 'require' (string expected, got number)"));
        }

        [Test]
        public void Dofile_MultipleReturns()
        {
            var modules = new Dictionary<string, string>
            {
                ["f"] = "return 1, 2, 3",
            };

            var vm = Utils.Run("return dofile('f')", 1000, Reader(modules));
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
            Utils.AssertIntegerResult(vm, 3, 2);
        }

        [Test]
        public void Dofile_TruncatesToRequestedResults()
        {
            var modules = new Dictionary<string, string>
            {
                ["f"] = "return 1, 2, 3",
            };

            string source = @"
                local a, b = dofile('f')
                return a, b";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Assert.That(vm.ExecutionResult.Length, Is.EqualTo(2));
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
        }

        [Test]
        public void Dofile_RunsEveryCall()
        {
            var modules = new Dictionary<string, string>
            {
                ["f"] = @"
                    count = (count or 0) + 1
                    return count",
            };

            string source = @"
                local a = dofile('f')
                local b = dofile('f')
                return a, b";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertIntegerResult(vm, 1, 0);
            Utils.AssertIntegerResult(vm, 2, 1);
        }

        [Test]
        public void Dofile_NotFound_CaughtByPcall()
        {
            string source = @"
                local ok, err = pcall(dofile, 'nope')
                return ok, err";

            var vm = Utils.Run(source, 1000, Reader(new Dictionary<string, string>()));
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "cannot open 'nope'", 1);
        }

        [Test]
        public void Dofile_MissingArg_Throws()
        {
            var ex = Assert.Throws<RuntimeException>(
                () => Utils.Run("return dofile()", 1000, Reader(new Dictionary<string, string>())));
            Assert.That(ex.Message, Is.EqualTo("bad argument #1 to 'dofile' (string expected, got no value)"));
        }

        [Test]
        public void Dofile_RuntimeError_CaughtByPcall()
        {
            var modules = new Dictionary<string, string>
            {
                ["f"] = "error('kaboom')",
            };

            string source = @"
                local ok, err = pcall(dofile, 'f')
                return ok, err";

            var vm = Utils.Run(source, 1000, Reader(modules));
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "kaboom", 1);
        }
    }
}
