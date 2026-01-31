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
            var bytecode = new LuaFunction(1);
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
        public void LOAD_CONST()
        {
            var bytecode = new LuaFunction(1);

            bytecode.Instructions.Add(Instruction.LOAD_CONST(0, 0));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            bytecode.Constants.Add(new NumberObject(42));

            var vm = Utils.Run(bytecode, 2);

            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void LOAD_INT()
        {
            var bytecode = new LuaFunction(1);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 2);

            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void LOAD_BOOL()
        {
            var bytecode = new LuaFunction(2);

            bytecode.Instructions.Add(Instruction.LOAD_BOOL(0, true));
            bytecode.Instructions.Add(Instruction.LOAD_BOOL(1, false));
            bytecode.Instructions.Add(Instruction.RETURN(0, 2));

            var vm = Utils.Run(bytecode, 3);

            Utils.AssertBoolResult(vm, true,  0);
            Utils.AssertBoolResult(vm, false, 1);
        }

        [Test]
        public void LOAD_FALSE_SKIP()
        {
            var bytecode = new LuaFunction(1);

            bytecode.Instructions.Add(Instruction.LOAD_FALSE_SKIP(0));
            bytecode.Instructions.Add(Instruction.LOAD_TRUE(0));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 3);

            Utils.AssertBoolResult(vm, false, 0);
        }

        [Test]
        public void LOAD_NIL()
        {
            var bytecode = new LuaFunction(1);

            bytecode.Instructions.Add(Instruction.LOAD_TRUE(0));
            bytecode.Instructions.Add(Instruction.LOAD_NIL(0));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 3);

            Utils.AssertNilResult(vm, 0);
        }

        [Test]
        public void MOVE()
        {
            var bytecode = new LuaFunction(2);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 2));
            bytecode.Instructions.Add(Instruction.MOVE(1, 0));
            bytecode.Instructions.Add(Instruction.RETURN(1, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void JMP()
        {
            var bytecode = new LuaFunction(1);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));
            bytecode.Instructions.Add(Instruction.JMP(1));
            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 32));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 42);
        }
    }
}