using TickLUA.Compilers.LUA;
using TickLUA.VM.Objects;

namespace TickLUA_Tests.Instructions
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
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 1);
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

        [Test]
        public void Move()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 42));
            bytecode.Instructions.Add(Instruction.LOADI(1, 2));
            bytecode.Instructions.Add(Instruction.MOVE(1, 0));
            bytecode.Instructions.Add(Instruction.RETURN(1, 1));

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