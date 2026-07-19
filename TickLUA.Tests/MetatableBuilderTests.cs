using TickLUA.VM.Objects;

namespace TickLUA_Tests
{
    internal class MetatableBuilderTests
    {
        [Test]
        public void Builder_ImplicitCast_YieldsTableWithHandlers()
        {
            NativeFunction func = args => LuaObject.NoResults;

            TableObject mt = MetatableBuilder.Call(func).Add(func);

            Assert.That(mt[LuaObject.CALL], Is.InstanceOf<NativeFunctionObject>());
            Assert.That(mt[LuaObject.ADD], Is.InstanceOf<NativeFunctionObject>());
            Assert.That(mt[LuaObject.SUB], Is.EqualTo(NilObject.Nil));
        }

        [Test]
        public void Builder_Chaining_ReturnsSameChain()
        {
            NativeFunction func = args => LuaObject.NoResults;

            var chain = MetatableBuilder.Index(func);
            Assert.That(chain.NewIndex(func), Is.SameAs(chain));
        }

        [Test]
        public void Builder_Repeat_OverwritesSlot()
        {
            NativeFunction first = args => LuaObject.NoResults;
            NativeFunction second = args => LuaObject.NoResults;

            TableObject mt = MetatableBuilder.Call(first).Call(second);

            var handler = (NativeFunctionObject)mt[LuaObject.CALL];
            Assert.That(handler.Function, Is.SameAs(second));
        }

        [Test]
        public void Builder_Metatable_DispatchesInVm()
        {
            var obj = new TableObject();
            obj.Metatable = MetatableBuilder
                .Call(args => new LuaObject[] { new NumberObject(args.CheckInteger(1) * 2) })
                .Add(args => new LuaObject[] { new NumberObject(100) })
                .Len(args => new LuaObject[] { new NumberObject(7) });

            string source = @"
            local obj = ...
            return obj(21), obj + 1, #obj";

            var vm = Utils.Run(source, 1000, obj);
            Utils.AssertIntegerResult(vm, 42, 0);
            Utils.AssertIntegerResult(vm, 100, 1);
            Utils.AssertIntegerResult(vm, 7, 2);
        }

        [Test]
        public void Builder_Protect_SetsMetatableField()
        {
            TableObject def = MetatableBuilder.Protect();
            TableObject named = MetatableBuilder.Protect("Vec2");

            Assert.That(((StringObject)def[LuaObject.METATABLE]).Value, Is.EqualTo("protected"));
            Assert.That(((StringObject)named[LuaObject.METATABLE]).Value, Is.EqualTo("Vec2"));
        }

        [Test]
        public void Builder_Protect_LocksMetatableInVm()
        {
            var obj = new TableObject();
            obj.Metatable = MetatableBuilder
                .Len(args => new LuaObject[] { new NumberObject(7) })
                .Protect("Vec2");

            string source = @"
            local obj = ...
            local ok, err = pcall(setmetatable, obj, {})
            return ok, err, getmetatable(obj), #obj";

            var vm = Utils.Run(source, 1000, obj);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "cannot change a protected metatable", 1);
            Utils.AssertStringResult(vm, "Vec2", 2);
            Utils.AssertIntegerResult(vm, 7, 3);
        }

        [Test]
        public void Builder_HandlerName_IsEventName()
        {
            var obj = new TableObject();
            obj.Metatable = MetatableBuilder.Call(args =>
            {
                args.CheckInteger(1); // errors: caller passes a string
                return LuaObject.NoResults;
            });

            string source = @"
            local obj = ...
            local ok, err = pcall(obj, 'not a number')
            return ok, err";

            var vm = Utils.Run(source, 1000, obj);
            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertStringResult(vm, "bad argument #2 to '__call' (number expected, got string)", 1);
        }
    }
}
