using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersLogic
    {
        internal static void TEST(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg       = instruction.A;
            bool expected = instruction.B != 0;

            var obj = frame.Registers[reg].Value;

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

            var test_obj = frame.Registers[test_reg].Value;

            if ((bool)test_obj.ToBooleanObject() != expected)
            {
                frame.PC++;
            }
            else
            {
                frame.Registers[dest_reg].Value = test_obj;
            }
        }

        internal static void EQ(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_a     = instruction.A;
            int reg_b     = instruction.B;
            bool expected = instruction.C != 0;

            var obj_a = frame.Registers[reg_a].Value;
            var obj_b = frame.Registers[reg_b].Value;

            if (obj_a.Equals(obj_b))
            {
                if (!expected)
                    frame.PC++;
                return;
            }

            // __eq is consulted only when primitive equality fails and both
            // operands are tables; its result drives the skip via the sink.
            if (obj_a is TableObject && obj_b is TableObject)
            {
                var handler = Metamethods.GetHandler(obj_a, Metamethods.EqualsKey)
                           ?? Metamethods.GetHandler(obj_b, Metamethods.EqualsKey);
                if (handler != null)
                {
                    Metamethods.Call(vm, frame, handler, new LuaObject[] { obj_a, obj_b }, 0, 1,
                        sink: expected ? Metamethods.BranchExpectTrue : Metamethods.BranchExpectFalse);
                    return;
                }
            }

            if (expected)
                frame.PC++;
        }

        internal static void LT(TickVM vm, StackFrame frame, Instruction instruction)
        {
            Compare(vm, frame, instruction, Metamethods.LessKey, le: false);
        }

        internal static void LE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            Compare(vm, frame, instruction, Metamethods.LessEqKey, le: true);
        }

        /// <summary>
        /// Shared LT/LE ordering: numbers compare primitively, anything else
        /// dispatches to __lt/__le (left operand's handler first). The
        /// metamethod's boolean-coerced result drives the skip via the sink.
        /// </summary>
        private static void Compare(TickVM vm, StackFrame frame, Instruction instruction,
            StringObject event_key, bool le)
        {
            int reg_a     = instruction.A;
            int reg_b     = instruction.B;
            bool expected = instruction.C != 0;

            var obj_a = frame.Registers[reg_a].Value;
            var obj_b = frame.Registers[reg_b].Value;

            if (obj_a is NumberObject number_a && obj_b is NumberObject number_b)
            {
                bool result = le ? number_a <= number_b : number_a < number_b;
                if (result != expected)
                    frame.PC++;
                return;
            }

            if (obj_a is StringObject string_a && obj_b is StringObject string_b)
            {
                // Byte-order (locale-independent) comparison per the Lua manual §3.4.4.
                int cmp = string.CompareOrdinal(string_a.Value, string_b.Value);
                bool result = le ? cmp <= 0 : cmp < 0;
                if (result != expected)
                    frame.PC++;
                return;
            }

            var handler = Metamethods.GetHandler(obj_a, event_key)
                       ?? Metamethods.GetHandler(obj_b, event_key);
            if (handler == null)
                throw new RuntimeException(
                    $"attempt to compare {NativeArgs.TypeName(obj_a)} with {NativeArgs.TypeName(obj_b)}");

            Metamethods.Call(vm, frame, handler, new LuaObject[] { obj_a, obj_b }, 0, 1,
                sink: expected ? Metamethods.BranchExpectTrue : Metamethods.BranchExpectFalse);
        }

        internal static void NOT(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int a = instruction.A;
            int b = instruction.B;
            var obj = frame.Registers[b].Value;

            frame.Registers[a].Value = !obj.ToBooleanObject();
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
