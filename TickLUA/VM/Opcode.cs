namespace TickLUA.VM
{
    internal enum Opcode
    {
        /// <summary>
        /// No operation
        /// </summary>
        NOP,
        /// <summary>
        /// Copy value from one register to another. 
        /// A - target reg, B - source reg
        /// </summary>
        MOVE,
        /// <summary>
        /// Load constant. 
        /// A - target reg, Bx - constant index
        /// </summary>
        LOAD_CONST,
        /// <summary>
        /// Load integer. 
        /// A - target reg, Bx - literal value
        /// </summary>
        LOAD_INT,
        /// <summary>
        /// Load boolean true. 
        /// A - target reg
        /// </summary>
        LOAD_TRUE,
        /// <summary>
        /// Load boolean false. 
        /// A - target reg
        /// </summary>
        LOAD_FALSE,
        /// <summary>
        /// Load boolean false and skip next instruction. 
        /// A - target reg
        /// </summary>
        LOAD_FALSE_SKIP,
        /// <summary>
        /// Load nil. 
        /// A - start reg, Bx - number of registers to set to nil
        /// </summary>
        LOAD_NIL,
        /// <summary>
        /// Create new table object.
        /// A - target reg
        /// </summary>
        NEW_TABLE,
        /// <summary>
        /// Set field of a table.
        /// A - table reg, B - key constant, C - value reg
        /// </summary>
        SET_FIELD,
        /// <summary>
        /// Get field of a table
        /// A - result reg, B - table reg, C - key constant
        /// </summary>
        GET_FIELD,
        /// <summary>
        /// Set value of an array.
        /// A - table reg, B - start register, C - number elements to set
        /// </summary>
        SET_LIST,
        /// <summary>
        /// Set key value pair of a table.
        /// A - table reg, B - key reg, C - value reg
        /// </summary>
        SET_TABLE,
        /// <summary>
        /// Get value from a table by key
        /// A - result reg, B - table reg, C - key reg
        /// </summary>
        GET_TABLE,


        //GETUPVAL,
        //GETGLOBAL,
        //SETTABUP,
        //SETUPVAL,
        //SETGLOBAL,
        //SELF,

        /// <summary>
        /// Add two values. 
        /// A - result reg, B - left val reg, C - right val reg
        /// </summary>
        ADD,
        /// <summary>
        /// Subtract two values. 
        /// A - result reg, B - left val reg, C - right val reg
        /// </summary>
        SUB,
        /// <summary>
        /// Multiply two values. 
        /// A - result reg, B - left val reg, C - right val reg
        /// </summary>
        MUL,
        /// <summary>
        /// Modulus of two values. 
        /// A - result reg, B - left val reg, C - right val reg
        /// </summary>
        MOD,
        /// <summary>
        /// Power of two values. 
        /// A - result reg, B - left val reg, C - right val reg
        /// </summary>
        POW,
        /// <summary>
        /// Divide two values. 
        /// A - result reg, B - left val reg, C - right val reg
        /// </summary>
        DIV,
        /// <summary>
        /// Integer divide two values. 
        /// A - result reg, B - left val reg, C - right val reg
        /// </summary>
        IDIV,
        /// <summary>
        /// Unary minus.
        /// A - result reg, B - source reg
        /// </summary>
        UNM,
        /// <summary>
        /// Logical NOT.
        /// A - result reg, B - source reg
        /// </summary>
        NOT,
        /// <summary>
        /// Length operation.
        /// A - result reg, B - source reg
        /// </summary>
        LEN,
        //CONCAT,

        /// <summary>
        /// Relative jump. 
        /// sBx - jump offset
        /// </summary>
        JMP,
        /// <summary>
        /// Equality test and skip next instruction if not expected result. 
        /// A - first register to compare, B - second register to compare, C - expected boolean value (0 - false, 1 - true)
        /// </summary>
        EQ,
        /// <summary>
        /// Less-than test and skip next instruction if not expected result. 
        /// A - first register to compare, B - second register to compare, C - expected boolean value (0 - false, 1 - true)
        /// </summary>
        LT,
        /// <summary>
        /// Less-than-or-equal test and skip next instruction if not expected result. 
        /// A - first register to compare, B - second register to compare, C - expected boolean value (0 - false, 1 - true)
        /// </summary>
        LE,

        /// <summary>
        /// Test condition and skip next instruction if not expected result. 
        /// A - register to test, B - expected boolean value (0 - false, 1 - true)
        /// </summary>
        TEST,
        /// <summary>
        /// Test condition in register B. 
        /// If not expected result, skip next instruction.
        /// Else, assign register B to register A.
        /// A - result register, B - register to test, C - expected boolean value (0 - false, 1 - true)
        /// </summary>
        TESTSET,

        //CALL,
        //TAILCALL,
        /// <summary>
        /// Return from a function. 
        /// A - start result reg, Bx - number of registers to return (0 - all, 1 - none, 2+ - (Bx-1))
        /// </summary>
        RETURN,

        //FORLOOP,
        //FORPREP,
        //TFORCALL,
        //TFORLOOP,
        //CLOSE,
        //CLOSURE,
        //VARARG
    }
}
