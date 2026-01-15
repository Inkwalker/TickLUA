namespace TickLUA.Compilers.LUA.Parser
{
    internal enum BinaryOperation
    {
        Invalid,

        Add,
        Sub,
        Div,
        iDiv,
        Mul,
        Mod,
        Pow,

        Concat,

        Less,
        LessEq,
        Greater,
        GreaterEq,
        Equals,
        NotEquals,

        LogicAnd,
        LogicOr
    }
}
