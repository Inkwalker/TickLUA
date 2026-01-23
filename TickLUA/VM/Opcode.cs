namespace TickLUA.VM
{
    internal enum Opcode
    {
        /// <summary>
        /// No operation
        /// </summary>
        NOP,
        /// <summary>
        /// Copy value from one register to another. A - target reg, B - source reg
        /// </summary>
        MOVE,
        /// <summary>
        /// Load constant. A - target reg, Bx - constant index
        /// </summary>
        LOADK,
        /// <summary>
        /// Load integer. A - target reg, Bx - literal value
        /// </summary>
        LOADI,
        /// <summary>
        /// Load boolean. A - target reg, B - bool value (0 - false, 1 - true)
        /// </summary>
        LOADBOOL,
        /// <summary>
        /// Load nil. A - start reg, Bx - number of registers to set to nil
        /// </summary>
        LOADNIL,

        //GETUPVAL,
        //GETGLOBAL,
        //GETTABLE,
        //SETTABUP,
        //SETUPVAL,
        //SETGLOBAL,
        //SETTABLE,
        //NEWTABLE,
        //SELF,

        /// <summary>
        /// Add two values. A - result reg, B - left val reg, C - right val reg
        /// </summary>
        ADD,
        /// <summary>
        /// Subtract two values. A - result reg, B - left val reg, C - right val reg
        /// </summary>
        SUB,
        /// <summary>
        /// Multiply two values. A - result reg, B - left val reg, C - right val reg
        /// </summary>
        MUL,
        /// <summary>
        /// Modulus of two values. A - result reg, B - left val reg, C - right val reg
        /// </summary>
        MOD,
        /// <summary>
        /// Power of two values. A - result reg, B - left val reg, C - right val reg
        /// </summary>
        POW,
        /// <summary>
        /// Divide two values. A - result reg, B - left val reg, C - right val reg
        /// </summary>
        DIV,
        /// <summary>
        /// Integer divide two values. A - result reg, B - left val reg, C - right val reg
        /// </summary>
        IDIV,
        //UNM,
        //NOT,
        //LEN,
        //CONCAT,

        /// <summary>
        /// Relative jump. sBx - jump offset
        /// </summary>
        JMP,
        //EQ,
        //LT,
        //LE,
        //TEST,
        //TESTSET,
        //CALL,
        //TAILCALL,
        /// <summary>
        /// Return from a function. A - start result reg, Bx - number of registers to return (0 - all, 1 - none, 2+ - (Bx-1))
        /// </summary>
        RETURN,
        //FORLOOP,
        //FORPREP,
        //TFORCALL,
        //TFORLOOP,
        //SETLIST,
        //CLOSE,
        //CLOSURE,
        //VARARG
    }
}
