namespace TickLUA_Tests
{
    public class TestsCore
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Nop()
        {
            var bytecode = new LuaFunction();
            bytecode.Instructions.Add(Instruction.NOP());
            bytecode.Instructions.Add(Instruction.NOP());
            bytecode.Instructions.Add(Instruction.NOP());

            var vm = new TickVM(bytecode);

            for (int i = 0; i < 2; i++)
            {
                vm.Tick();
            }

            Assert.IsFalse(vm.IsFinished);

            vm.Tick();

            Assert.IsTrue(vm.IsFinished);
        }
    }
}