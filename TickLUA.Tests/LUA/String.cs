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
    }
}
