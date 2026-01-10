namespace TickLUA.VM
{
    internal enum Opcode
    {
        /// <summary>
        /// No operation
        /// </summary>
        NOP,
        /// <summary>
        /// Load constant. A - target reg, Bx - constant index
        /// </summary>
        LOADK,
        /// <summary>
        /// Load integer. A - target reg, Bx - literal value
        /// </summary>
        LOADI,
        //LOADBOOL,
        //LOADNIL,
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
        //SUB,
        //MUL,
        //DIV,
        //MOD,
        //POW,
        //UNM,
        //NOT,
        //LEN,
        //CONCAT,
        //JMP,
        //EQ,
        //LT,
        //LE,
        //TEST,
        //TESTSET,
        //CALL,
        //TAILCALL,
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
