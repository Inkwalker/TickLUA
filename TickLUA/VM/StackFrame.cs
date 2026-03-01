using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    internal class StackFrame
    {
        public RegisterCell[] Registers { get; }
        public IReadOnlyList<LuaObject> Constants => Function.Constants;

        public LuaFunction Function { get; private set; }
        public int PC { get; set; }

        public bool IsFinished => PC >= Function.Instructions.Count;
        public RegisterCell[] Upvalues { get; }

        public byte ResultsStartRegister { get; set; }
        public int ResultsCount { get; set; }

        public StackFrame(LuaFunction function, RegisterCell[] upvalues)
        {
            Function = function;
            Registers = new RegisterCell[function.RegisterCount];

            for (int i = 0; i < Registers.Length; i++)
                Registers[i] = new RegisterCell();

            Upvalues = upvalues;
        }

        public Instruction Step()
        {
            var instruction = Function.Instructions[PC];
            PC += 1;
            return instruction;
        }
    }
}
