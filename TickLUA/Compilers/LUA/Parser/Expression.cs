using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;

namespace TickLUA.Compilers.LUA.Parser
{
    /// <summary>
    /// Base class for all expressions.
    /// By default, all expressions are RValues (read operations).
    /// </summary>
    internal abstract class Expression : AstNode
    {
        public int ResultRegister { get; protected set; } = -1;

        /// <summary>
        /// Release all temporary registers used by this expression.
        /// </summary>
        public virtual void ReleaseRegisters(FunctionBuilder builder)
        {
            if (ResultRegister != -1)
            {
                builder.DeallocateRegisters((byte)ResultRegister, 1);
                ResultRegister = -1;
            }
        }

        /// <summary>
        /// Compile expression
        /// </summary>
        /// <returns>Index of result register. -1 if no result</returns>
        public abstract byte CompileRead(FunctionBuilder builder);

        public static Expression Create(LuaLexer lexer)
        {
            return SubExpr(lexer, true);
        }

        internal static Expression SubExpr(LuaLexer lexer, bool isPrimary)
        {
            Expression e = null;

            Token T = lexer.Current;

            if (T.IsUnaryOperator())
            {
                lexer.Next();
                e = SubExpr(lexer, false);

                // check for power operator
                Token unaryOp = T;
                T = lexer.Current;

                if (isPrimary && T.Type == TokenType.OP_Pow)
                {
                    List<Expression> powerChain = new List<Expression>();
                    powerChain.Add(e);

                    while (isPrimary && T.Type == TokenType.OP_Pow)
                    {
                        lexer.Next();
                        powerChain.Add(SubExpr(lexer, false));
                        T = lexer.Current;
                    }

                    e = powerChain[powerChain.Count - 1];

                    for (int i = powerChain.Count - 2; i >= 0; i--)
                    {
                        e = new BinaryExpression(powerChain[i], e, BinaryOperation.Pow);
                    }
                }

                e = new UnaryExpression(e, unaryOp, lexer);
            }
            else
            {
                e = SimpleExp(lexer);
            }

            T = lexer.Current;

            if (isPrimary && T.IsBinaryOperator())
            {
                object chain = BinaryExpression.BeginOperatorChain();

                BinaryExpression.AddExpressionToChain(chain, e);

                while (T.IsBinaryOperator())
                {
                    BinaryExpression.AddOperatorToChain(chain, T);
                    lexer.Next();
                    Expression right = SubExpr(lexer, false);
                    BinaryExpression.AddExpressionToChain(chain, right);
                    T = lexer.Current;
                }

                e = BinaryExpression.CommitOperatorChain(chain);
            }

            return e;
        }

        internal static Expression SimpleExp(LuaLexer lexer)
        {
            Token t = lexer.Current;

            switch (t.Type)
            {
                case TokenType.Number:
                case TokenType.String:
                case TokenType.Nil:
                case TokenType.True:
                case TokenType.False:
                    return new LiteralExpression(lexer);
                //case TokenType.VarArgs:
                //    return new SymbolRefExpression(t, lcontext);
                //case TokenType.BRK_CUR_Left:
                //    return new TableDefinitionExpression(lexer);
                //case TokenType.Function:
                //    return new FunctionDefinitionExpression(lexer);
                default:
                    return PrimaryExp(lexer);
            }
        }

        internal static Expression PrimaryExp(LuaLexer lexer)
        {
            Expression e = PrefixExp(lexer);

            while (true)
            {
                Token T = lexer.Current;

                switch (T.Type)
                {
                    //case TokenType.Dot:
                    //    {
                    //        lexer.Next();
                    //        AssertToken(lexer.Current, TokenType.Name);
                    //        e = new IndexExpression(e, lexer.Current.Content);
                    //        lexer.Next();
                    //    }
                    //    break;
                    //case TokenType.BRK_SQR_Left:
                    //    {
                    //        lexer.Next();
                    //        Expression index = Create(lexer);
                    //        AssertTokenNext(lexer, TokenType.BRK_SQR_Right);
                    //        e = new IndexExpression(e, index);
                    //    }
                    //    break;
                    //case TokenType.Colon:
                    //    {
                    //        lexer.Next();
                    //        AssertToken(lexer.Current, TokenType.Name);
                    //        var method_name = lexer.Current.Content;
                    //        lexer.Next();
                    //        e = new MethodCallExpression(e, method_name, lexer);
                    //    }
                    //    break;
                    //case TokenType.BRK_ROUND_Left:
                    //    e = new FunctionCallExpression(e, lexer);
                    //    break;
                    default:
                        return e;
                }
            }
        }

        private static Expression PrefixExp(LuaLexer lexer)
        {
            Token T = lexer.Current;
            switch (T.Type)
            {
                case TokenType.BRK_ROUND_Left:
                    lexer.Next();
                    Expression e = Create(lexer);
                    e = new AdjustmentExpression(e);
                    if (lexer.Current.Type != TokenType.BRK_ROUND_Right)
                        throw new CompilationException("Unclosed '('", lexer.Current.Position);
                    lexer.Next();
                    return e;
                case TokenType.Name:
                    return new SymbolExpression(lexer);
                default:
                    throw new CompilationException($"Unexpected symbol near '{T.Content}'", T.Position);
            }
        }
    }
}
