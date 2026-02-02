using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersTable
    {
        internal static void NEW_TABLE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;

            frame.Registers[a] = new TableObject();
        }

        internal static void SET_TABLE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A; // table
            byte b = instruction.B; // key
            byte c = instruction.C; // value

            var table = frame.Registers[a] as TableObject;
            if (table != null)
            {
                var key = frame.Registers[b];
                var val = frame.Registers[c];

                table[key] = val;
            }
            else
                throw new RuntimeException("Not a table");
        }

        internal static void GET_TABLE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A; // result
            byte b = instruction.B; // table
            byte c = instruction.C; // key

            var table = frame.Registers[b] as TableObject;
            if (table != null)
            {
                var key = frame.Registers[c];

                frame.Registers[a] = table[key];
            }
            else
                throw new RuntimeException("Not a table");
        }

        internal static void SET_FIELD(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A; // table
            byte b = instruction.B; // key const
            byte c = instruction.C; // value

            var table = frame.Registers[a] as TableObject;
            if (table != null)
            {
                var key = frame.Constants[b];
                var val = frame.Registers[c];

                table[key] = val;
            }
            else
                throw new RuntimeException("Not a table");
        }

        internal static void GET_FIELD(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A; // result
            byte b = instruction.B; // table
            byte c = instruction.C; // key const

            var table = frame.Registers[b] as TableObject;
            if (table != null)
            {
                var key = frame.Constants[c];

                frame.Registers[a] = table[key];
            }
            else
                throw new RuntimeException("Not a table");
        }

        internal static void SET_LIST(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;
            byte b = instruction.B;
            byte c = instruction.C;

            var table = frame.Registers[a] as TableObject;
            if (table != null)
            {
                int len = (int)table.Len();

                for (int i = 0; i < c; i++)
                {
                    var key = i + len + 1;
                    var val = frame.Registers[b + i];

                    table[key] = val;
                }
            }
            else
                throw new RuntimeException("Not a table");
        }
    }
}

namespace TickLUA.VM
{
    internal partial struct Instruction
    {
        internal static Instruction NEW_TABLE(byte reg) => new Instruction(Opcode.NEW_TABLE, reg, 0, 0);
        internal static Instruction SET_TABLE(byte reg_table, byte reg_key, byte reg_value) => new Instruction(Opcode.SET_TABLE, reg_table, reg_key, reg_value);
        internal static Instruction GET_TABLE(byte reg_result, byte reg_table, byte reg_key) => new Instruction(Opcode.GET_TABLE, reg_result, reg_table, reg_key);
        internal static Instruction SET_FIELD(byte reg_table, byte const_key, byte reg_value) => new Instruction(Opcode.SET_FIELD, reg_table, const_key, reg_value);
        internal static Instruction GET_FIELD(byte reg_result, byte reg_table, byte const_key) => new Instruction(Opcode.GET_FIELD, reg_result, reg_table, const_key);
        internal static Instruction SET_LIST(byte reg_table, byte start_reg, byte num_reg) => new Instruction(Opcode.SET_LIST, reg_table, start_reg, num_reg);
    }
}
