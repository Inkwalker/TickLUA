namespace TickLUA.VM
{
    internal partial struct Instruction
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
    }
}