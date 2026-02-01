using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersLogic
    {
        internal static void TEST(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg       = instruction.A;
            bool expected = instruction.B != 0;

            var obj = frame.Registers[reg];

            if ((bool)obj.ToBooleanObject() != expected)
            {
                frame.PC++;
            }
        }

        internal static void TESTSET(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int dest_reg  = instruction.A;
            int test_reg  = instruction.B;
            bool expected = instruction.C != 0;

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

        internal static void EQ(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_a     = instruction.A;
            int reg_b     = instruction.B;
            bool expected = instruction.C != 0;

            var obj_a = frame.Registers[reg_a];
            var obj_b = frame.Registers[reg_b];

            if ((obj_a == obj_b) != expected)
            {
                frame.PC++;
            }
        }

        internal static void LT(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_a     = instruction.A;
            int reg_b     = instruction.B;
            bool expected = instruction.C != 0;

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

        internal static void LE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_a     = instruction.A;
            int reg_b     = instruction.B;
            bool expected = instruction.C != 0;

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

        internal static void NOT(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int a = instruction.A;
            int b = instruction.B;
            var obj = frame.Registers[b];

            frame.Registers[a] = !obj.ToBooleanObject();
        }
    }
}

namespace TickLUA.VM
{
    internal partial struct Instruction
    {
        internal static Instruction TEST(byte reg, bool expected) => new Instruction(Opcode.TEST, reg, (byte)(expected ? 1 : 0));
        internal static Instruction TESTSET(byte dest_reg, byte test_reg, bool expected) => new Instruction(Opcode.TESTSET, dest_reg, test_reg, (byte)(expected ? 1 : 0));
        internal static Instruction EQ(byte reg_a, byte reg_b, bool expected) => new Instruction(Opcode.EQ, reg_a, reg_b, (byte)(expected ? 1 : 0));
        internal static Instruction LT(byte reg_a, byte reg_b, bool expected) => new Instruction(Opcode.LT, reg_a, reg_b, (byte)(expected ? 1 : 0));
        internal static Instruction LE(byte reg_a, byte reg_b, bool expected) => new Instruction(Opcode.LE, reg_a, reg_b, (byte)(expected ? 1 : 0));
        internal static Instruction NOT(byte dest_reg, byte reg_source) => new Instruction(Opcode.NOT, dest_reg, reg_source, 0);

    }
}
