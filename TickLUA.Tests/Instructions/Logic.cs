using TickLUA.VM.Objects;

namespace TickLUA_Tests.Instructions
{
    public class Logic
    {
        [Test]
        public void TEST()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 16));
            bytecode.Instructions.Add(Instruction.LOAD_TRUE(2));

            // If register 2 is true, execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.TEST(2, true));
            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 32));

            // If register 2 is false, execute the next instruction (should skip it)
            bytecode.Instructions.Add(Instruction.TEST(2, false));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 64));

            bytecode.Instructions.Add(Instruction.RETURN(0, 2));

            var vm = Utils.Run(bytecode, 8);

            Utils.AssertIntegerResult(vm, 32, 0);
            Utils.AssertIntegerResult(vm, 16, 1);
        }

        [Test]
        public void TESTSET()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_BOOL(0, true));
            bytecode.Instructions.Add(Instruction.LOAD_BOOL(1, false));

            // If register 1 is false, assign its value to register 2 and execute the next instruction
            // Else, skip the next instruction, no assignment.
            // (should assign it)
            bytecode.Instructions.Add(Instruction.TESTSET(2, 1, false));
            bytecode.Instructions.Add(Instruction.JMP(1));
            bytecode.Instructions.Add(Instruction.MOVE(2, 0));

            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            var vm = Utils.Run(bytecode, 5);

            Utils.AssertBoolResult(vm, false, 0);
        }

        [Test]
        public void EQ_Numbers()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 16));

            // If reg[0] == reg[1], execute the next instruction (should skip it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 42));

            // If reg[0] != reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 64));

            bytecode.Instructions.Add(Instruction.RETURN(1, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertIntegerResult(vm, 64, 0);
        }

        [Test]
        public void EQ_Nil()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));
            bytecode.Instructions.Add(Instruction.LOAD_NIL(1));

            // If reg[0] == nil, execute the next instruction (should skip it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 32));

            // If reg[0] != nil, execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 64));

            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertIntegerResult(vm, 64, 0);
        }

        [Test]
        public void EQ_Bool()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_BOOL(0, true));
            bytecode.Instructions.Add(Instruction.LOAD_BOOL(1, false));

            // If reg[0] != reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOAD_BOOL(1, true));

            // If reg[0] == reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.EQ(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOAD_BOOL(0, false));

            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertBoolResult(vm, false, 0);
        }

        [Test]
        public void LT()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 32));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 42));

            // If reg[0] < reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.LT(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 64));

            // If reg[0] >= reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.LT(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));

            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertIntegerResult(vm, 42, 0);
        }

        [Test]
        public void LE()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 42));

            // If reg[0] <= reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.LE(0, 1, true));
            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 64));

            // If reg[0] > reg[1], execute the next instruction (should execute it)
            bytecode.Instructions.Add(Instruction.LE(0, 1, false));
            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));

            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertIntegerResult(vm, 42, 0);
        }

        [Test]
        public void NOT()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 42));
            bytecode.Instructions.Add(Instruction.LOAD_BOOL(1, false));
            bytecode.Instructions.Add(Instruction.LOAD_NIL(2));

            bytecode.Instructions.Add(Instruction.NOT(0, 0)); // not 42 -> false
            bytecode.Instructions.Add(Instruction.NOT(1, 1)); // not false -> true
            bytecode.Instructions.Add(Instruction.NOT(2, 2)); // not nil -> true

            bytecode.Instructions.Add(Instruction.RETURN(0, 3));

            var vm = Utils.Run(bytecode, 7);

            Utils.AssertBoolResult(vm, false, 0);
            Utils.AssertBoolResult(vm, true, 1);
            Utils.AssertBoolResult(vm, true, 2);
        }
    }
}
