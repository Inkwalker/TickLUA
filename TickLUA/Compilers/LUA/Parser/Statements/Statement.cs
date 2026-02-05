using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal abstract class Statement : AstNode
    {
        public abstract void Compile(FunctionBuilder builder);

        public static Statement Create(LuaLexer lexer)
        {
            Token tkn = lexer.Current;

            switch (tkn.Type)
            {
                //case TokenType.DoubleColon:
                //    return new LabelStatement(lcontext);
                //case TokenType.Goto:
                //    return new GotoStatement(lcontext);
                //case TokenType.Class:
                //    return new ClassDefinitionStatement(false, lexer);
                //case TokenType.Semicolon:
                //    return new EmptyStatement();
                case TokenType.If:
                    return new IfStatement(lexer);
                case TokenType.While:
                    return new WhileLoopStatement(lexer);
                //case TokenType.Do:
                //    return new ScopeStatement(lexer);
                //case TokenType.For:
                //    return ForLoopStatement.Create(lexer);
                //case TokenType.Repeat:
                //    return new RepeatLoopStatement(lexer);
                //case TokenType.Function:
                //    return new FunctionDefinitionStatement(false, lexer);
                case TokenType.Local:
                    lexer.Next();
                    switch (lexer.Current.Type)
                    {
                        //case TokenType.Function: return new FunctionDefinitionStatement(true, lexer);
                        //case TokenType.Class: return new ClassDefinitionStatement(true, lexer);
                        default: return new AssignmentStatement(true, lexer);
                    }
                case TokenType.Return:
                    return new ReturnStatement(lexer);
                //case TokenType.Break:
                //    return new BreakStatement(lexer);
                //case TokenType.Continue:
                //    return new ContinueStatement(lexer);
                default:
                    {
                        Expression exp = Expression.PrimaryExp(lexer);
                        //        FunctionCallExpression fnexp = exp as FunctionCallExpression;

                        //        if (fnexp != null)
                        //            return new FunctionCallStatement(fnexp);
                        //        else
                    
                        return new AssignmentStatement(exp, lexer);
                    }
            }
        }
    }
}
