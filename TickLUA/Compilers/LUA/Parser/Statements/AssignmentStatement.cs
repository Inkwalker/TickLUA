using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.Compilers.LUA.Parser.Expressions;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class AssignmentStatement : Statement
    {
        private List<LValueExpression> variables;
        private List<Expression> values;

        private BinaryOperation operation;

        private bool local;

        public AssignmentStatement(Expression first_var, LuaLexer lexer)
        {
            variables = new List<LValueExpression>();

            if (first_var is LValueExpression v)
                variables.Add(v);
            else
                throw new CompilationException($"Attempt to assign a value to {first_var}", lexer.Current.Position);

            values = new List<Expression>();
            local = false;

            var start_pos = first_var.SourceRange.from;

            if (lexer.Current.Type == TokenType.Coma)
            {
                lexer.Next();
                ParsePrimaryVars(lexer);
            }

            operation = GetOp(lexer.Current.Type);
            lexer.Next();

            ParseValues(lexer);

            var end_pos = lexer.Current.Position;

            SourceRange = new SourceRange(start_pos, end_pos);
        }

        public AssignmentStatement(bool local, LuaLexer lexer)
        {
            variables = new List<LValueExpression>();
            values = new List<Expression>();
            this.local = local;

            var start_pos = lexer.Current.Position;

            if (local)
                ParseLocalVars(lexer);
            else
                ParsePrimaryVars(lexer);

            operation = GetOp(lexer.Current.Type);
            lexer.Next();

            ParseValues(lexer);

            var end_pos = lexer.Current.Position;

            SourceRange = new SourceRange(start_pos, end_pos);
        }

        private void ParseValues(LuaLexer lexer)
        {
            do
            {
                if (lexer.Current.Type == TokenType.Coma) lexer.Next();
                var value = Expression.Create(lexer);
                values.Add(value);
            }
            while (lexer.Current.Type == TokenType.Coma);
        }

        private void ParseLocalVars(LuaLexer lexer)
        {
            do
            {
                if (lexer.Current.Type == TokenType.Coma) lexer.Next();

                if (lexer.Current.Type == TokenType.Name)
                {
                    var variable = new SymbolExpression(lexer);
                    variable.IsLocal = true;
                    variables.Add(variable);
                }
                else throw new CompilationException("Only names are allowed for local assignments", lexer.Current.Position);
            }
            while (lexer.Current.Type == TokenType.Coma);
        }

        private void ParsePrimaryVars(LuaLexer lexer)
        {
            do
            {
                if (lexer.Current.Type == TokenType.Coma) lexer.Next();
                var variable = Expression.PrimaryExp(lexer);
                if (variable is LValueExpression v)
                    variables.Add(v);
                else
                    throw new CompilationException($"Can't assign value to {variable}", lexer.Current.Position);
            }
            while (lexer.Current.Type == TokenType.Coma);
        }

        private BinaryOperation GetOp(TokenType token)
        {
            switch (token)
            {
                case TokenType.OP_Assignment:      return BinaryOperation.Invalid;
                case TokenType.OP_Assignment_Add:  return BinaryOperation.Add;
                case TokenType.OP_Assignment_Sub:  return BinaryOperation.Sub;
                case TokenType.OP_Assignment_Div:  return BinaryOperation.Div;
                case TokenType.OP_Assignment_iDiv: return BinaryOperation.iDiv;
                case TokenType.OP_Assignment_Mul:  return BinaryOperation.Mul;
                case TokenType.OP_Assignment_Mod:  return BinaryOperation.Mod;
                case TokenType.OP_Assignment_Pow:  return BinaryOperation.Pow;
                default: 
                    return BinaryOperation.Invalid;
            }
        }

        public override void Compile(FunctionBuilder builder)
        {
            if (values.Count == 1 && variables.Count == 1) //code optimization
            {
                var bin_op = ApplyOperation(variables[0], values[0]);

                byte reg_result = builder.AllocateRegisters(1);
                bin_op.CompileRead(builder, reg_result);

                variables[0].CompileWrite(builder, reg_result);

                builder.DeallocateRegisters(reg_result);
            }
            //else
            //{
            //    for (int i = 0; i < values.Count; i++)
            //    {
            //        if (i < variables.Count)
            //        {
            //            var value = ApplyOperation(variables[i] as Expression, values[i]);
            //            value.Compile(context, code);
            //        }
            //    }
            //    code += Instruction.MakeTuple(Math.Min(variables.Count, values.Count));
            //    for (int i = 0; i < variables.Count; i++)
            //    {
            //        code += Instruction.Dupe();
            //        code += Instruction.TupleIndex(i);
            //        variables[i].CompileStore(context, code, local);
            //    }
            //}
        }

        private Expression ApplyOperation(Expression variable, Expression value)
        {
            if (operation == BinaryOperation.Invalid) return value;


            return new BinaryExpression(variable, value, operation);
        }
    }
}
