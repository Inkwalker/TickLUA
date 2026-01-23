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
    }
}
