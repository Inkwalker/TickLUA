using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersTable
    {
        internal static void NEW_TABLE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;

            frame.Registers[a].Value = new TableObject();
        }

        internal static void SET_TABLE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A; // table
            byte b = instruction.B; // key
            byte c = instruction.C; // value

            Metamethods.NewIndex(vm, frame,
                frame.Registers[a].Value, frame.Registers[b].Value, frame.Registers[c].Value);
        }

        internal static void GET_TABLE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A; // result
            byte b = instruction.B; // table
            byte c = instruction.C; // key

            Metamethods.Index(vm, frame,
                frame.Registers[b].Value, frame.Registers[c].Value, a);
        }

        internal static void SET_FIELD(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A; // table
            byte b = instruction.B; // key const
            byte c = instruction.C; // value

            Metamethods.NewIndex(vm, frame,
                frame.Registers[a].Value, frame.Constants[b], frame.Registers[c].Value);
        }

        internal static void GET_FIELD(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A; // result
            byte b = instruction.B; // table
            byte c = instruction.C; // key const

            Metamethods.Index(vm, frame,
                frame.Registers[b].Value, frame.Constants[c], a);
        }

        internal static void SET_LIST(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;
            byte b = instruction.B;
            int  c = instruction.C - 1;

            // c < 0 signals a variable-length list: take everything from b up to the
            // stack top left by a preceding trailing multi return call.
            if (c < 0)
                c = System.Math.Max(0, frame.Top - b);

            var table = frame.Registers[a].Value as TableObject;
            if (table != null)
            {
                int len = (int)table.Len();

                for (int i = 0; i < c; i++)
                {
                    var key = i + len + 1;
                    var val = frame.Registers[b + i].Value;

                    table[key] = val;
                }
            }
            else
                throw new RuntimeException("Not a table");
        }

        internal static void LEN(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A; // result
            byte b = instruction.B; // value

            var obj = frame.Registers[b].Value;

            if (obj is StringObject str)
            {
                frame.Registers[a].Value = str.Len();
                return;
            }

            if (obj is TableObject table)
            {
                // __len takes priority over the raw border for tables.
                var handler = Metamethods.GetHandler(table, Metamethods.LenKey);
                if (handler != null)
                {
                    Metamethods.Call(vm, frame, handler, new LuaObject[] { table }, a, 1);
                    return;
                }

                frame.Registers[a].Value = table.Len();
                return;
            }

            throw new RuntimeException(
                $"attempt to get length of a {NativeArgs.TypeName(obj)} value");
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
        internal static Instruction SET_LIST(byte reg_table, byte start_reg, int num_reg)
        {
            byte c = num_reg < -1 ? (byte)0 : (byte)(num_reg + 1);
            return new Instruction(Opcode.SET_LIST, reg_table, start_reg, c);
        }
        internal static Instruction LEN(byte reg_result, byte reg_source) => new Instruction(Opcode.LEN, reg_result, reg_source, 0);
    }
}
