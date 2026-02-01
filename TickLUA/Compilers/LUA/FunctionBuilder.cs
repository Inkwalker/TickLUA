using System.Collections.Generic;
using TickLUA.VM;
using TickLUA.VM.Objects;

namespace TickLUA.Compilers.LUA
{
    internal class FunctionBuilder
    {
        private List<Instruction> instructions = new List<Instruction>();
        private List<LuaObject> constants = new List<LuaObject>();
        private LuaFunction.Metadata metadata = new LuaFunction.Metadata();

        private List<BlockFrame> blocks = new List<BlockFrame>();

        private RegisterAllocator allocator = new RegisterAllocator();

        private BlockFrame TopBlock => blocks[blocks.Count - 1];

        public int InstructionCount => instructions.Count;

        public LuaFunction Finish()
        {
            var func = new LuaFunction(
                instructions,
                constants,
                metadata,
                allocator.MaxRegisters
            );

            return func;
        }

        /// <summary>
        /// Add instruction to the bytecode.
        /// </summary>
        /// <returns>Address of the instruction in the bytecode</returns>
        public int AddInstruction(Instruction instruction, ushort line)
        {
            instructions.Add(instruction);
            metadata.Lines.Add(line);
            return instructions.Count - 1;
        }

        /// <summary>
        /// Set instruction in the bytecode at given address.
        /// Must be within <see cref="InstructionCount"/>range.
        /// </summary>
        /// <returns>Old instruction at <paramref name="address"/></returns>
        public Instruction SetInstruction(int address, Instruction instruction)
        {
            var old_inst = instructions[address];
            instructions[address] = instruction;
            return old_inst;
        }

        public ushort AddConstant(LuaObject constant)
        {
            for (int i = 0; i < constants.Count; i++)
            {
                if (constants[i] == constant) return (ushort)i;
            }

            constants.Add(constant);
            int index = constants.Count - 1;

            if (index > ushort.MaxValue)
                // TODO: add source code location
                throw new CompilationException("Too many constants", 1, 1);

            return (ushort)index;
        }

        public byte AllocateRegisters(int count)
        {
            var index = allocator.Allocate(count);
            var block = TopBlock;

            for (int i = index; i < index + count; i++)
                block.AllocatedRegisters.Add(i);

            if (index >= byte.MaxValue)
                // TODO: add source code location
                throw new CompilationException("Out of registers", 1, 1);
            return (byte)index;
        }

        public void DeallocateRegisters(byte start_reg, int count)
        {
            var block = TopBlock;

            for (int i = start_reg; i < start_reg + count; i++)
            {
                block.AllocatedRegisters.Remove(i);
            }

            allocator.Deallocate(start_reg, count);
        }

        public byte AllocateVariable(string name)
        {
            var index = allocator.Allocate(1);
            var block = TopBlock;

            block.AllocatedRegisters.Add(index);
            block.LocalsLookup[name] = index;

            if (index >= byte.MaxValue)
                // TODO: add source code location
                throw new CompilationException("Out of registers", 1, 1);
            return (byte)index;
        }

        public void NameRegister(int index, string name)
        {
            TopBlock.LocalsLookup[name] = index;
        }

        /// <summary>
        /// Get register index allocated for the variable
        /// </summary>
        /// <returns>Register index or -1 if not defined in the function</returns>
        public int ResolveVariable(string name)
        {
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                if (blocks[i].LocalsLookup.TryGetValue(name, out var reg))
                    return reg;
            }

            return -1;
        }

        public void BlockStart()
        {
            blocks.Add(new BlockFrame());
        }

        public void BlockEnd()
        {
            var frame = blocks[blocks.Count - 1];

            foreach (int reg in frame.AllocatedRegisters)
                allocator.Deallocate(reg);
        }

        private class BlockFrame
        {
            /// <summary>
            /// Registers that are allocated for this block
            /// </summary>
            public HashSet<int> AllocatedRegisters = new HashSet<int>();
            /// <summary>
            /// Lookup table register indexes by variable name
            /// </summary>
            public Dictionary<string, int> LocalsLookup = new Dictionary<string, int>();

            public bool IsLocal(string name) => LocalsLookup.ContainsKey(name);
        }

        private class RegisterAllocator
        {
            private List<bool> in_use = new List<bool>();

            public int MaxRegisters => in_use.Count;

            /// <summary>
            /// Allocate <paramref name="count"/> registers. All registers will be sequential.
            /// </summary>
            /// <returns>Index of the first allocated register</returns>
            public int Allocate(int count = 1)
            {
                // check all previously allocated registers
                // and try to reuse them
                for (int i = 0; i < in_use.Count; i++)
                {
                    int last_used = AreNotInUse(i, count);
                    if (last_used < 0)
                    {
                        SetInUse(i, count);
                        return i;
                    }
                    else
                    {
                        i = last_used; // will be incremented on the next loop
                        continue;
                    }
                }

                // if we are here then all registers are in use
                // add more

                int index = in_use.Count;
                SetInUse(index, count);
                return index;
            }

            public void Deallocate(int reg_index, int count = 1)
            {
                for (int i = reg_index; i < reg_index + count; i++)
                {
                    if (i >= in_use.Count) return;
                    in_use[i] = false;
                }
            }

            private void SetInUse(int reg_index, int count)
            {
                for (int i = reg_index; i < reg_index + count; i++)
                {
                    if (i >= in_use.Count)
                        in_use.Add(true);
                    else
                        in_use[i] = true;
                }
            }

            // Returns first in use register index or -1 if all available
            private int AreNotInUse(int reg_index, int count)
            {
                for (int i = reg_index; i < reg_index + count; i++)
                {
                    if (i >= in_use.Count) return -1;
                    if (in_use[i]) return i;
                }

                return -1;
            }
        }
    }
}
