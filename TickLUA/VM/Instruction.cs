namespace TickLUA.VM
{
    // instruction layout:
    // +--------+--------+--------+--------+
    // |   C    |   B    |   A    | OPCODE |
    // |        Bx       |   A    | OPCODE |
    // +--------+--------+--------+--------+
    // 32       24       16       8        0

    internal static class Instruction
    {
        internal static Opcode GetOpcode(uint instruction)
        {
            return (Opcode)(instruction & 0xFF);
        }

        internal static uint GetOpcodeI(uint instruction)
        {
            return instruction & 0xFF;
        }

        internal static byte GetA(uint instruction)
        {
            return (byte)((instruction >> 8) & 0xFF);
        }

        internal static byte GetB(uint instruction)
        {
            return (byte)((instruction >> 16) & 0xFF);
        }

        internal static byte GetC(uint instruction)
        {
            return (byte)((instruction >> 24) & 0xFF);
        }

        internal static ushort GetBx(uint instruction)
        {
            return (ushort)((instruction >> 16) & 0xFFFF);
        }

        internal static short GetBxSigned(uint instruction)
        {
            return (short)((instruction >> 16) & 0xFFFF);
        }

        internal static uint New(Opcode opcode, byte a, byte b, byte c)
        {
            return (uint)((byte)opcode | (a << 8) | (b << 16) | (c << 24));
        }

        internal static uint New(Opcode opcode, byte a, short sbx)
        {
            return (uint)((byte)opcode | (a << 8) | (sbx << 16));
        }

        internal static uint New(Opcode opcode, byte a, ushort bx)
        {
            return (uint)((byte)opcode | (a << 8) | (bx << 16));
        }

        #region Factory methods

        internal static uint NOP() => New(Opcode.NOP, 0, 0);
        internal static uint MOVE(byte dest_reg, byte src_reg) => New(Opcode.MOVE, dest_reg, src_reg, 0);
        internal static uint LOADK(byte dest_reg, ushort const_index) => New(Opcode.LOADK, dest_reg, (short)const_index);
        internal static uint LOADI(byte dest_reg, short integer) => New(Opcode.LOADI, dest_reg, integer);
        internal static uint LOADBOOL(byte dest_reg, bool value) => New(Opcode.LOADBOOL, dest_reg, (byte)(value ? 1 : 0), 0);
        internal static uint LOADNIL(byte start_reg, byte count = 1) => New(Opcode.LOADNIL, start_reg, count, 0);
        internal static uint ADD(byte dest_reg, byte left_reg, byte right_reg) => New(Opcode.ADD, dest_reg, left_reg, right_reg);
        internal static uint SUB(byte dest_reg, byte left_reg, byte right_reg) => New(Opcode.SUB, dest_reg, left_reg, right_reg);
        internal static uint MUL(byte dest_reg, byte left_reg, byte right_reg) => New(Opcode.MUL, dest_reg, left_reg, right_reg);
        internal static uint MOD(byte dest_reg, byte left_reg, byte right_reg) => New(Opcode.MOD, dest_reg, left_reg, right_reg);
        internal static uint POW(byte dest_reg, byte left_reg, byte right_reg) => New(Opcode.POW, dest_reg, left_reg, right_reg);
        internal static uint DIV(byte dest_reg, byte left_reg, byte right_reg) => New(Opcode.DIV, dest_reg, left_reg, right_reg);
        internal static uint IDIV(byte dest_reg, byte left_reg, byte right_reg) => New(Opcode.IDIV, dest_reg, left_reg, right_reg);
        internal static uint RETURN(byte start_reg, int count)
        {
            int c = count < -1 ? -1 : count;
            return New(Opcode.RETURN, start_reg, (ushort)(c + 1));
        }

        #endregion
    }
}