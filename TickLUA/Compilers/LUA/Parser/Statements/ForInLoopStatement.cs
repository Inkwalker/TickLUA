using System.Collections.Generic;
using TickLUA.Compilers.LUA.Parser.Expressions;
using TickLUA.VM;

namespace TickLUA.Compilers.LUA.Parser.Statements
{
    internal class ForInLoopStatement : ForLoopStatement
    {
        private List<string> variables;
        private List<Expression> values;
        private CompoundStatement body;

        public ForInLoopStatement(List<string> variables, List<Expression> values, CompoundStatement body, SourceRange range)
        {
            this.variables = variables;
            this.values = values;
            this.body = body;

            SourceRange = range;
        }

        public override void Compile(FunctionBuilder builder)
        {
            ushort line = (ushort)SourceRange.from.line;

            builder.LoopStart();
            builder.BlockStart();

            // Compile the expression list into the three control registers
            // (iterator function, state, control), nil-padding when short and
            // expanding a trailing multi-value expression. Registers are allocated
            // one slot at a time so that a trailing call finds its argument
            // registers right after its own slot (see the register-order note in
            // AssignmentStatement.CompileExpandedCall).
            byte reg_base = 0;
            for (int i = 0; i < 3; i++)
            {
                byte reg = builder.AllocateRegisters(1);
                if (i == 0) reg_base = reg;

                if (i == values.Count - 1 && i < 2 && values[i].IsMultiValue)
                {
                    values[i].CompileRead(builder, new Expression.RegisterContext(reg, 3 - i));
                    builder.AllocateRegisters(2 - i);
                    break;
                }

                if (i < values.Count)
                    values[i].CompileRead(builder, new Expression.RegisterContext(reg, 1));
                else
                    builder.AddInstruction(Instruction.LOAD_NIL(reg), line);
            }

            // Values beyond the control triple are still evaluated for their side
            // effects, then discarded.
            for (int i = 3; i < values.Count; i++)
            {
                var context = builder.AllocateRegistersContext(1);
                values[i].CompileRead(builder, context);
                builder.FreeRegisters(context);
            }

            byte reg_vars = builder.AllocateRegisters(variables.Count);
            for (int i = 0; i < variables.Count; i++)
            {
                builder.NameRegister(reg_vars + i, variables[i]);
            }

            int addr_jmp = builder.AddInstruction(Instruction.NOP(), line); // JMP to TFORCALL placeholder

            int addr_body = builder.InstructionCount;
            body.Compile(builder);

            // Close upvalues if there any
            if (builder.BlockHasEscapingVars())
            {
                builder.AddInstruction(Instruction.CLOSE(reg_vars), line);
            }

            int addr_tforcall = builder.InstructionCount;
            builder.AddInstruction(Instruction.TFORCALL(reg_base, (byte)variables.Count), line);
            builder.AddInstruction(Instruction.TFORLOOP(reg_base, (short)(addr_body - addr_tforcall - 2)), line);

            builder.SetInstruction(addr_jmp, Instruction.JMP(addr_tforcall - addr_jmp - 1));

            builder.BlockEnd();
            builder.LoopEnd();
        }
    }
}
