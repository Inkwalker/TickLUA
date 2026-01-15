using TickLUA.VM.Objects;

namespace TickLUA_Tests
{
    public class TestsMath
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Add()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 40));
            bytecode.Instructions.Add(Instruction.LOADI(1, 2));
            bytecode.Instructions.Add(Instruction.ADD(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            bytecode.RegisterCount = 3;

            var vm = new TickVM(bytecode);

            for (int i = 0; i < 4; i++)
            {
                vm.Tick();
            }

            Assert.IsTrue(vm.IsFinished);

            Assert.NotNull(vm.ExecutionResult);
            Assert.IsTrue(vm.ExecutionResult.Length == 1);
            Assert.IsInstanceOf<IntegerObject>(vm.ExecutionResult[0]);

            var answer = vm.ExecutionResult[0] as IntegerObject;

            Assert.AreEqual(answer.Value, 42);
        }
    }
}