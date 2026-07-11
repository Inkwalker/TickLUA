using System;
using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    internal class StackFrame
    {
        public RegisterCell[] Registers { get; private set; }
        public IReadOnlyList<LuaObject> Constants => Function.Constants;

        public LuaFunction Function { get; private set; }
        public int PC { get; set; }

        public bool IsFinished => PC >= Function.Instructions.Count;
        public RegisterCell[] Upvalues { get; }

        public byte ResultsStartRegister { get; set; }
        public int ResultsCount { get; set; }

        /// <summary>
        /// Set on frames pushed by pcall. On a normal return the results get
        /// true prepended; on error unwinding this frame is the catch boundary.
        /// </summary>
        public bool IsProtected { get; set; }

        /// <summary>
        /// Set on frames pushed for metamethod handlers whose result must not
        /// land in a register (e.g. __lt drives a conditional skip). When set,
        /// RETURN passes the results here instead of calling WriteResults.
        /// Never fires on error unwinding — errors go to the pcall boundary.
        /// </summary>
        internal ResultsSinkDelegate Sink { get; set; }

        /// <summary>
        /// One past the last register holding a value of a variable-count CALL.
        /// Set when a caller returns a variable number of results into this frame,
        /// consumed by a following multi value RETURN.
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// Extra arguments passed beyond the declared parameters of a vararg function.
        /// Read by the VARARG instruction.
        /// </summary>
        public LuaObject[] Varargs { get; set; } = new LuaObject[0];

        public StackFrame(LuaFunction function, RegisterCell[] upvalues)
        {
            Function = function;
            Registers = new RegisterCell[function.RegisterCount];

            for (int i = 0; i < Registers.Length; i++)
                Registers[i] = new RegisterCell();

            Upvalues = upvalues;
        }

        public bool GrowRegisters(int newSize)
        {
            if (newSize <= Registers.Length)
                return false;
            var newRegisters = new RegisterCell[newSize];
            Array.Copy(Registers, newRegisters, Registers.Length);
            for (int i = Registers.Length; i < newSize; i++)
                newRegisters[i] = new RegisterCell();
            Registers = newRegisters;
            return true;
        }

        public bool ShrinkRegisters(int newSize)
        {
            if (newSize >= Registers.Length)
                return false;
            var newRegisters = new RegisterCell[newSize];
            Array.Copy(Registers, newRegisters, newSize);
            Registers = newRegisters;
            return true;
        }

        public Instruction Step()
        {
            var instruction = Function.Instructions[PC];
            PC += 1;
            return instruction;
        }
    }
}
