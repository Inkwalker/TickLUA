using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;
using TickLUA.VM.Serialization;

namespace TickLUA_Tests
{
    internal class SerializationTests
    {
        #region Value round-trips

        [Test]
        public void Nil_RoundTrip()
        {
            var result = LuaValueSerializer.Deserialize(LuaValueSerializer.Serialize(NilObject.Nil));
            Assert.That(result, Is.SameAs(NilObject.Nil));
        }

        [Test]
        public void Booleans_RoundTrip()
        {
            Assert.That(LuaValueSerializer.Deserialize(LuaValueSerializer.Serialize(BooleanObject.True)),
                Is.SameAs(BooleanObject.True));
            Assert.That(LuaValueSerializer.Deserialize(LuaValueSerializer.Serialize(BooleanObject.False)),
                Is.SameAs(BooleanObject.False));
        }

        [TestCase(0f)]
        [TestCase(42f)]
        [TestCase(-17f)]
        [TestCase(3.5f)]
        [TestCase(float.MaxValue)]
        [TestCase(float.NegativeInfinity)]
        public void Numbers_RoundTrip(float value)
        {
            var result = LuaValueSerializer.Deserialize(LuaValueSerializer.Serialize(new NumberObject(value)));

            Assert.IsInstanceOf<NumberObject>(result);
            Assert.That(((NumberObject)result).Value, Is.EqualTo(value));
        }

        [TestCase("")]
        [TestCase("hello")]
        [TestCase("multi\nline \"quoted\"")]
        [TestCase("юнікод 💡")]
        public void Strings_RoundTrip(string value)
        {
            var result = LuaValueSerializer.Deserialize(LuaValueSerializer.Serialize(new StringObject(value)));

            Assert.IsInstanceOf<StringObject>(result);
            Assert.That(((StringObject)result).Value, Is.EqualTo(value));
        }

        [Test]
        public void Table_RoundTrip()
        {
            var inner = new TableObject(new NumberObject(1), new NumberObject(2));
            var table = new TableObject();
            table["name"] = new StringObject("test");
            table["flag"] = BooleanObject.True;
            table[1] = new NumberObject(10);
            table["inner"] = inner;

            var result = (TableObject)LuaValueSerializer.Deserialize(LuaValueSerializer.Serialize(table));

            Assert.That(result.Elements.Count, Is.EqualTo(4));
            Assert.That(result["name"], Is.EqualTo(new StringObject("test")));
            Assert.That(result["flag"], Is.SameAs(BooleanObject.True));
            Assert.That(result[1], Is.EqualTo(new NumberObject(10)));

            var result_inner = (TableObject)result["inner"];
            Assert.That(result_inner[1], Is.EqualTo(new NumberObject(1)));
            Assert.That(result_inner[2], Is.EqualTo(new NumberObject(2)));
        }

        [Test]
        public void Table_Metatable_RoundTrip()
        {
            var table = new TableObject();
            table.Metatable = new TableObject();
            table.Metatable[LuaObject.INDEX_GET] = new StringObject("fallback");

            var result = (TableObject)LuaValueSerializer.Deserialize(LuaValueSerializer.Serialize(table));

            Assert.NotNull(result.Metatable);
            Assert.That(result.Metatable[LuaObject.INDEX_GET], Is.EqualTo(new StringObject("fallback")));
        }

        [Test]
        public void Table_SharedReference_PreservesIdentity()
        {
            var shared = new TableObject();
            var table = new TableObject();
            table["a"] = shared;
            table["b"] = shared;

            var result = (TableObject)LuaValueSerializer.Deserialize(LuaValueSerializer.Serialize(table));

            Assert.That(result["a"], Is.SameAs(result["b"]));
        }

        [Test]
        public void Table_Cycle_RoundTrip()
        {
            var table = new TableObject();
            table["self"] = table;

            var result = (TableObject)LuaValueSerializer.Deserialize(LuaValueSerializer.Serialize(table));

            Assert.That(result["self"], Is.SameAs(result));
        }

        [Test]
        public void UnsupportedType_Throws()
        {
            var native = new NativeFunctionObject(args => null);

            Assert.Throws<NotSupportedException>(() => LuaValueSerializer.Serialize(native));
        }

        [Test]
        public void GarbageValueStream_Throws()
        {
            Assert.Throws<BytecodeFormatException>(() => LuaValueSerializer.Deserialize(new byte[] { 200 }));
        }

        #endregion

        #region Bytecode round-trips

        private const string Program =
            @"local counter = 0

              local function make_adder(step)
                  return function(x)
                      counter = counter + step
                      return x + step
                  end
              end

              local add3 = make_adder(3)
              local sum = 0
              for i = 1, 5 do
                  sum = sum + add3(i)
              end

              local t = { greeting = 'hello', 1, 2, 3 }
              return sum, counter, t.greeting, #t, ...";

        [Test]
        public void Bytecode_RoundTrip_SameResults()
        {
            var compiled = LuaCompiler.Compile(Program);
            var restored = BytecodeSerializer.Deserialize(BytecodeSerializer.Serialize(compiled));

            var vm = Utils.Run(restored, 1000, new NumberObject(99));

            // sum = (1+3) + (2+3) + ... + (5+3) = 30; counter = 5 * 3 = 15
            Utils.AssertIntegerResult(vm, 30, 0);
            Utils.AssertIntegerResult(vm, 15, 1);
            Utils.AssertStringResult(vm, "hello", 2);
            Utils.AssertIntegerResult(vm, 3, 3);
            Utils.AssertIntegerResult(vm, 99, 4);
        }

        [Test]
        public void Bytecode_RoundTrip_PreservesStructure()
        {
            var compiled = LuaCompiler.Compile(Program);
            var restored = BytecodeSerializer.Deserialize(BytecodeSerializer.Serialize(compiled));

            AssertFunctionsEqual(compiled, restored);
        }

        private static void AssertFunctionsEqual(LuaFunction expected, LuaFunction actual)
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.HasVarargs, Is.EqualTo(expected.HasVarargs));
            Assert.That(actual.ParameterCount, Is.EqualTo(expected.ParameterCount));
            Assert.That(actual.RegisterCount, Is.EqualTo(expected.RegisterCount));
            Assert.That(actual.CompilerVersion, Is.EqualTo(expected.CompilerVersion));

            Assert.That(actual.Instructions.Count, Is.EqualTo(expected.Instructions.Count));
            for (int i = 0; i < expected.Instructions.Count; i++)
                Assert.That(actual.Instructions[i].Raw, Is.EqualTo(expected.Instructions[i].Raw));

            Assert.That(actual.Meta.Lines, Is.EqualTo(expected.Meta.Lines));

            Assert.That(actual.HasDebugInfo, Is.EqualTo(expected.HasDebugInfo));
            Assert.That(actual.Meta.Locals.Count, Is.EqualTo(expected.Meta.Locals.Count));
            for (int i = 0; i < expected.Meta.Locals.Count; i++)
            {
                Assert.That(actual.Meta.Locals[i].Name, Is.EqualTo(expected.Meta.Locals[i].Name));
                Assert.That(actual.Meta.Locals[i].Register, Is.EqualTo(expected.Meta.Locals[i].Register));
                Assert.That(actual.Meta.Locals[i].StartPC, Is.EqualTo(expected.Meta.Locals[i].StartPC));
                Assert.That(actual.Meta.Locals[i].EndPC, Is.EqualTo(expected.Meta.Locals[i].EndPC));
            }

            Assert.That(actual.Constants.Count, Is.EqualTo(expected.Constants.Count));
            for (int i = 0; i < expected.Constants.Count; i++)
                Assert.That(actual.Constants[i], Is.EqualTo(expected.Constants[i]));

            Assert.That(actual.Upvalues.Count, Is.EqualTo(expected.Upvalues.Count));
            for (int i = 0; i < expected.Upvalues.Count; i++)
            {
                Assert.That(actual.Upvalues[i].Name, Is.EqualTo(expected.Upvalues[i].Name));
                Assert.That(actual.Upvalues[i].IsLocalToParent, Is.EqualTo(expected.Upvalues[i].IsLocalToParent));
                Assert.That(actual.Upvalues[i].Index, Is.EqualTo(expected.Upvalues[i].Index));
            }

            Assert.That(actual.NestedFunctions.Count, Is.EqualTo(expected.NestedFunctions.Count));
            for (int i = 0; i < expected.NestedFunctions.Count; i++)
                AssertFunctionsEqual(expected.NestedFunctions[i], actual.NestedFunctions[i]);
        }

        #endregion

        #region Debug info stripping

        [Test]
        public void Strip_RemovesLocals_KeepsLines()
        {
            var compiled = LuaCompiler.Compile(Program);
            var stripped = BytecodeSerializer.Deserialize(BytecodeSerializer.Serialize(compiled, stripDebugInfo: true));

            void AssertStripped(LuaFunction expected, LuaFunction actual)
            {
                Assert.IsFalse(actual.HasDebugInfo);
                Assert.That(actual.Meta.Locals, Is.Empty);
                Assert.That(actual.Meta.Lines, Is.EqualTo(expected.Meta.Lines));

                Assert.That(actual.NestedFunctions.Count, Is.EqualTo(expected.NestedFunctions.Count));
                for (int i = 0; i < expected.NestedFunctions.Count; i++)
                    AssertStripped(expected.NestedFunctions[i], actual.NestedFunctions[i]);
            }

            Assert.IsTrue(compiled.HasDebugInfo);
            AssertStripped(compiled, stripped);
        }

        [Test]
        public void Strip_SameExecutionResults()
        {
            var compiled = LuaCompiler.Compile(Program);
            var stripped = BytecodeSerializer.Deserialize(BytecodeSerializer.Serialize(compiled, stripDebugInfo: true));

            var vm = Utils.Run(stripped, 1000, new NumberObject(99));

            Utils.AssertIntegerResult(vm, 30, 0);
            Utils.AssertIntegerResult(vm, 15, 1);
            Utils.AssertStringResult(vm, "hello", 2);
            Utils.AssertIntegerResult(vm, 3, 3);
            Utils.AssertIntegerResult(vm, 99, 4);
        }

        [Test]
        public void Strip_ProducesSmallerPayload()
        {
            var compiled = LuaCompiler.Compile(Program);

            var full = BytecodeSerializer.Serialize(compiled);
            var stripped = BytecodeSerializer.Serialize(compiled, stripDebugInfo: true);

            Assert.That(stripped.Length, Is.LessThan(full.Length));
        }

        [Test]
        public void Strip_ReserializedStrippedChunk_StaysStripped()
        {
            var compiled = LuaCompiler.Compile(Program);
            var stripped = BytecodeSerializer.Deserialize(BytecodeSerializer.Serialize(compiled, stripDebugInfo: true));

            // No strip flag this time: HasDebugInfo == false must carry through.
            var round_tripped = BytecodeSerializer.Deserialize(BytecodeSerializer.Serialize(stripped));

            Assert.IsFalse(round_tripped.HasDebugInfo);
            Assert.That(round_tripped.Meta.Locals, Is.Empty);
        }

        [Test]
        public void Strip_TracebackKeepsRealLineNumbers()
        {
            var compiled = LuaCompiler.Compile("local x = 1\nlocal y = x .. {}\nreturn y");
            var stripped = BytecodeSerializer.Deserialize(BytecodeSerializer.Serialize(compiled, stripDebugInfo: true));

            var vm = Utils.Load(stripped);
            var ex = Assert.Throws<RuntimeException>(() =>
            {
                while (!vm.IsFinished) vm.Tick();
            });

            Assert.That(ex.LuaTraceback, Is.Not.Empty);
            Assert.That(ex.LuaTraceback[0].Line, Is.EqualTo(2));
        }

        #endregion

        #region Version and format checks

        [Test]
        public void BadMagic_Throws()
        {
            var data = BytecodeSerializer.Serialize(LuaCompiler.Compile("return 1"));
            data[0] = (byte)'X';

            Assert.Throws<BytecodeFormatException>(() => BytecodeSerializer.Deserialize(data));
        }

        [Test]
        public void TruncatedStream_Throws()
        {
            var data = BytecodeSerializer.Serialize(LuaCompiler.Compile("return 1"));
            var truncated = data.Take(data.Length / 2).ToArray();

            Assert.Throws<BytecodeFormatException>(() => BytecodeSerializer.Deserialize(truncated));
        }

        [Test]
        public void IncompatibleVersion_DeserializationRejected()
        {
            var data = BytecodeSerializer.Serialize(LuaCompiler.Compile("return 1"));

            // The version is a little-endian ushort right after the 4-byte magic.
            ushort bad_version = LuaFunction.CurrentCompilerVersion + 1;
            data[4] = (byte)(bad_version & 0xFF);
            data[5] = (byte)(bad_version >> 8);

            var ex = Assert.Throws<BytecodeFormatException>(() => BytecodeSerializer.Deserialize(data));
            StringAssert.Contains("version", ex.Message);
        }

        [Test]
        public void IncompatibleVersion_VmRejected()
        {
            var func = LuaCompiler.Compile("return 1");
            func.CompilerVersion = LuaFunction.CurrentCompilerVersion + 1;

            Assert.Throws<BytecodeFormatException>(() => Utils.Load(func));
        }

        [Test]
        public void CompiledFunction_CarriesCurrentVersion()
        {
            var func = LuaCompiler.Compile("return 1");
            Assert.That(func.CompilerVersion, Is.EqualTo(LuaFunction.CurrentCompilerVersion));
        }

        #endregion
    }
}
