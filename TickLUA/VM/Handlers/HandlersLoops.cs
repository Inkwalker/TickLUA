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

            var obj_init = (NumberObject)frame.Registers[reg_init].Value;
            var obj_step = (NumberObject)frame.Registers[reg_step].Value;

            frame.Registers[reg_init].Value = obj_init - obj_step;
            frame.PC += jmp;
        }

        internal static void FORLOOP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_int  = instruction.A;
            int reg_limit = reg_int + 1;
            int reg_step  = reg_int + 2;
            int reg_ext   = reg_int + 3;
            int jmp = instruction.BxSigned;

            var obj_int   = (NumberObject)frame.Registers[reg_int].Value;
            var obj_limit = (NumberObject)frame.Registers[reg_limit].Value;
            var obj_step  = (NumberObject)frame.Registers[reg_step].Value;

            obj_int = obj_int + obj_step;

            bool condition = obj_step.Value > 0 ? obj_int <= obj_limit : obj_int >= obj_limit;

            if (condition)
            {
                frame.Registers[reg_int].Value = obj_int;
                frame.Registers[reg_ext].Value = obj_int;
                frame.PC += jmp;
            }

            // else the next instruction will be performed
        }

        internal static void TFORCALL(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_base = instruction.A;
            int var_count = instruction.B;
            int reg_vars = reg_base + 3;

            var func_value = frame.Registers[reg_base].Value;
            var state = frame.Registers[reg_base + 1].Value;
            var control = frame.Registers[reg_base + 2].Value;

            if (func_value is ClosureObject closure)
            {
                var new_frame = new StackFrame(closure.Function, closure.Upvalues);

                int param_count = closure.Function.HasVarargs
                    ? closure.Function.ParameterCount
                    : new_frame.Registers.Length;

                if (param_count > 0) new_frame.Registers[0].Value = state;
                if (param_count > 1) new_frame.Registers[1].Value = control;

                if (closure.Function.HasVarargs && param_count < 2)
                {
                    new_frame.Varargs = param_count == 1
                        ? new LuaObject[] { control }
                        : new LuaObject[] { state, control };
                }

                // Results land in the loop variable registers; the iterator/state/control
                // triple below them stays intact for the next iteration.
                new_frame.ResultsStartRegister = (byte)reg_vars;
                new_frame.ResultsCount = var_count;

                vm.PushFrame(new_frame);
            }
            else if (func_value is NativeFunctionObject native)
            {
                var args = new LuaObject[] { state, control };
                var results = native.Function(new NativeArgs(args, native.Name)) ?? LuaObject.NoResults;

                frame.GrowRegisters(reg_vars + var_count);

                for (int i = 0; i < var_count; i++)
                {
                    frame.Registers[reg_vars + i].Value =
                        i < results.Length && results[i] != null ? results[i] : NilObject.Nil;
                }
            }
            else
            {
                throw new RuntimeException($"Attempt to call a non-function value of type {func_value.GetType().Name}");
            }
        }

        internal static void TFORLOOP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_base = instruction.A;
            int jmp = instruction.BxSigned;

            var first_result = frame.Registers[reg_base + 3].Value;

            if (!(first_result is NilObject))
            {
                frame.Registers[reg_base + 2].Value = first_result;
                frame.PC += jmp;
            }

            // else the loop is over and the next instruction will be performed
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
        /// <summary>
        /// Generic for loop iterator call.
        /// </summary>
        /// <param name="start_reg">Base register index.</param>
        /// <param name="var_count">Number of loop variables receiving iterator results.</param>
        /// <remarks>
        /// Register usage:
        /// <paramref name="start_reg"/>   - iterator function.
        /// <paramref name="start_reg"/>+1 - state value.
        /// <paramref name="start_reg"/>+2 - control value.
        /// <paramref name="start_reg"/>+3 onwards - loop variables (<paramref name="var_count"/> registers).
        /// </remarks>
        internal static Instruction TFORCALL(byte start_reg, byte var_count) => new Instruction(Opcode.TFORCALL, start_reg, var_count, 0);
        /// <summary>
        /// Generic for loop test. Continues the loop while the first iterator result is not nil.
        /// </summary>
        /// <param name="start_reg">Base register index, same as the paired TFORCALL.</param>
        /// <param name="jump">Relative jump back to the first instruction of the loop body.</param>
        internal static Instruction TFORLOOP(byte start_reg, short jump) => new Instruction(Opcode.TFORLOOP, start_reg, jump);
    }
}
