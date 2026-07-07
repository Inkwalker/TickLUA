using TickLUA.VM.Objects;

namespace TickLUA_Tests
{
    public class NativeArgsTests
    {
        private static NativeArgs Args(params LuaObject[] args) => new NativeArgs(args);

        private static NativeArgs Named(string name, params LuaObject[] args)
        {
            var native = new NativeFunctionObject(name, a => null);
            Assert.That(native.Name, Is.EqualTo(name));
            // Internal ctor carries the name, same path the VM uses.
            return new TickLUA.VM.NativeArgs(args, name);
        }

        [Test]
        public void Indexer_OutOfRange_IsNil()
        {
            var args = Args(new NumberObject(1));

            Assert.That(args[-1], Is.EqualTo(NilObject.Nil));
            Assert.That(args[1], Is.EqualTo(NilObject.Nil));
            Assert.That(args[100], Is.EqualTo(NilObject.Nil));
        }

        [Test]
        public void Count_And_Default()
        {
            Assert.That(Args().Count, Is.EqualTo(0));
            Assert.That(Args(new NumberObject(1), NilObject.Nil).Count, Is.EqualTo(2));

            var empty = default(NativeArgs);
            Assert.That(empty.Count, Is.EqualTo(0));
            Assert.That(empty[0], Is.EqualTo(NilObject.Nil));
            Assert.That(empty.FunctionName, Is.EqualTo("?"));
        }

        [Test]
        public void None_Vs_Nil()
        {
            var args = Args(NilObject.Nil);

            Assert.IsTrue(args.IsNil(0));
            Assert.IsFalse(args.IsNone(0));
            Assert.IsTrue(args.IsNilOrNone(0));

            Assert.IsFalse(args.IsNil(1));
            Assert.IsTrue(args.IsNone(1));
            Assert.IsTrue(args.IsNilOrNone(1));
        }

        [Test]
        public void TypePredicates()
        {
            var args = Args(
                new NumberObject(1.5f),
                new StringObject("hi"),
                BooleanObject.True,
                new TableObject(),
                new NativeFunctionObject(a => null),
                new ClosureObject(new LuaFunction("f", 0)),
                new NumberObject(2));

            Assert.IsTrue(args.IsNumber(0));
            Assert.IsFalse(args.IsInteger(0));
            Assert.IsTrue(args.IsString(1));
            Assert.IsTrue(args.IsBoolean(2));
            Assert.IsTrue(args.IsTable(3));
            Assert.IsTrue(args.IsFunction(4));
            Assert.IsTrue(args.IsFunction(5));
            Assert.IsTrue(args.IsInteger(6));

            Assert.IsFalse(args.IsNumber(1));
            Assert.IsFalse(args.IsString(0));
            Assert.IsFalse(args.IsBoolean(0));
            Assert.IsFalse(args.IsTable(0));
            Assert.IsFalse(args.IsFunction(0));
        }

        [Test]
        public void Check_HappyPaths()
        {
            var table = new TableObject();
            var args = Args(
                new NumberObject(2.5f),
                new StringObject("abc"),
                BooleanObject.False,
                table,
                new NumberObject(7));

            Assert.That(args.CheckNumber(0), Is.EqualTo(2.5f));
            Assert.That(args.CheckString(1), Is.EqualTo("abc"));
            Assert.That(args.CheckBoolean(2), Is.False);
            Assert.That(args.CheckTable(3), Is.SameAs(table));
            Assert.That(args.CheckInteger(4), Is.EqualTo(7));
            Assert.That(args.CheckAny(0), Is.InstanceOf<NumberObject>());
        }

        [Test]
        public void Check_WrongType_ExactMessage()
        {
            var args = Named("add", new NumberObject(2), new StringObject("x"));

            var ex = Assert.Throws<RuntimeException>(() => args.CheckNumber(1));
            Assert.That(ex.Message, Is.EqualTo("bad argument #2 to 'add' (number expected, got string)"));
        }

        [Test]
        public void Check_NoValue_Message()
        {
            var args = Named("add", new NumberObject(2));

            var ex = Assert.Throws<RuntimeException>(() => args.CheckNumber(1));
            Assert.That(ex.Message, Does.Contain("got no value"));

            var ex_any = Assert.Throws<RuntimeException>(() => args.CheckAny(1));
            Assert.That(ex_any.Message, Does.Contain("value expected"));
        }

        [Test]
        public void Check_UnnamedFunction_UsesQuestionMark()
        {
            var args = Args(new StringObject("x"));

            var ex = Assert.Throws<RuntimeException>(() => args.CheckNumber(0));
            Assert.That(ex.Message, Does.Contain("to '?'"));
        }

        [Test]
        public void CheckInteger_Fractional_Throws()
        {
            var args = Named("f", new NumberObject(1.5f));

            var ex = Assert.Throws<RuntimeException>(() => args.CheckInteger(0));
            Assert.That(ex.Message, Does.Contain("number has no integer representation"));
        }

        [Test]
        public void Opt_DefaultsOnNilOrNone()
        {
            var args = Args(NilObject.Nil);

            Assert.That(args.OptNumber(0, 3.5f), Is.EqualTo(3.5f));
            Assert.That(args.OptNumber(1, 4f), Is.EqualTo(4f));
            Assert.That(args.OptInteger(0, 5), Is.EqualTo(5));
            Assert.That(args.OptString(0, "d"), Is.EqualTo("d"));
            Assert.That(args.OptBoolean(0, true), Is.True);
            var table = new TableObject();
            Assert.That(args.OptTable(0, table), Is.SameAs(table));
        }

        [Test]
        public void Opt_PresentValue_Returned()
        {
            var args = Args(new NumberObject(9), new StringObject("s"));

            Assert.That(args.OptNumber(0, 1f), Is.EqualTo(9f));
            Assert.That(args.OptString(1, "d"), Is.EqualTo("s"));
        }

        [Test]
        public void Opt_WrongType_Throws()
        {
            var args = Args(new StringObject("x"));

            Assert.Throws<RuntimeException>(() => args.OptNumber(0, 1f));
        }

        [Test]
        public void ToArray_IsACopy()
        {
            var args = Args(new NumberObject(1), new NumberObject(2));

            var copy = args.ToArray();
            copy[0] = new NumberObject(99);

            Assert.That(args[0], Is.EqualTo(new NumberObject(1)));
            Assert.That(copy.Length, Is.EqualTo(2));
        }

        [Test]
        public void TypeName_AllTypes()
        {
            Assert.That(NativeArgs.TypeName(NilObject.Nil), Is.EqualTo("nil"));
            Assert.That(NativeArgs.TypeName(null), Is.EqualTo("nil"));
            Assert.That(NativeArgs.TypeName(BooleanObject.True), Is.EqualTo("boolean"));
            Assert.That(NativeArgs.TypeName(new NumberObject(1)), Is.EqualTo("number"));
            Assert.That(NativeArgs.TypeName(new StringObject("s")), Is.EqualTo("string"));
            Assert.That(NativeArgs.TypeName(new TableObject()), Is.EqualTo("table"));
            Assert.That(NativeArgs.TypeName(new NativeFunctionObject(a => null)), Is.EqualTo("function"));
            Assert.That(NativeArgs.TypeName(new ClosureObject(new LuaFunction("f", 0))), Is.EqualTo("function"));
        }
    }
}
