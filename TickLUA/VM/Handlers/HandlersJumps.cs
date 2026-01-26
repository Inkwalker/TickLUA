using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersJumps
    {
        internal static void JMP(TickVM vm, StackFrame frame, uint instruction)
        {
            int delta = Instruction.GetAxSigned(instruction);
            frame.PC += delta;
        }

        internal static void TEST(TickVM vm, StackFrame frame, uint instruction)
        {
            int reg = Instruction.GetA(instruction);
            bool expected = Instruction.GetB(instruction) != 0;

            var obj = frame.Registers[reg];

            if ((bool)obj.ToBooleanObject() != expected)
            {
                frame.PC++;
            }
        }
    }
}
