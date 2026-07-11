using TickLUA.Compilers.LUA.Lexer;
using TickLUA.VM;
using TickLUA.VM.Objects;

namespace TickLUA.Compilers.LUA.Parser.Expressions
{
    /// <summary>
    /// Method call 'obj:name(args)', sugar for obj.name(obj, args) with obj
    /// evaluated only once.
    /// </summary>
    internal class MethodCallExpression : FunctionCallExpression
    {
        private string method_name;

        public MethodCallExpression(Expression obj, string method_name, LuaLexer lexer)
            : base(obj, lexer)
        {
            this.method_name = method_name;
        }

        public override void CompileRead(FunctionBuilder builder, RegisterContext func_register)
        {
            ushort line = (ushort)SourceRange.from.line;

            bool multi_args = args.Count > 0 && args[args.Count - 1].IsMultiValue;

            // The object goes into the first argument slot (func_reg + 1) as the
            // implicit 'self'; the explicit arguments follow it.
            int args_start = builder.AllocateRegisters(args.Count + 1);
            var self_context = new RegisterContext { index = (byte)args_start, count = 1 };
            function_expr.CompileRead(builder, self_context);

            // func_reg = self[method_name]
            ushort name_const = builder.AddConstant(new StringObject(method_name));
            var context_key = builder.AllocateRegistersContext(1);
            builder.AddInstruction(Instruction.LOAD_CONST(context_key.index, name_const), line);
            builder.AddInstruction(Instruction.GET_TABLE(func_register.index, self_context.index, context_key.index), line);
            builder.FreeRegisters(context_key);

            for (int i = 0; i < args.Count; i++)
            {
                bool expand = multi_args && i == args.Count - 1;
                var context = new RegisterContext { index = (byte)(args_start + 1 + i), count = expand ? -1 : 1 };
                args[i].CompileRead(builder, context);
            }

            builder.AddInstruction(emit_tail_call
                ? Instruction.TAILCALL(func_register.index, multi_args ? -1 : args.Count + 1)
                : Instruction.CALL(func_register.index, multi_args ? -1 : args.Count + 1, func_register.count),
                line);

            builder.FreeRegisters(args.Count + 1);
        }
    }
}
