using System.Collections.Generic;
using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    /// <summary>
    /// Table definition expression
    /// </summary>
    internal class TableExpression : Expression
    {
        private List<TableArgumentNode> args;

        public TableExpression(LuaLexer lexer)
        {
            var start_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.BRK_CUR_Left);

            args = new List<TableArgumentNode>();

            while (lexer.Current.Type != TokenType.BRK_CUR_Right)
            {
                switch (lexer.Current.Type)
                {
                    case TokenType.Name: //key = value
                        if (lexer.Peek().Type == TokenType.OP_Assignment)
                            ParseMapField(lexer);
                        else
                            ParseArrayField(lexer);
                        break;
                    case TokenType.BRK_SQR_Left: // [key] = value
                        ParseMapField(lexer);
                        break;
                    default: 
                        ParseArrayField(lexer); 
                        break;
                }

                if (lexer.Current.Type == TokenType.Coma || lexer.Current.Type == TokenType.Semicolon) lexer.Next();
            }

            var end_pos = lexer.Current.Position;
            AssertTokenNext(lexer, TokenType.BRK_CUR_Right);

            SourceRange = new SourceRange(start_pos, end_pos);
        }

        private void ParseArrayField(LuaLexer lexer)
        {
            Expression key = null;
            var value = Expression.Create(lexer);
            args.Add(new TableArgumentNode(key, value));
        }

        private void ParseMapField(LuaLexer lexer)
        {
            Expression key = null;
            if (lexer.Current.Type == TokenType.Name)
            {
                //key = new LiteralExpression(lexer.Current.Content);
                //lexer.Next();
                throw new System.NotImplementedException("String keys are not implemented");
            }
            else if (lexer.Current.Type == TokenType.BRK_SQR_Left)
            {
                lexer.Next();
                key = Expression.Create(lexer);
                AssertTokenNext(lexer, TokenType.BRK_SQR_Right);
            }

            AssertTokenNext(lexer, TokenType.OP_Assignment);
            var value = Expression.Create(lexer);
            args.Add(new TableArgumentNode(key, value));
        }

        public override void CompileRead(FunctionBuilder builder, byte reg_result)
        { 
            builder.AddInstruction(Instruction.NEW_TABLE(reg_result), (ushort)SourceRange.from.line);

            int array_size = GetArrayElementsCount();
            byte start_reg = array_size > 0 ? builder.AllocateRegisters(array_size) : (byte)0;
            byte array_index = 0;

            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                if (arg.HasKey)
                {
                    byte reg_key = builder.AllocateRegisters(1);
                    byte reg_val = builder.AllocateRegisters(1);

                    arg.Key.CompileRead(builder, reg_key);
                    arg.Value.CompileRead(builder, reg_val);
                    builder.AddInstruction(Instruction.SET_TABLE(reg_result, reg_key, reg_val), (ushort)arg.Key.SourceRange.from.line);
                    
                    builder.DeallocateRegisters(reg_key);
                    builder.DeallocateRegisters(reg_val);
                }
                else
                {
                    arg.Value.CompileRead(builder, (byte)(start_reg + array_index));
                    array_index++;
                }
            }

            if (array_size > 0)
            {
                builder.AddInstruction(Instruction.SET_LIST(reg_result, start_reg, (byte)array_size), (ushort)SourceRange.from.line);
                builder.DeallocateRegisters(start_reg, array_size);
            }
        }

        private int GetArrayElementsCount()
        {
            int count = 0;
            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].HasKey == false) count++;
            }
            return count;
        }


        class TableArgumentNode
        {
            public Expression Key { get; }
            public Expression Value { get; }

            public bool HasKey => Key != null;

            public TableArgumentNode(Expression value)
            {
                Key = null;
                Value = value;
            }

            //public TableArgumentNode(string key, Expression value)
            //{
            //    Key = new LiteralExpression(key);
            //    Value = value;
            //}

            public TableArgumentNode(Expression key, Expression value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}
