using System;
using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    internal class StackFrame
    {
        public LuaObject[] Registers { get; }
        public IReadOnlyList<LuaObject> Constants => Function.Constants;

        public LuaFunction Function { get; private set; }
        public int PC { get; set; }

        public bool IsFinished => PC >= Function.Instructions.Count;
        public LuaObject[] Results { get; private set; }

        public StackFrame(LuaFunction function) 
        { 
            Function = function;
            Registers = new LuaObject[function.RegisterCount];
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
            Array.Copy(Registers, start_reg, Results, 0, count);
        }
    }
}
