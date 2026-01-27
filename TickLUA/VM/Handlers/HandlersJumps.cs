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

        internal static void EQ(TickVM vm, StackFrame frame, uint instruction)
        {
            int reg_a = Instruction.GetA(instruction);
            int reg_b = Instruction.GetB(instruction);
            bool expected = Instruction.GetC(instruction) != 0;

            var obj_a = frame.Registers[reg_a];
            var obj_b = frame.Registers[reg_b];

            if ((obj_a == obj_b) != expected)
            {
                frame.PC++;
            }
        }
    }
}
