using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersCore
    {
        internal static void NOP(TickVM vm, StackFrame frame, uint instruction)
        {
            // No operation
        }

        internal static void MOVE(TickVM vm, StackFrame frame, uint instruction)
        {
            byte a = Instruction.GetA(instruction);
            byte b = Instruction.GetB(instruction);

            // Move value from register b to register a
            frame.Registers[a] = frame.Registers[b];
        }

        internal static void LOAD_CONST(TickVM vm, StackFrame frame, uint instruction)
        {
            byte  a   = Instruction.GetA(instruction);
            ushort bx = Instruction.GetBx(instruction);

            // Load constant at index bx into register a
            frame.Registers[a] = frame.Constants[bx];
        }

        internal static void LOAD_INT(TickVM vm, StackFrame frame, uint instruction)
        {
            byte  a = Instruction.GetA(instruction);
            short b = Instruction.GetBxSigned(instruction);

            // Load integer b into register a
            frame.Registers[a] = new NumberObject(b);
        }

        internal static void LOAD_TRUE(TickVM vm, StackFrame frame, uint instruction)
        {
            byte a = Instruction.GetA(instruction);

            // Load boolean true into register a
            frame.Registers[a] = BooleanObject.True;
        }

        internal static void LOAD_FALSE(TickVM vm, StackFrame frame, uint instruction)
        {
            byte a = Instruction.GetA(instruction);

            // Load boolean false into register a
            frame.Registers[a] = BooleanObject.False;
        }

        internal static void LOAD_FALSE_SKIP(TickVM vm, StackFrame frame, uint instruction)
        {
            byte a = Instruction.GetA(instruction);

            // Load boolean false into register a and skip next instruction
            frame.Registers[a] = BooleanObject.False;
            frame.PC++;
        }

        internal static void LOAD_NIL(TickVM vm, StackFrame frame, uint instruction)
        {
            byte a = Instruction.GetA(instruction);
            byte b = Instruction.GetB(instruction);

            // Load nil into registers. a - start register, b - number of registers

            for (int i = 0; i < b; i++)
            {
                frame.Registers[a + i] = NilObject.Nil;
            }
        }

        internal static void RETURN(TickVM vm, StackFrame frame, uint instruction)
        {
            frame.PC = frame.Function.Instructions.Count;

            byte a = Instruction.GetA(instruction);
            ushort b = Instruction.GetBx(instruction);

            frame.SetResults(a, b - 1);
        }
    }
}
