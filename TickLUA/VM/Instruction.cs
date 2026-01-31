namespace TickLUA.VM
{
    internal struct Instruction
    {
        // instruction layout:
        // +--------+--------+--------+--------+
        // |   C    |   B    |   A    | OPCODE |
        // |        Bx       |   A    | OPCODE |
        // +--------+--------+--------+--------+
        // 32       24       16       8        0
        private uint raw;


        public Opcode Opcode => (Opcode)(raw & 0xFF);

        public byte A => (byte)((raw >> 8) & 0xFF);

        public uint Ax => raw >> 8;

        public int AxSigned
        {
            get
            {
                uint ax = raw >> 8;
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
        }
        public byte B => (byte)((raw >> 16) & 0xFF);

        public byte C => (byte)((raw >> 24) & 0xFF);

        public ushort Bx => (ushort)((raw >> 16) & 0xFFFF);

        public short BxSigned => (short)((raw >> 16) & 0xFFFF);

        /// <summary>
        /// Creates a new instruction with three 8-bit unsigned arguments.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <param name="a">The first 8-bit value to encode.</param>
        /// <param name="b">The second 8-bit value to encode.</param>
        /// <param name="c">The third 8-bit value to encode.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        public Instruction(Opcode opcode, byte a, byte b, byte c)
        {
            raw = (uint)((byte)opcode | (a << 8) | (b << 16) | (c << 24));
        }

        /// <summary>
        /// Creates a new instruction with a 8-bit unsigned and a 16-bit signed arguments.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <param name="a">The 8-bit value to encode.</param>
        /// <param name="sbx">The 16-bit signed value to encode.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        public Instruction(Opcode opcode, byte a, short sbx)
        {
            raw = (uint)((byte)opcode | (a << 8) | (sbx << 16));
        }

        /// <summary>
        /// Creates a new instruction with a 8-bit unsigned and a 16-bit unsigned arguments.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <param name="a">The 8-bit value to encode.</param>
        /// <param name="sbx">The 16-bit unsigned value to encode.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        public Instruction(Opcode opcode, byte a, ushort bx)
        {
            raw = (uint)((byte)opcode | (a << 8) | (bx << 16));
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
        public Instruction(Opcode opcode, uint ax)
        {
            raw = (byte)opcode | (ax << 8);
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
        public Instruction(Opcode opcode, int sax)
        {
            raw = (byte)opcode | (uint)(sax << 8);
        }

        /// <summary>
        /// Creates a new instruction with only an opcode and no arguments.
        /// </summary>
        /// <param name="opcode">The opcode of the instruction.</param>
        /// <returns>A 32-bit unsigned integer representing the instruction.</returns>
        public Instruction(Opcode opcode)
        {
            raw = (byte)opcode;
        }

        public override string ToString()
        {
            var opcode = Opcode;

            switch (opcode)
            {
                case Opcode.NOP:
                    return opcode.ToString();
                case Opcode.LOAD_TRUE:
                case Opcode.LOAD_FALSE:
                case Opcode.LOAD_FALSE_SKIP:
                    return $"{opcode} {A}";
                case Opcode.MOVE:
                case Opcode.LOAD_NIL:
                case Opcode.NOT:
                case Opcode.UNM:
                case Opcode.TEST:
                    return $"{opcode} {A} {B}";
                case Opcode.LOAD_CONST:
                case Opcode.RETURN:
                    return $"{opcode} {A} {Bx}";
                case Opcode.LOAD_INT:
                    return $"{opcode} {A} {BxSigned}";
                case Opcode.ADD:
                case Opcode.SUB:
                case Opcode.MUL:
                case Opcode.DIV:
                case Opcode.MOD:
                case Opcode.POW:
                case Opcode.IDIV:
                case Opcode.TESTSET:
                case Opcode.LE:
                case Opcode.LT:
                case Opcode.EQ:
                    return $"{opcode} {A} {B} {C}";
                case Opcode.JMP:
                    return $"{opcode} {AxSigned}";
                default:
                    return $"{opcode} {A} {B} {C}";
            }
        }

        #region Factory methods

        internal static Instruction NOP() => new Instruction(Opcode.NOP);
        internal static Instruction MOVE(byte dest_reg, byte src_reg) => new Instruction(Opcode.MOVE, dest_reg, src_reg, 0);
        internal static Instruction LOAD_CONST(byte dest_reg, ushort const_index) => new Instruction(Opcode.LOAD_CONST, dest_reg, (short)const_index);
        internal static Instruction LOAD_INT(byte dest_reg, short integer) => new Instruction(Opcode.LOAD_INT, dest_reg, integer);
        internal static Instruction LOAD_TRUE(byte dest_reg) => new Instruction(Opcode.LOAD_TRUE, dest_reg, 0, 0);
        internal static Instruction LOAD_FALSE(byte dest_reg) => new Instruction(Opcode.LOAD_FALSE, dest_reg, 0, 0);
        internal static Instruction LOAD_FALSE_SKIP(byte dest_reg) => new Instruction(Opcode.LOAD_FALSE_SKIP, dest_reg, 0, 0);
        internal static Instruction LOAD_BOOL(byte dest_reg, bool value) => value ? LOAD_TRUE(dest_reg) : LOAD_FALSE(dest_reg);
        internal static Instruction LOAD_NIL(byte start_reg, byte count = 1) => new Instruction(Opcode.LOAD_NIL, start_reg, count, 0);
        internal static Instruction ADD(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.ADD, dest_reg, left_reg, right_reg);
        internal static Instruction SUB(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.SUB, dest_reg, left_reg, right_reg);
        internal static Instruction MUL(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.MUL, dest_reg, left_reg, right_reg);
        internal static Instruction MOD(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.MOD, dest_reg, left_reg, right_reg);
        internal static Instruction POW(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.POW, dest_reg, left_reg, right_reg);
        internal static Instruction DIV(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.DIV, dest_reg, left_reg, right_reg);
        internal static Instruction IDIV(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.IDIV, dest_reg, left_reg, right_reg);
        internal static Instruction RETURN(byte start_reg, int count)
        {
            int c = count < -1 ? -1 : count;
            return new Instruction(Opcode.RETURN, start_reg, (ushort)(c + 1));
        }
        internal static Instruction JMP(int offset) => new Instruction(Opcode.JMP, offset);
        internal static Instruction TEST(byte reg, bool expected) => new Instruction(Opcode.TEST, reg, (byte)(expected ? 1 : 0));
        internal static Instruction TESTSET(byte dest_reg, byte test_reg, bool expected) => new Instruction(Opcode.TESTSET, dest_reg, test_reg, (byte)(expected ? 1 : 0));
        internal static Instruction EQ(byte reg_a, byte reg_b, bool expected) => new Instruction(Opcode.EQ, reg_a, reg_b, (byte)(expected ? 1 : 0));
        internal static Instruction LT(byte reg_a, byte reg_b, bool expected) => new Instruction(Opcode.LT, reg_a, reg_b, (byte)(expected ? 1 : 0));
        internal static Instruction LE(byte reg_a, byte reg_b, bool expected) => new Instruction(Opcode.LE, reg_a, reg_b, (byte)(expected ? 1 : 0));
        internal static Instruction NOT(byte dest_reg, byte reg_source) => new Instruction(Opcode.NOT, dest_reg, reg_source, 0);
        internal static Instruction UNM(byte dest_reg, byte reg_source) => new Instruction(Opcode.UNM, dest_reg, reg_source, 0);

        #endregion
    }
}