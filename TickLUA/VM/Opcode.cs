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

        /// <summary>
        /// Copy value from an upvalue to a register.
        /// A - target reg, B - source upvalue
        /// </summary>
        GET_UPVAL,
        /// <summary>
        /// Copy value from a register to an upvalue.
        /// A - source reg, B - target upvalue
        /// </summary>
        SET_UPVAL,

        //GETGLOBAL,
        //SETTABUP,
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
        /// <summary>
        /// String concatenation operation.
        /// A - result reg, B -left val reg, C - right val reg
        /// </summary>
        CONCAT,

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

        /// <summary>
        /// Call a function.
        /// A - function closure reg, 
        /// B - number of args (0 - all, 1 - none, 2+ - (B-1), 
        /// C - number of results (0 - all, 1 - none, 2+ - (C-1)
        /// Will read arguments from A + 1, A + 2, ... and write results to A, A + 1, ...
        /// </summary>
        CALL,
        /// <summary>
        /// Tail call: the caller replaces the current frame instead of stacking on it.
        /// A - function closure reg,
        /// B - number of args (0 - all up to Top, 1 - none, 2+ - (B-1))
        /// No result count: results are delivered through the replaced frame's sink.
        /// Always followed by a RETURN(A, all) that finishes the call for callers
        /// that cannot replace the frame (VM-aware natives, __call metamethods).
        /// </summary>
        TAILCALL,
        /// <summary>
        /// Return from a function. 
        /// A - start result reg, Bx - number of registers to return (0 - all, 1 - none, 2+ - (Bx-1))
        /// </summary>
        RETURN,

        /// <summary>
        /// Numeric for loop condition test and increment.
        /// A - init value reg, A + 1 - limit reg, A + 2 - step reg, A + 3 - external state reg, sBx - relative jump
        /// </summary>
        FORLOOP,

        /// <summary>
        /// Numeric for loop preparation.
        /// A - init value reg, A + 1 - limit reg, A + 2 - step reg, A + 3 - external state reg, sBx - relative jump to test instruction
        /// </summary>
        FORPREP,

        /// <summary>
        /// Generic for loop iterator call.
        /// Calls R(A) with arguments R(A+1) (state) and R(A+2) (control), storing B results at R(A+3) onwards.
        /// A - base reg, B - number of loop variables
        /// </summary>
        TFORCALL,

        /// <summary>
        /// Generic for loop test.
        /// If R(A+3) is not nil, copies it into R(A+2) (control) and jumps back into the loop body.
        /// A - base reg, sBx - relative jump
        /// </summary>
        TFORLOOP,

        /// <summary>
        /// Close upvalues.
        /// Every register from A upwards is replaced with a fresh cell holding the same value.
        /// A - start reg
        /// </summary>
        CLOSE,

        /// <summary>
        /// Create closure object form <see cref="LuaFunction"/>
        /// A - result reg, Bx - function index
        /// </summary>
        CLOSURE,

        /// <summary>
        /// Load the varargs of the current function into registers.
        /// Fixed counts are nil-padded; expanding all sets the frame top.
        /// A - start result reg, Bx - number of values (0 - all, 1 - none, 2+ - (Bx-1)).
        /// </summary>
        VARARG
    }
}
