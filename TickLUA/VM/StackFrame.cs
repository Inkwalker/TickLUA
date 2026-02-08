using System;
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
        public LuaObject[] Results { get; private set; }
        public RegisterCell[] Upvalues { get; }

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

        /// <summary>
        /// Prepare return values of the function.
        /// </summary>
        /// <param name="start_reg">Starting from register index</param>
        /// <param name="count">
        /// How many registers to include. 
        /// Set to -1 to include all allocated registers after <paramref name="start_reg"/>
        /// </param>
        /// <returns>Array of values in the order of allocated registers</returns>
        /// <remarks>Might not set correct values if called before the stack frame finishes</remarks>
        public void SetResults(int start_reg, int count)
        {
            if (count == 0) Results = new LuaObject[0];

            if (count < 0)
                count = Registers.Length - start_reg;

            Results = new LuaObject[count];

            for (int i = 0; i < count; i++)
            {
                Results[i] = Registers[i + start_reg].Value;
            }
        }
    }
}
