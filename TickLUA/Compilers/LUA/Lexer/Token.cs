namespace TickLUA.Compilers.LUA.Lexer
{
    internal class Token
    {
        public TokenType Type { get; private set; }
        public string Content { get; set; }

        public int Line { get; set; }
        public int Colon { get; set; }

        public Token(TokenType type)
        {
            Type = type;
            Line = 1;
            Colon = 1;
        }

        public Token(TokenType type, int line, int col)
        {
            Type = type;
            Line = line;
            Colon = col;
        }

        public override string ToString()
        {
            return $"Token: {Type} | {Content} [ln:{Line},ch:{Colon}]";
        }

        public bool IsEndOfBlock()
        {
            switch (Type)
            {
                case TokenType.Else:
                case TokenType.Elseif:
                case TokenType.End:
                case TokenType.Until:
                case TokenType.EOF:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsUnaryOperator()
        {
            return Type == TokenType.OP_Sub || Type == TokenType.Not || Type == TokenType.OP_Len;
        }

        public bool IsBinaryOperator()
        {
            switch (Type)
            {
                case TokenType.OP_LogicAnd:
                case TokenType.OP_LogicOr:
                case TokenType.OP_Equals:
                case TokenType.OP_NotEquals:
                case TokenType.OP_LessThan:
                case TokenType.OP_LessEq:
                case TokenType.OP_GreaterThan:
                case TokenType.OP_GreaterEq:
                case TokenType.OP_Concat:
                case TokenType.OP_Pow:
                case TokenType.OP_Mod:
                case TokenType.OP_Div:
                case TokenType.OP_iDiv:
                case TokenType.OP_Mul:
                case TokenType.OP_Sub:
                case TokenType.OP_Add:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsAssignmentOperator()
        {
            switch (Type)
            {
                case TokenType.OP_Assignment:
                case TokenType.OP_Assignment_Add:
                case TokenType.OP_Assignment_Div:
                case TokenType.OP_Assignment_iDiv:
                case TokenType.OP_Assignment_Mod:
                case TokenType.OP_Assignment_Mul:
                case TokenType.OP_Assignment_Sub:
                case TokenType.OP_Assignment_Pow:
                    return true;
                default:
                    return false;
            }
        }
    }
}
