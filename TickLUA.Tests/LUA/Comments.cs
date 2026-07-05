namespace TickLUA_Tests.LUA
{
    internal class Comments
    {
        [Test]
        public void LineComment_Trailing()
        {
            string source =
                "local a = 5 -- assign five to a\n" +
                "return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void LineComment_FullLine()
        {
            string source =
                "-- this whole line is a comment\n" +
                "local a = 42\n" +
                "return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void LineComment_BetweenStatements()
        {
            string source =
                "local a = 1\n" +
                "-- bump it\n" +
                "a = a + 1\n" +
                "return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2);
        }

        [Test]
        public void BlockComment_MultiLine()
        {
            string source =
                "--[[ this comment\n" +
                "   spans several\n" +
                "   lines ]]\n" +
                "local a = 5\n" +
                "return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void BlockComment_SingleLine()
        {
            string source =
                "local a = --[[ inline ]] 42\n" +
                "return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void BlockComment_BetweenStatements()
        {
            string source =
                "local a = 1\n" +
                "--[[ multi\n" +
                "line note ]]\n" +
                "a = a + 1\n" +
                "return a";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 2);
        }
    }
}
