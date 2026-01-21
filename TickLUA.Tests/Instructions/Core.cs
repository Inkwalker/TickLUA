using TickLUA.VM.Objects;
using TickLUA_Tests.LUA;

namespace TickLUA_Tests.Instructions
{
    public class Core
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void NOP()
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
        public void LOADK()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 1);

            bytecode.Instructions.Add(Instruction.LOADK(0, 0));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            bytecode.Constants.Add(new NumberObject(42));

            var vm = Utils.Run(bytecode, 2);

            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void LOADI()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 1);

            bytecode.Instructions.Add(Instruction.LOADI(0, 42));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 2);

            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void LOADBOOL()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 2);

            bytecode.Instructions.Add(Instruction.LOADBOOL(0, true));
            bytecode.Instructions.Add(Instruction.LOADBOOL(1, false));
            bytecode.Instructions.Add(Instruction.RETURN(0, 2));

            var vm = Utils.Run(bytecode, 3);

            Utils.AssertBoolResult(vm, true,  0);
            Utils.AssertBoolResult(vm, false, 1);
        }

        [Test]
        public void LOADNIL()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 1);

            bytecode.Instructions.Add(Instruction.LOADBOOL(0, true));
            bytecode.Instructions.Add(Instruction.LOADNIL(0));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 3);

            Utils.AssertNilResult(vm, 0);
        }

        [Test]
        public void MOVE()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 2);

            bytecode.Instructions.Add(Instruction.LOADI(0, 42));
            bytecode.Instructions.Add(Instruction.LOADI(1, 2));
            bytecode.Instructions.Add(Instruction.MOVE(1, 0));
            bytecode.Instructions.Add(Instruction.RETURN(1, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 42);
        }
    }
}