using System;
using System.Collections.Generic;
using TickLUA.VM;
using TickLUA.VM.Objects;

namespace TickLUA.Compilers.LUA
{
    internal class FunctionBuilder
    {
        private LuaFunction function;

        private IndexableStack<BlockFrame> blocks;

        private List<FunctionBuilder> nested_builders = new List<FunctionBuilder>();

        public int InstructionCount => function.InstructionCount;
        public FunctionBuilder Parent { get; private set; } = null;
        public int MaxRegisters { get; private set; }

        public FunctionBuilder(FunctionBuilder parent = null)
        {
            Parent = parent;

            function = new LuaFunction(0);
            blocks = new IndexableStack<BlockFrame>();
        }

        public LuaFunction Finish()
        {
            function.RegisterCount = MaxRegisters;

            foreach (var c in nested_builders)
            {
                function.NestedFunctions.Add(c.Finish());
            }

            return function;
        }

        /// <summary>
        /// Add instruction to the bytecode.
        /// </summary>
        /// <param name="line">Source code line number for debugging purposes</param>
        /// <returns>Address of the instruction in the bytecode</returns>
        public int AddInstruction(Instruction instruction, ushort line)
        {
            function.Instructions.Add(instruction);
            function.Meta.Lines.Add(line);
            return InstructionCount - 1;
        }

        /// <summary>
        /// Set instruction in the bytecode at given address.
        /// Must be within <see cref="InstructionCount"/>range.
        /// </summary>
        /// <returns>Old instruction at <paramref name="address"/></returns>
        public Instruction SetInstruction(int address, Instruction instruction)
        {
            var old_inst = function.Instructions[address];
            function.Instructions[address] = instruction;
            return old_inst;
        }

        public ushort AddConstant(LuaObject constant)
        {
            for (int i = 0; i < function.Constants.Count; i++)
            {
                if (function.Constants[i].Equals(constant)) return (ushort)i;
            }

            function.Constants.Add(constant);
            int index = function.Constants.Count - 1;

            if (index > ushort.MaxValue)
                // TODO: add source code location
                throw new CompilationException("Too many constants", 1, 1);

            return (ushort)index;
        }

        /// <summary>
        /// Allocate registers for the variables in the current block.
        /// </summary>
        /// <exception cref="CompilationException">When allocated more than 255 registers</exception>
        public byte AllocateRegisters(int count)
        {
            var block = blocks.Peek();
            var index = block.Allocator.Allocate(count);

            if (index >= byte.MaxValue)
                // TODO: add source code location
                throw new CompilationException("Out of registers", 1, 1);

            MaxRegisters = Math.Max(MaxRegisters, index + count);

            return (byte)index;
        }

        /// <summary>
        /// Free registers allocated in the current block.
        /// </summary>
        /// <exception cref="InvalidOperationException">When trying to free more registers than allocated</exception>"
        public void FreeRegisters(int count = 1)
        {
            var block = blocks.Peek();
            block.Allocator.Free(count);
        }

        /// <summary>
        /// Allocates a register for a variable and assigns it the specified name.
        /// </summary>
        /// <returns>The index of the allocated register</returns>
        /// <exception cref="CompilationException">When allocated more than 255 registers</exception>"
        public byte AllocateVariable(string name)
        {
            var index = AllocateRegisters(1);
            NameRegister(index, name);
            return (byte)index;
        }

        /// <summary>
        /// Name a register allocated with the specified variable name.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">When the register index is out of bounds</exception>
        public void NameRegister(int index, string name)
        {
            if (index < 0 || index >= MaxRegisters)
                throw new ArgumentOutOfRangeException(nameof(index), "Register index is out of bounds");

            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];

                if (index >= block.Allocator.Offset)
                {
                    block.Allocator.NameRegister(index, name);
                    break;
                }
            }
        }

        /// <summary>
        /// Get register index allocated for the variable
        /// </summary>
        /// <returns>Register index or -1 if not defined in the function</returns>
        public int ResolveVariable(string name)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                int reg = block.Allocator.FindRegisterByName(name);
                if (reg >= 0)
                    return reg;
            }

            return -1;
        }

        public void MarkEscaping(int index)
        {
            if (index < 0 || index >= MaxRegisters)
                throw new ArgumentOutOfRangeException(nameof(index), "Register index is out of bounds");

            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];

                if (index > block.Allocator.Offset)
                    block.Allocator.MarkEscaping(index);
            }
        }

        public bool BlockHasEscapingVars()
        {
            return blocks.Peek().Allocator.HasEscapingVars();
        }

        public void BlockStart()
        {
            int offset = 0;
            if (blocks.Count > 0) 
            {
                var b = blocks.Peek();
                offset = b.Allocator.Offset + b.Allocator.RegisterCount;
            }
            blocks.Push(new BlockFrame(offset));
        }

        public void BlockEnd()
        {
            blocks.Pop();
        }

        public FunctionBuilder CreateNestedFunction(out int func_index)
        {
            var nested = new FunctionBuilder(this);
            nested_builders.Add(nested);
            func_index = nested_builders.Count - 1;
            return nested;
        }

        private class BlockFrame
        {
            public BlockRegisterAllocator Allocator { get; }

            public BlockFrame(int register_offset)
            {
                Allocator = new BlockRegisterAllocator(register_offset);
            }
        }
    }
}
