using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    abstract class ForLoopStatement : Statement
    {
        public static new ForLoopStatement Create(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.For);

            var varibles = new List<string>();

            AssertToken(lexer.Current, TokenType.Name);
            varibles.Add(lexer.Current.Content);
            lexer.Next();

            switch (lexer.Current.Type)
            {
                case TokenType.Coma:
                case TokenType.In:
                    //return ParseForInLoop(varibles, lexer, line);
                    throw new CompilationException("For in loops are not implemented yet", start_pos);
                case TokenType.OP_Assignment:
                    return ParseForRangeLoop(varibles[0], lexer, start_pos);
                default: throw new CompilationException($"Unexpected symbol near {lexer.Current.Type}", lexer.Current.Position);
            }
        }

        //private static ForLoopStatement ParseForInLoop(List<string> variables, Lexer lexer, int startLine)
        //{
        //    while (true)
        //    {
        //        if (lexer.Current.Type == TokenType.Coma) lexer.Next();
        //        if (lexer.Current.Type == TokenType.In) break;

        //        AssertToken(lexer.Current, TokenType.Name);

        //        var var_name = lexer.Current.Content;
        //        variables.Add(var_name);

        //        lexer.Next();
        //    }

        //    AssertTokenNext(lexer, TokenType.In);

        //    Expression iterator = Expression.Create(lexer);

        //    AssertTokenNext(lexer, TokenType.Do);

        //    CompoundStatement body = new CompoundStatement(lexer);

        //    AssertTokenNext(lexer, TokenType.End);

        //    return new ForInLoopStatement(variables, iterator, body, startLine);
        //}

        public static ForLoopStatement ParseForRangeLoop(string variable, LuaLexer lexer, SourcePosition start_pos)
        {
            AssertTokenNext(lexer, TokenType.OP_Assignment);

            var init_value_expr = Expression.Create(lexer);

            AssertTokenNext(lexer, TokenType.Coma);
            var limit_value_expr = Expression.Create(lexer);

            Expression step_value_expr = null;
            if (lexer.Current.Type == TokenType.Coma)
            {
                lexer.Next();
                step_value_expr = Expression.Create(lexer);
            }

            AssertTokenNext(lexer, TokenType.Do);

            var body = new BlockStatement(lexer);

            var end_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.End);

            var range = new SourceRange(start_pos, end_pos);

            return new NumericForLoopStatement(variable, init_value_expr, limit_value_expr, step_value_expr, body, range);
        }
    }
}
