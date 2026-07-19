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
                var key_range = new SourceRange(lexer.Current.Position,
                    lexer.Current.Position + lexer.Current.Content.Length);
                key = new LiteralExpression(lexer.Current.Content, key_range);
                lexer.Next();
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

        public override void CompileRead(FunctionBuilder builder, RegisterContext target_register)
        {
            builder.AddInstruction(Instruction.NEW_TABLE(target_register.index), (ushort)SourceRange.from.line);

            int array_size = GetArrayElementsCount();

            // If the last field is a positional multi-value expression (function call or
            // '...'), it expands to ALL of its values as trailing array elements (Lua
            // multi return semantics). It is compiled with a variable result count and
            // the result span is resolved at runtime via frame.Top; a SET_LIST count of
            // -1 signals that.
            bool multi_return = args.Count > 0
                        && !args[args.Count - 1].HasKey
                        && args[args.Count - 1].Value.IsMultiValue;

            byte start_reg = array_size > 0 ? builder.AllocateRegisters(array_size) : (byte)0;
            byte array_index = 0;

            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                if (arg.HasKey)
                {
                    var context_key = arg.Key.CompileReadAuto(builder);
                    var context_val = arg.Value.CompileReadAuto(builder);

                    builder.AddInstruction (
                        Instruction.SET_TABLE (
                            target_register.index,
                            context_key.index,
                            context_val.index
                        ),
                        (ushort) arg.Key.SourceRange.from.line
                    );

                    builder.FreeRegisters(context_val);
                    builder.FreeRegisters(context_key);
                }
                else
                {
                    // The trailing multi return call occupies the top of the array block, so
                    // its arguments allocate right after its func slot (func_reg+1) where
                    // CALL expects them.
                    bool expand = multi_return && i == args.Count - 1;
                    var context = new RegisterContext((byte)(start_reg + array_index), expand ? -1 : 1);
                    arg.Value.CompileRead(builder, context);
                    array_index++;
                }
            }

            if (array_size > 0)
            {
                // count 0 => variable length: consume registers up to frame.Top left by
                // the trailing multi return call.
                int set_count = multi_return ? -1 : array_size;
                builder.AddInstruction(Instruction.SET_LIST(target_register.index, start_reg, set_count), (ushort)SourceRange.from.line);
                builder.FreeRegisters(array_size);
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

            public TableArgumentNode(string key, Expression value)
            {
                Key = new LiteralExpression(key, value.SourceRange);
                Value = value;
            }

            public TableArgumentNode(Expression key, Expression value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}
