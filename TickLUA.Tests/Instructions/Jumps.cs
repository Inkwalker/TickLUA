using TickLUA.VM.Objects;
using TickLUA_Tests.LUA;

namespace TickLUA_Tests.Instructions
{
    public class Jumps
    {
        [Test]
        public void Jump()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 1);

            bytecode.Instructions.Add(Instruction.LOADI(0, 42));
            bytecode.Instructions.Add(Instruction.JMP(1));
            bytecode.Instructions.Add(Instruction.LOADI(0, 32));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertIntegerResult(vm, 42);
        }

        [Test]
        public void Test()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 42));
            bytecode.Instructions.Add(Instruction.LOADI(1, 16));
            bytecode.Instructions.Add(Instruction.LOADBOOL(2, true));

            // If register 2 is true, execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.TEST(2, true));
            bytecode.Instructions.Add(Instruction.LOADI(0, 32));

            // If register 2 is false, execute the next instruction (should skip it)
            bytecode.Instructions.Add(Instruction.TEST(2, false));
            bytecode.Instructions.Add(Instruction.LOADI(1, 64));

            bytecode.Instructions.Add(Instruction.RETURN(0, 2));

            var vm = Utils.Run(bytecode, 8);

            Utils.AssertIntegerResult(vm, 32, 0);
            Utils.AssertIntegerResult(vm, 16, 1);
        }

        [Test]
        public void EQ_Numbers()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 42));
            bytecode.Instructions.Add(Instruction.LOADI(1, 16));

            // If reg[0] == reg[1], execute the next instruction (should skip it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOADI(1, 42));

            // If reg[0] != reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOADI(1, 64));

            bytecode.Instructions.Add(Instruction.RETURN(1, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertIntegerResult(vm, 64, 0);
        }

        [Test]
        public void EQ_Nil()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 42));
            bytecode.Instructions.Add(Instruction.LOADNIL(1));

            // If reg[0] == nil, execute the next instruction (should skip it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOADI(0, 32));

            // If reg[0] != nil, execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOADI(0, 64));

            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertIntegerResult(vm, 64, 0);
        }

        [Test]
        public void EQ_Bool()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADBOOL(0, true));
            bytecode.Instructions.Add(Instruction.LOADBOOL(1, false));

            // If reg[0] != reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOADBOOL(1, true));

            // If reg[0] == reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOADBOOL(0, false));

            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertBoolResult(vm, false, 0);
        }

        [Test]
        public void LT()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 32));
            bytecode.Instructions.Add(Instruction.LOADI(1, 42));

            // If reg[0] < reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.LT(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOADI(0, 64));

            // If reg[0] >= reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.LT(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOADI(0, 42));

            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertIntegerResult(vm, 42, 0);
        }

        [Test]
        public void LE()
        {
            var bytecode = new LuaFunction(new List<uint>(), new List<LuaObject>(), 3);

            bytecode.Instructions.Add(Instruction.LOADI(0, 42));
            bytecode.Instructions.Add(Instruction.LOADI(1, 42));

            // If reg[0] <= reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.LT(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOADI(0, 64));

            // If reg[0] > reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.LT(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOADI(0, 42));

            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertIntegerResult(vm, 42, 0);
        }
    }
}
