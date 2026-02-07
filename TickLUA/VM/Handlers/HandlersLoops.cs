using System;
using System.Collections.Generic;
using System.Text;
using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal class HandlersLoops
    {
        internal static void FORPREP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_init  = instruction.A;
            int reg_step  = reg_init + 2;
            int jmp = instruction.BxSigned;

            var obj_init = (NumberObject)frame.Registers[reg_init];
            var obj_step = (NumberObject)frame.Registers[reg_step];

            frame.Registers[reg_init] = obj_init - obj_step;
            frame.PC += jmp;
        }

        internal static void FORLOOP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_int  = instruction.A;
            int reg_limit = reg_int + 1;
            int reg_step  = reg_int + 2;
            int reg_ext   = reg_int + 3;
            int jmp = instruction.BxSigned;

            var obj_int   = (NumberObject)frame.Registers[reg_int];
            var obj_limit = (NumberObject)frame.Registers[reg_limit];
            var obj_step  = (NumberObject)frame.Registers[reg_step];

            obj_int = obj_int + obj_step;

            bool condition = obj_step.Value > 0 ? obj_int <= obj_limit : obj_int >= obj_limit;

            if (condition)
            {
                frame.Registers[reg_int] = obj_int;
                frame.Registers[reg_ext] = obj_int;
                frame.PC += jmp;
            }

            // else the next instruction will be performed
        }
    }
}

namespace TickLUA.VM
{
    internal partial struct Instruction
    {
        /// <summary>
        /// Numeric for loop preparation. Requires 4 registers.
        /// </summary>
        /// <param name="start_reg">Start register index. 4 consequential registers will be used.</param>
        /// <param name="jump">Relative jump to FORLOOP instruction for this loop.</param>
        /// <remarks>
        /// Register usage: 
        /// <paramref name="start_reg"/>   - initial value and internal state.
        /// <paramref name="start_reg"/>+1 - limit value.
        /// <paramref name="start_reg"/>+2 - step value.
        /// <paramref name="start_reg"/>+3 - external value.
        /// </remarks>
        internal static Instruction FORPREP(byte start_reg, short jump) => new Instruction(Opcode.FORPREP, start_reg, jump);
        /// <summary>
        /// Numeric for loop test and increment. Requires 4 registers.
        /// </summary>
        /// <param name="start_reg">Start register index. 4 consequential registers will be used.</param>
        /// <param name="jump">Relative jump to FORLOOP instruction for this loop.</param>
        /// <remarks>
        /// Register usage: 
        /// <paramref name="start_reg"/>   - initial value and internal state.
        /// <paramref name="start_reg"/>+1 - limit value.
        /// <paramref name="start_reg"/>+2 - step value.
        /// <paramref name="start_reg"/>+3 - external value.
        /// </remarks>
        internal static Instruction FORLOOP(byte start_reg, short jump) => new Instruction(Opcode.FORLOOP, start_reg, jump);
    }
}
