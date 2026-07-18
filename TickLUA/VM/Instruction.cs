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

        /// <summary>
        /// The raw 32-bit encoding of the instruction, used by bytecode serialization.
        /// </summary>
        public uint Raw => raw;

        /// <summary>
        /// Reconstructs an instruction from its raw 32-bit encoding
        /// (see <see cref="Raw"/>), used by bytecode deserialization.
        /// </summary>
        public static Instruction FromRaw(uint raw)
        {
            var instruction = default(Instruction);
            instruction.raw = raw;
            return instruction;
        }

        public override string ToString()
        {
            return $"{Opcode} {A} {B} {C}";
        }
    }
}