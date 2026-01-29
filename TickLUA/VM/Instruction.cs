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

        internal static uint GetAx(uint instruction)
        {
            return instruction >> 8;
        }

        internal static int GetAxSigned(uint instruction)
        {
            uint ax = instruction >> 8;
            if ((ax & 0x800000) != 0)
            {
                // negative number
                return (int)(ax | 0xFF000000);
            }
            else
            {
                return (int)ax;
            }
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

        /// <summary>
        /// Creates a new instruction with three 8-bit unsigned arguments.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <param name="a">The first 8-bit value to encode.</param>
        /// <param name="b">The second 8-bit value to encode.</param>
        /// <param name="c">The third 8-bit value to encode.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        internal static uint New(Opcode opcode, byte a, byte b, byte c)
        {
            return (uint)((byte)opcode | (a << 8) | (b << 16) | (c << 24));
        }

        /// <summary>
        /// Creates a new instruction with a 8-bit unsigned and a 16-bit signed arguments.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <param name="a">The 8-bit value to encode.</param>
        /// <param name="sbx">The 16-bit signed value to encode.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        internal static uint New(Opcode opcode, byte a, short sbx)
        {
            return (uint)((byte)opcode | (a << 8) | (sbx << 16));
        }

        /// <summary>
        /// Creates a new instruction with a 8-bit unsigned and a 16-bit unsigned arguments.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <param name="a">The 8-bit value to encode.</param>
        /// <param name="sbx">The 16-bit unsigned value to encode.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        internal static uint New(Opcode opcode, byte a, ushort bx)
        {
            return (uint)((byte)opcode | (a << 8) | (bx << 16));
        }

        /// <summary>
        /// Creates a new instruction with a 24-bit unsigned argument.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <param name="ax">The 24-bit unsigned argument to encode in the instruction in range from 0 to 16_777_215.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        /// <remarks>
        /// Argument range is not checked. Make sure the provided value fits in 24 bits.
        /// </remarks>
        internal static uint New(Opcode opcode, uint ax)
        {
            return (byte)opcode | (ax << 8);
        }

        /// <summary>
        /// Creates a new instruction with a 24-bit signed argument.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <param name="sax">The 24-bit signed argument to encode in the instruction in range from −8_388_608 to 8_388_607.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        /// <remarks>
        /// Argument range is not checked. Make sure the provided value fits in 24 bits.
        /// </remarks>
        internal static uint New(Opcode opcode, int sax)
        {
            return (byte)opcode | (uint)(sax << 8);
        }

        /// <summary>
        /// Creates a new instruction with only an opcode and no arguments.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        internal static uint New(Opcode opcode)
        {
            return (byte)opcode;
        }

        #region Factory methods

        internal static uint NOP() => New(Opcode.NOP);
        internal static uint MOVE(byte dest_reg, byte src_reg) => New(Opcode.MOVE, dest_reg, src_reg, 0);
        internal static uint LOAD_CONST(byte dest_reg, ushort const_index) => New(Opcode.LOAD_CONST, dest_reg, (short)const_index);
        internal static uint LOAD_INT(byte dest_reg, short integer) => New(Opcode.LOAD_INT, dest_reg, integer);
        internal static uint LOAD_TRUE(byte dest_reg) => New(Opcode.LOAD_TRUE, dest_reg, 0, 0);
        internal static uint LOAD_FALSE(byte dest_reg) => New(Opcode.LOAD_FALSE, dest_reg, 0, 0);
        internal static uint LOAD_FALSE_SKIP(byte dest_reg) => New(Opcode.LOAD_FALSE_SKIP, dest_reg, 0, 0);
        internal static uint LOAD_BOOL(byte dest_reg, bool value) => value ? LOAD_TRUE(dest_reg) : LOAD_FALSE(dest_reg);
        internal static uint LOAD_NIL(byte start_reg, byte count = 1) => New(Opcode.LOAD_NIL, start_reg, count, 0);
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
        internal static uint JMP(int offset) => New(Opcode.JMP, offset);
        internal static uint TEST(byte reg, bool expected) => New(Opcode.TEST, reg, (byte)(expected ? 1 : 0));
        internal static uint TESTSET(byte dest_reg, byte test_reg, bool expected) => New(Opcode.TESTSET, dest_reg, test_reg, (byte)(expected ? 1 : 0));
        internal static uint EQ(byte reg_a, byte reg_b, bool expected) => New(Opcode.EQ, reg_a, reg_b, (byte)(expected ? 1 : 0));
        internal static uint LT(byte reg_a, byte reg_b, bool expected) => New(Opcode.LT, reg_a, reg_b, (byte)(expected ? 1 : 0));
        internal static uint LE(byte reg_a, byte reg_b, bool expected) => New(Opcode.LE, reg_a, reg_b, (byte)(expected ? 1 : 0));
        internal static uint NOT(byte dest_reg, byte reg_source) => New(Opcode.NOT, dest_reg, reg_source, 0);
        internal static uint UNM(byte dest_reg, byte reg_source) => New(Opcode.UNM, dest_reg, reg_source, 0);

        #endregion
    }
}