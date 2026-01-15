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

        internal static void LOADK(TickVM vm, StackFrame frame, uint instruction)
        {
            byte  a   = Instruction.GetA(instruction);
            ushort bx = Instruction.GetBx(instruction);

            // Load constant at index bx into register a
            frame.Registers[a] = frame.Constants[bx];
        }

        internal static void LOADI(TickVM vm, StackFrame frame, uint instruction)
        {
            byte  a = Instruction.GetA(instruction);
            short b = Instruction.GetBxSigned(instruction);

            // Load integer b into register a
            frame.Registers[a] = new IntegerObject(b);
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
