using TickLUA.VM.Objects;
using TickLUA_Tests.LUA;

namespace TickLUA_Tests.Instructions
{
    public class Math
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ADD()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 40));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 2));
            bytecode.Instructions.Add(Instruction.ADD(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void SUB()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 2));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 12));
            bytecode.Instructions.Add(Instruction.SUB(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, -10);
        }

        [Test]
        public void MUL()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 3));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 4));
            bytecode.Instructions.Add(Instruction.MUL(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 12);
        }

        [Test]
        public void MOD()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 32));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 5));
            bytecode.Instructions.Add(Instruction.MOD(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 2);
        }

        [Test]
        public void POW()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 2));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 6));
            bytecode.Instructions.Add(Instruction.POW(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertFloatResult(vm, 64);
        }

        [Test]
        public void DIV()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 5));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 2));
            bytecode.Instructions.Add(Instruction.DIV(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertFloatResult(vm, 2.5f);
        }

        [Test]
        public void IDIV()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 12));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 4));
            bytecode.Instructions.Add(Instruction.IDIV(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 3);
        }

        [Test]
        public void UNM()
        {
            var bytecode = new LuaFunction(2);

            bytecode.Constants.Add(new NumberObject(3.14f));

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 12));
            bytecode.Instructions.Add(Instruction.LOAD_CONST(1, 0));
            bytecode.Instructions.Add(Instruction.UNM(0, 0));
            bytecode.Instructions.Add(Instruction.UNM(1, 1));
            bytecode.Instructions.Add(Instruction.RETURN(0, 2));

            var vm = Utils.Run(bytecode, 5);

            Utils.AssertIntegerResult(vm, -12, 0);
            Utils.AssertFloatResult(vm, -3.14f, 1);
        }
    }
}