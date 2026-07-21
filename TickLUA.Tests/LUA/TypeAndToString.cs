using TickLUA.VM.Objects;

namespace TickLUA_Tests.LUA
{
    /// <summary>
    /// The type() and tostring() base functions, including __tostring dispatch
    /// (which can be a Lua closure, so it runs across ticks like any other
    /// metamethod call).
    /// </summary>
    internal class TypeAndToString
    {
        /// <summary>A host type that names itself through the default.</summary>
        private sealed class Vec2 : LuaObject
        {
            public override StringObject ToStringObject() => new StringObject("(1, 2)");
            public override long ShallowMemoryCost() => 16;
        }

        /// <summary>A host type that picks its own script-facing name.</summary>
        private sealed class Handle : LuaObject
        {
            public override string TypeName => "entity";
            public override StringObject ToStringObject() => new StringObject("[entity]");
            public override long ShallowMemoryCost() => 8;
        }

        [Test]
        public void Type_NamesEveryBuiltinType()
        {
            var source = @"
                return type(nil), type(true), type(1.5), type('s'), type({}),
                       type(function() end), type(pcall),
                       type(coroutine.create(function() end))";

            var vm = Utils.Run(source, 1000);
            Utils.AssertStringResult(vm, "nil", 0);
            Utils.AssertStringResult(vm, "boolean", 1);
            Utils.AssertStringResult(vm, "number", 2);
            Utils.AssertStringResult(vm, "string", 3);
            Utils.AssertStringResult(vm, "table", 4);
            Utils.AssertStringResult(vm, "function", 5); // closure
            Utils.AssertStringResult(vm, "function", 6); // native
            Utils.AssertStringResult(vm, "thread", 7);
        }

        [Test]
        public void Type_OfAMissingGlobalIsNil()
        {
            var vm = Utils.Run("return type(no_such_global)", 100);
            Utils.AssertStringResult(vm, "nil");
        }

        [Test]
        public void Type_OfAHostTypeDefaultsToItsClassName()
        {
            var vm = Utils.Load(TickLUA.Compilers.LUA.LuaCompiler.Compile("return type(vec)"));
            vm.Globals["vec"] = new Vec2();
            Utils.Run(vm, 100);

            Utils.AssertStringResult(vm, "Vec2");
        }

        [Test]
        public void Type_HostTypeCanNameItself()
        {
            var vm = Utils.Load(TickLUA.Compilers.LUA.LuaCompiler.Compile(
                "return type(h), type(h) == 'entity'"));
            vm.Globals["h"] = new Handle();
            Utils.Run(vm, 100);

            Utils.AssertStringResult(vm, "entity", 0);
            Utils.AssertBoolResult(vm, true, 1);
        }

        [Test]
        public void Type_HostNameAlsoShowsUpInArgumentErrors()
        {
            // One name, one source: the same override feeds error messages.
            var vm = Utils.Load(TickLUA.Compilers.LUA.LuaCompiler.Compile(
                "local ok, err = pcall(math.sqrt, h) return err"));
            vm.Globals["h"] = new Handle();
            Utils.Run(vm, 100);

            Utils.AssertStringResult(vm, "bad argument #1 to 'sqrt' (number expected, got entity)");
        }

        [Test]
        public void Type_WithNoArgumentIsAnError()
        {
            var vm = Utils.Run("local ok, err = pcall(type) return ok, err", 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "bad argument #1 to 'type' (value expected, got no value)", 1);
        }

        [Test]
        public void Tostring_Primitives()
        {
            var vm = Utils.Run(
                "return tostring(nil), tostring(true), tostring(false), tostring(12), tostring(1.5), tostring('s')",
                1000);

            Utils.AssertStringResult(vm, "nil", 0);
            Utils.AssertStringResult(vm, "true", 1);
            Utils.AssertStringResult(vm, "false", 2);
            Utils.AssertStringResult(vm, "12", 3);
            Utils.AssertStringResult(vm, "1.5", 4);
            Utils.AssertStringResult(vm, "s", 5);
        }

        [Test]
        public void Tostring_ResultIsAStringUsableInConcat()
        {
            var vm = Utils.Run("return 'v=' .. tostring(nil)", 100);
            Utils.AssertStringResult(vm, "v=nil");
        }

        [Test]
        public void Tostring_UsesTheHostTypesOwnRendering()
        {
            var vm = Utils.Load(TickLUA.Compilers.LUA.LuaCompiler.Compile("return tostring(vec)"));
            vm.Globals["vec"] = new Vec2();
            Utils.Run(vm, 100);

            Utils.AssertStringResult(vm, "(1, 2)");
        }

        [Test]
        public void Tostring_CallsTheTostringMetamethod()
        {
            var source = @"
                local t = setmetatable({ n = 7 }, { __tostring = function(self) return 'n=' .. self.n end })
                return tostring(t)";

            var vm = Utils.Run(source, 1000);
            Utils.AssertStringResult(vm, "n=7");
        }

        [Test]
        public void Tostring_MetamethodResultFlowsIntoTheExpressionAroundIt()
        {
            // The handler is a closure, so its result only arrives a few ticks
            // later — the concat has to still see it.
            var source = @"
                local t = setmetatable({}, { __tostring = function() return 'X' end })
                return '[' .. tostring(t) .. ']'";

            var vm = Utils.Run(source, 1000);
            Utils.AssertStringResult(vm, "[X]");
        }

        [Test]
        public void Tostring_MetamethodMustReturnAString()
        {
            var source = @"
                local t = setmetatable({}, { __tostring = function() return 42 end })
                local ok, err = pcall(function() return tostring(t) end)
                return ok, err";

            var vm = Utils.Run(source, 1000);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "'__tostring' must return a string", 1);
        }

        [Test]
        public void Tostring_ErrorInsideTheMetamethodIsCatchable()
        {
            var source = @"
                local t = setmetatable({}, { __tostring = function() error('boom') end })
                local ok, err = pcall(function() return tostring(t) end)
                return ok, err";

            var vm = Utils.Run(source, 1000);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "boom", 1);
        }

        [Test]
        public void Tostring_CalledThroughPcallDirectly()
        {
            // pcall hands its own sinks to the args form of the native.
            var vm = Utils.Run("local ok, s = pcall(tostring, 4) return ok, s", 100);
            Utils.AssertBoolResult(vm, true, 0);
            Utils.AssertStringResult(vm, "4", 1);
        }

        [Test]
        public void Tostring_WithNoArgumentIsAnError()
        {
            var vm = Utils.Run("local ok, err = pcall(tostring) return ok, err", 100);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "bad argument #1 to 'tostring' (value expected, got no value)", 1);
        }
    }
}
