using TickLUA.Compilers.LUA.Parser.Expressions;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class NumericForLoopStatement : ForLoopStatement
    {
        private string variable_name;
        private Expression init_expr;
        private Expression limit_expr;
        private Expression step_expr;
        private CompoundStatement body;

        public NumericForLoopStatement(string variable_name, Expression init_expr, Expression limit_expr, Expression step_expr, CompoundStatement body, SourceRange range)
        {
            this.variable_name = variable_name;
            this.init_expr = init_expr;
            this.limit_expr = limit_expr;
            this.step_expr = step_expr;
            this.body = body;

            SourceRange = range;
        }

        public override void Compile(FunctionBuilder builder)
        {
            builder.BlockStart();

            byte reg_int   = builder.AllocateRegisters(4);
            byte reg_limit = (byte)(reg_int + 1);
            byte reg_step  = (byte)(reg_int + 2);
            byte reg_ext   = (byte)(reg_int + 3);

            builder.NameRegister(reg_ext, variable_name);

            var context_int   = new Expression.RegisterContext(reg_int, 1);
            var context_limit = new Expression.RegisterContext(reg_limit, 1);
            var context_step  = new Expression.RegisterContext(reg_step, 1);

            init_expr.CompileRead(builder, context_int);
            limit_expr.CompileRead(builder, context_limit);

            if (step_expr != null)
            {
                step_expr.CompileRead(builder, context_step);
            }
            else
            {
                // load default step of 1 if no step expression provided
                builder.AddInstruction(Instruction.LOAD_INT(reg_step, 1), (ushort)SourceRange.from.line);
            }

            int addr_forprep = builder.AddInstruction(Instruction.NOP(), (ushort)SourceRange.from.line); // FORPREP placeholder

            body.Compile(builder);

            // Close upvalues if there any
            if (builder.BlockHasEscapingVars())
            {
                builder.AddInstruction(Instruction.CLOSE(reg_ext), (ushort)SourceRange.from.line);
            }

            int addr_forloop = builder.InstructionCount;

            builder.AddInstruction(Instruction.FORLOOP(reg_int, (short)(addr_forprep - addr_forloop)), (ushort)SourceRange.from.line);
            builder.SetInstruction(addr_forprep, Instruction.FORPREP(reg_int, (short)(addr_forloop - addr_forprep - 1)));

            //builder.FreeRegisters(4);

            builder.BlockEnd();
        }
    }
}
