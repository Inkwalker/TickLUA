namespace TickLUA_Tests.LUA
{
    internal class String
    {
        [Test]
        public void Len()
        {
            var source =
                @"local x = 'hello world'
                  return #x";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 11);
        }

        [Test]
        public void Index()
        {
            var source =
                @"local x = 'hello world'
                  return x[5]";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "o");
        }

        [Test]
        public void Concat_Chained()
        {
            var source = @"return 'a' .. 'b' .. 'c'";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "abc");
        }

        [Test]
        public void DoubleQuotedString()
        {
            // Double quotes delimit strings exactly like single quotes.
            var source = "return \"hello world\"";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "hello world");
        }

        [Test]
        public void NewlineEscape()
        {
            // The \n escape sequence produces a literal newline character.
            var source = @"return 'a\nb'";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "a\nb");
        }

        [Test]
        public void Concat_NumberCoercion()
        {
            // Concatenation coerces numbers to their string representation.
            var source = @"return 1 .. 2";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "12");
        }

        [Test]
        public void Concat_MixedStringNumber()
        {
            var source = @"return 'x' .. 5";

            var vm = Utils.Run(source, 100);
            Utils.AssertStringResult(vm, "x5");
        }

        [Test]
        public void Compare_LessThan()
        {
            // Strings are compared by byte order (locale-independent here).
            // See https://www.lua.org/manual/5.4/manual.html#3.4.4
            var source = @"return 'a' < 'b'";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true);
        }

        [Test]
        public void Compare_Lexicographic()
        {
            var source = @"return 'apple' < 'banana'";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true);
        }

        [Test]
        public void Compare_PrefixIsLess()
        {
            // A proper prefix compares as less than the longer string.
            var source = @"return 'car' < 'card'";

            var vm = Utils.Run(source, 100);
            Utils.AssertBoolResult(vm, true);
        }
    }
}
