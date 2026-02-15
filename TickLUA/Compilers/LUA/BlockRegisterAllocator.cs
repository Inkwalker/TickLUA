using System;

namespace TickLUA.Compilers.LUA
{
    internal class BlockRegisterAllocator
    {
        private IndexableStack<RegisterInfo> registers;

        public int MaxRegisters { get; private set; }
        public int RegisterCount => registers.Count;
        public int Offset { get; private set; }

        public BlockRegisterAllocator(int offset) 
        {
            registers = new IndexableStack<RegisterInfo>();
            Offset = offset;
        }

        /// <summary>
        /// Allocate <paramref name="count"/> registers. All registers will be sequential
        /// </summary>
        /// <returns>Absolute index of the first allocated register</returns>
        public int Allocate(int count = 1)
        {
            // we always allocate register at the end of the stack

            int index = registers.Count;

            for (int i = 0; i < count; i++)
            {
                registers.Push(new RegisterInfo(null));
            }

            MaxRegisters = Math.Max(MaxRegisters, registers.Count);

            return index + Offset;
        }

        /// <summary>
        /// Free <paramref name="count"/> registers from the stack
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if there are not enough registers to free</exception>
        public void Free(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (registers.Count == 0)
                    throw new System.InvalidOperationException("Internal error: No registers to free");

                registers.Pop();
            }
        }

        /// <summary>
        /// Associates a variable name with a register at the specified index.
        /// </summary>
        /// <param name="reg_index">Absolute index of the register to name.</param>
        /// <param name="var_name">The variable name to assign to the register.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the register index is out of bounds.</exception>
        public void NameRegister(int reg_index, string var_name)
        {
            int local_index = GetLocalIndex(reg_index);
            if (local_index < 0 || local_index >= registers.Count)
                throw new System.ArgumentOutOfRangeException("Internal error: Register index out of bounds");

            registers[local_index] = new RegisterInfo(var_name) { Escapes = registers[local_index].Escapes };
        }

        /// <summary>
        /// Searches for a register by variable name and returns its absolute index.
        /// </summary>
        /// <param name="var_name">The variable name to search for.</param>
        /// <returns>The absolute index of the register if found; otherwise, -1.</returns>
        public int FindRegisterByName(string var_name)
        {
            for (int i = 0; i < registers.Count; i++)
            {
                if (registers[i].VarName == var_name)
                    return GetAbsoluteIndex(i);
            }
            return -1;
        }

        /// <summary>
        /// Mark register as upvalue
        /// </summary>
        /// <param name="reg_index">Absolute index of the register</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the register index is out of bounds</exception>"
        public void MarkEscaping(int reg_index)
        {
            int local_index = GetLocalIndex(reg_index);

            if (local_index < 0 || local_index >= registers.Count)
                throw new System.ArgumentOutOfRangeException("Internal error: Register index out of bounds");

            registers[local_index] = new RegisterInfo(registers[local_index].VarName) { Escapes = true };
        }

        /// <summary>
        /// Is the register marked as an escaping upvalue?
        /// </summary>
        /// <param name="reg_index">Absolute index of the register</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the register index is out of bounds</exception>""
        public bool IsEscaping(int reg_index)
        {
            int local_index = GetLocalIndex(reg_index);

            if (local_index < 0 || local_index >= registers.Count)
                throw new System.ArgumentOutOfRangeException("Internal error: Register index out of bounds");

            return registers[local_index].Escapes;
        }

        public bool HasEscapingVars()
        {
            for (int i = 0; i < registers.Count; i++)
            {
                if (registers[i].Escapes)
                    return true;
            }
            return false;
        }

        private int GetLocalIndex(int reg_index)
        {
            int last_index = registers.Count - 1;
            return last_index - (reg_index - Offset);
        }

        private int GetAbsoluteIndex(int local_index)
        {
            int last_index = registers.Count - 1;
            return (last_index - local_index) + Offset;
        }

        private struct RegisterInfo
        {
            public string VarName { get; set; }
            public bool IsTemp => string.IsNullOrEmpty(VarName);
            public bool Escapes { get; set; }

            public RegisterInfo(string var_name)
            {
                VarName = var_name;
                Escapes = false;
            }
        }
    }
}
