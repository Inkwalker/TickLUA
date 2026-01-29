namespace TickLUA_Tests.LUA
{
    internal class Branching
    {
        [Test]
        public void IfThen()
        {
            string source = @"
                local a = 15

                if a > 10 then
                    return 42
                end

                return 5";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42, 0);
        }

        [Test]
        public void IfThenElse()
        {
            string source = @"
                local a = true
                local b = 0
                local c = 0

                if not a then
                    c = 12
                else
                    c = 5
                    c = 41
                end

                if a then
                    b = 6
                    b = 42
                else
                    b = 13
                end

                return b, c";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42, 0);
            Utils.AssertIntegerResult(vm, 41, 1);
        }

        [Test]
        public void ElseIf()
        {
            string source = @"
                local a = 5
                local c = 0

                if a > 5 then
                    c = 12
                elseif a < 5 then
                    c = 24
                else
                    c = 42
                end

                return c";

            var vm = Utils.Run(source, 100);
            Utils.AssertIntegerResult(vm, 42, 0);
        }
    }
}
