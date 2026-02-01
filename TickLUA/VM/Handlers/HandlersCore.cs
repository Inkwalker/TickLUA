using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersCore
    {
        internal static void NOP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // No operation
        }

        internal static void MOVE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;
            byte b = instruction.B;

            // Move value from register b to register a
            frame.Registers[a] = frame.Registers[b];
        }

        internal static void LOAD_CONST(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte  a   = instruction.A;
            ushort bx = instruction.Bx;

            // Load constant at index bx into register a
            frame.Registers[a] = frame.Constants[bx];
        }

        internal static void LOAD_INT(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte  a = instruction.A;
            short b = instruction.BxSigned;

            // Load integer b into register a
            frame.Registers[a] = new NumberObject(b);
        }

        internal static void LOAD_TRUE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;

            // Load boolean true into register a
            frame.Registers[a] = BooleanObject.True;
        }

        internal static void LOAD_FALSE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;

            // Load boolean false into register a
            frame.Registers[a] = BooleanObject.False;
        }

        internal static void LOAD_FALSE_SKIP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;

            // Load boolean false into register a and skip next instruction
            frame.Registers[a] = BooleanObject.False;
            frame.PC++;
        }

        internal static void LOAD_NIL(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;
            byte b = instruction.B;

            // Load nil into registers. a - start register, b - number of registers

            for (int i = 0; i < b; i++)
            {
                frame.Registers[a + i] = NilObject.Nil;
            }
        }

        internal static void RETURN(TickVM vm, StackFrame frame, Instruction instruction)
        {
            frame.PC = frame.Function.Instructions.Count;

            byte a = instruction.A;
            ushort b = instruction.Bx;

            frame.SetResults(a, b - 1);
        }

        internal static void JMP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int delta = instruction.AxSigned;
            frame.PC += delta;
        }
    }
}

namespace TickLUA.VM
{
    internal partial struct Instruction
    {
        internal static Instruction NOP() => new Instruction(Opcode.NOP);
        internal static Instruction MOVE(byte dest_reg, byte src_reg) => new Instruction(Opcode.MOVE, dest_reg, src_reg, 0);
        internal static Instruction LOAD_CONST(byte dest_reg, ushort const_index) => new Instruction(Opcode.LOAD_CONST, dest_reg, (short)const_index);
        internal static Instruction LOAD_INT(byte dest_reg, short integer) => new Instruction(Opcode.LOAD_INT, dest_reg, integer);
        internal static Instruction LOAD_TRUE(byte dest_reg) => new Instruction(Opcode.LOAD_TRUE, dest_reg, 0, 0);
        internal static Instruction LOAD_FALSE(byte dest_reg) => new Instruction(Opcode.LOAD_FALSE, dest_reg, 0, 0);
        internal static Instruction LOAD_FALSE_SKIP(byte dest_reg) => new Instruction(Opcode.LOAD_FALSE_SKIP, dest_reg, 0, 0);
        internal static Instruction LOAD_BOOL(byte dest_reg, bool value) => value ? LOAD_TRUE(dest_reg) : LOAD_FALSE(dest_reg);
        internal static Instruction LOAD_NIL(byte start_reg, byte count = 1) => new Instruction(Opcode.LOAD_NIL, start_reg, count, 0);
        internal static Instruction RETURN(byte start_reg, int count)
        {
            int c = count < -1 ? -1 : count;
            return new Instruction(Opcode.RETURN, start_reg, (ushort)(c + 1));
        }
        internal static Instruction JMP(int offset) => new Instruction(Opcode.JMP, offset);
    }
}
