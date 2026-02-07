using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickLUA.VM.Objects;

namespace TickLUA_Tests.Instructions
{
    internal class Loops
    {
        [Test]
        public void FORPREP()
        {
            var bytecode = new LuaFunction(4);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, 1));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 5));
            bytecode.Instructions.Add(Instruction.LOAD_INT(2, 2));
            bytecode.Instructions.Add(Instruction.LOAD_NIL(3));

            bytecode.Instructions.Add(Instruction.FORPREP(0, 1));

            bytecode.Instructions.Add(Instruction.NOP());

            bytecode.Instructions.Add(Instruction.RETURN(0, 4));

            var vm = Utils.Run(bytecode, 6);

            Utils.AssertIntegerResult(vm, -1, 0);
            Utils.AssertIntegerResult(vm, 5, 1);
            Utils.AssertIntegerResult(vm, 2, 2);
            Utils.AssertNilResult(vm, 3);
        }

        [Test]
        public void FORLOOP()
        {
            var bytecode = new LuaFunction(4);

            bytecode.Instructions.Add(Instruction.LOAD_INT(0, -1));
            bytecode.Instructions.Add(Instruction.LOAD_INT(1, 5));
            bytecode.Instructions.Add(Instruction.LOAD_INT(2, 2));
            bytecode.Instructions.Add(Instruction.LOAD_NIL(3));

            bytecode.Instructions.Add(Instruction.FORLOOP(0, -1));

            bytecode.Instructions.Add(Instruction.RETURN(0, 4));

            var vm = Utils.Run(bytecode, 9);

            Utils.AssertIntegerResult(vm, 5, 0);
            Utils.AssertIntegerResult(vm, 5, 1);
            Utils.AssertIntegerResult(vm, 2, 2);
            Utils.AssertIntegerResult(vm, 5, 3);
        }
    }
}
