using TickLUA.VM.Objects;

namespace TickLUA_Tests.Instructions
{
    internal class Table
    {
        [Test]
        public void NEW_TABLE()
        {
            var bytecode = new LuaFunction(1);

            bytecode.Instructions.Add(Instruction.NEW_TABLE(0));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 2);

            Utils.AssertTableResult(vm);
        }

        [Test]
        public void SET_TABLE()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.NEW_TABLE(0));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 5));
            bytecode.Instructions.Add(Instruction.LOAD_BOOL(2, true));
            bytecode.Instructions.Add(Instruction.SET_TABLE(0, 2, 1));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 5);

            Utils.AssertTableResult(vm, BooleanObject.True, new NumberObject(5));
        }

        [Test]
        public void GET_TABLE()
        {
            var bytecode = new LuaFunction(4);

            bytecode.Instructions.Add(Instruction.NEW_TABLE(0));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 5));
            bytecode.Instructions.Add(Instruction.LOAD_BOOL(2, true));
            bytecode.Instructions.Add(Instruction.SET_TABLE(0, 2, 1));
            bytecode.Instructions.Add(Instruction.GET_TABLE(3, 0, 2));
            bytecode.Instructions.Add(Instruction.RETURN(3, 1));

            var vm = Utils.Run(bytecode, 6);

            Utils.AssertIntegerResult(vm, 5);
        }

        [Test]
        public void SET_LIST()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Instructions.Add(Instruction.NEW_TABLE(0));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 5));
            bytecode.Instructions.Add(Instruction.LOAD_INT(2, 6));
            bytecode.Instructions.Add(Instruction.SET_LIST(0, 1, 2));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 5);

            Utils.AssertTableResult(vm, new NumberObject(1), new NumberObject(5));
            Utils.AssertTableResult(vm, new NumberObject(2), new NumberObject(6));
        }

        [Test]
        public void SET_FIELD()
        {
            var bytecode = new LuaFunction(2);

            bytecode.Constants.Add(BooleanObject.False);

            bytecode.Instructions.Add(Instruction.NEW_TABLE(0));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 5));
            bytecode.Instructions.Add(Instruction.SET_FIELD(0, 0, 1));
            bytecode.Instructions.Add(Instruction.RETURN(0, 1));

            var vm = Utils.Run(bytecode, 4);

            Utils.AssertTableResult(vm, BooleanObject.False, new NumberObject(5));
        }

        [Test]
        public void GET_FIELD()
        {
            var bytecode = new LuaFunction(3);

            bytecode.Constants.Add(BooleanObject.False);

            bytecode.Instructions.Add(Instruction.NEW_TABLE(0));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 5));
            bytecode.Instructions.Add(Instruction.SET_FIELD(0, 0, 1));
            bytecode.Instructions.Add(Instruction.GET_FIELD(2, 0, 0));
            bytecode.Instructions.Add(Instruction.RETURN(2, 1));

            var vm = Utils.Run(bytecode, 5);

            Utils.AssertIntegerResult(vm, 5);
        }
    }
}
