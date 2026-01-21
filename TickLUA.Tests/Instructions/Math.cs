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
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 40));
            bytecode.Instructions.Add(Instruction.LOADI(1, 2));
            bytecode.Instructions.Add(Instruction.ADD(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            bytecode.RegisterCount = 3;

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void SUB()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 2));
            bytecode.Instructions.Add(Instruction.LOADI(1, 12));
            bytecode.Instructions.Add(Instruction.SUB(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            bytecode.RegisterCount = 3;

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, -10);
        }

        [Test]
        public void MUL()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 3));
            bytecode.Instructions.Add(Instruction.LOADI(1, 4));
            bytecode.Instructions.Add(Instruction.MUL(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            bytecode.RegisterCount = 3;

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 12);
        }

        [Test]
        public void MOD()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 32));
            bytecode.Instructions.Add(Instruction.LOADI(1, 5));
            bytecode.Instructions.Add(Instruction.MOD(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            bytecode.RegisterCount = 3;

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 2);
        }

        [Test]
        public void POW()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 2));
            bytecode.Instructions.Add(Instruction.LOADI(1, 6));
            bytecode.Instructions.Add(Instruction.POW(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            bytecode.RegisterCount = 3;

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertFloatResult(vm, 64);
        }

        [Test]
        public void DIV()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 5));
            bytecode.Instructions.Add(Instruction.LOADI(1, 2));
            bytecode.Instructions.Add(Instruction.DIV(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            bytecode.RegisterCount = 3;

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertFloatResult(vm, 2.5f);
        }

        [Test]
        public void IDIV()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 12));
            bytecode.Instructions.Add(Instruction.LOADI(1, 4));
            bytecode.Instructions.Add(Instruction.IDIV(2, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            bytecode.RegisterCount = 3;

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 3);
        }
    }
}