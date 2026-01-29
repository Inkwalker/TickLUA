using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersLogic
    {
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

        internal static void TESTSET(TickVM vm, StackFrame frame, uint instruction)
        {
            int dest_reg = Instruction.GetA(instruction);
            int test_reg = Instruction.GetB(instruction);
            bool expected = Instruction.GetC(instruction) != 0;

            var test_obj = frame.Registers[test_reg];

            if ((bool)test_obj.ToBooleanObject() != expected)
            {
                frame.PC++;
            }
            else
            {
                frame.Registers[dest_reg] = test_obj;
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

        internal static void LT(TickVM vm, StackFrame frame, uint instruction)
        {
            int reg_a = Instruction.GetA(instruction);
            int reg_b = Instruction.GetB(instruction);
            bool expected = Instruction.GetC(instruction) != 0;

            var obj_a = frame.Registers[reg_a];
            var obj_b = frame.Registers[reg_b];

            var number_a = obj_a as NumberObject;
            var number_b = obj_b as NumberObject;

            if (number_a == null || number_b == null)
            {
                // TODO: proper error handling
                throw new System.Exception("Attempt to compare non-number values.");
            }

            if ((number_a < number_b) != expected)
            {
                frame.PC++;
            }
        }

        internal static void LE(TickVM vm, StackFrame frame, uint instruction)
        {
            int reg_a = Instruction.GetA(instruction);
            int reg_b = Instruction.GetB(instruction);
            bool expected = Instruction.GetC(instruction) != 0;

            var obj_a = frame.Registers[reg_a];
            var obj_b = frame.Registers[reg_b];

            var number_a = obj_a as NumberObject;
            var number_b = obj_b as NumberObject;

            if (number_a == null || number_b == null)
            {
                // TODO: proper error handling
                throw new System.Exception("Attempt to compare non-number values.");
            }

            if ((number_a <= number_b) != expected)
            {
                frame.PC++;
            }
        }

        internal static void NOT(TickVM vm, StackFrame frame, uint instruction)
        {
            int a = Instruction.GetA(instruction);
            int b = Instruction.GetB(instruction);
            var obj = frame.Registers[b];

            frame.Registers[a] = !obj.ToBooleanObject();
        }
    }
}
