using System.Drawing;
using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersCore
    {
        internal static void NOP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // No operation
        }

        internal static void MOVE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;
            byte b = instruction.B;

            // Move value from register b to register a
            frame.Registers[a].Value = frame.Registers[b].Value;
        }

        internal static void LOAD_CONST(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte  a   = instruction.A;
            ushort bx = instruction.Bx;

            // Load constant at index bx into register a
            frame.Registers[a].Value = frame.Constants[bx];
        }

        internal static void LOAD_INT(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte  a = instruction.A;
            short b = instruction.BxSigned;

            // Load integer b into register a
            frame.Registers[a].Value = new NumberObject(b);
        }

        internal static void LOAD_TRUE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;

            // Load boolean true into register a
            frame.Registers[a].Value = BooleanObject.True;
        }

        internal static void LOAD_FALSE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;

            // Load boolean false into register a
            frame.Registers[a].Value = BooleanObject.False;
        }

        internal static void LOAD_FALSE_SKIP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;

            // Load boolean false into register a and skip next instruction
            frame.Registers[a].Value = BooleanObject.False;
            frame.PC++;
        }

        internal static void LOAD_NIL(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;
            byte b = instruction.B;

            // Load nil into registers. a - start register, b - number of registers

            for (int i = 0; i < b; i++)
            {
                frame.Registers[a + i].Value = NilObject.Nil;
            }
        }

        internal static void CALL(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte func_reg = instruction.A;
            int arg_count = instruction.B - 1;
            int res_count = instruction.C - 1;

            var func_value = frame.Registers[func_reg].Value;
            if (func_value is ClosureObject closure)
            {
                var new_frame = new StackFrame(closure.Function, closure.Upvalues);

                int copy_count = System.Math.Min(arg_count, new_frame.Registers.Length);
                for (int i = 0; i < copy_count; i++)
                {
                    new_frame.Registers[i].Value = frame.Registers[func_reg + i + 1].Value;
                }

                new_frame.ResultsStartRegister = func_reg;
                new_frame.ResultsCount = res_count;

                vm.PushFrame(new_frame);
            }
            else
            {
                throw new RuntimeException($"Attempt to call a non-function value of type {func_value.GetType().Name}");
            }
        }

        internal static void RETURN(TickVM vm, StackFrame frame, Instruction instruction)
        {
            frame.PC = frame.Function.Instructions.Count;

            byte reg_start = instruction.A;
            int count = instruction.Bx - 1;

            if (count < 0)
                // Multi return: everything from reg_start up to the top
                count = System.Math.Max(0, frame.Top - reg_start);

            var results = new LuaObject[count];

            for (int i = 0; i < count; i++)
            {
                results[i] = frame.Registers[i + reg_start].Value;
            }

            vm.PopFrame();

            var caller_frame = vm.PeekFrame();

            if (caller_frame != null)
            {
                byte reg_result = frame.ResultsStartRegister;
                int expected_count = frame.ResultsCount;

                if (expected_count < 0)
                {
                    // Caller wanted all results: record where they end
                    expected_count = results.Length;
                    caller_frame.Top = reg_result + results.Length;
                }

                caller_frame.GrowRegisters(reg_result + expected_count);

                for (int i = 0; i < expected_count; i++)
                {
                    caller_frame.Registers[reg_result + i].Value = i < results.Length ? results[i] : NilObject.Nil;
                }
            }
            else
                vm.SetExecutionResult(results);
        }

        internal static void JMP(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int delta = instruction.AxSigned;
            frame.PC += delta;
        }

        internal static void CLOSURE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_result = instruction.A;
            int func_index = instruction.Bx;

            var func = frame.Function.NestedFunctions[func_index];
            var upvalues = new RegisterCell[func.Upvalues.Count];

            for (int i = 0; i < upvalues.Length; i++)
            {
                var def = func.Upvalues[i];
                if (def.IsLocalToParent)
                    upvalues[i] = frame.Registers[def.Index];
                else
                    upvalues[i] = frame.Upvalues[def.Index];
            }

            frame.Registers[reg_result].Value = new ClosureObject(func, upvalues);
        }

        internal static void CLOSE(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int start = instruction.A;

            for (int i = start; i < frame.Registers.Length; i++)
            {
                frame.Registers[i] = new RegisterCell { Value = NilObject.Nil };
            }
        }

        internal static void GET_UPVAL(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_dest     = instruction.A;
            int upval_source = instruction.B;

            var value = frame.Upvalues[upval_source].Value;
            frame.Registers[reg_dest].Value = value;
        }

        internal static void SET_UPVAL(TickVM vm, StackFrame frame, Instruction instruction)
        {
            int reg_source = instruction.A;
            int upval_dest = instruction.B;

            var value = frame.Registers[reg_source].Value;
            frame.Upvalues[upval_dest].Value =value;
        }
    }
}

namespace TickLUA.VM
{
    internal partial struct Instruction
    {
        internal static Instruction NOP() => new Instruction(Opcode.NOP);
        internal static Instruction MOVE(byte dest_reg, byte src_reg) => new Instruction(Opcode.MOVE, dest_reg, src_reg, 0);
        internal static Instruction LOAD_CONST(byte dest_reg, ushort const_index) => new Instruction(Opcode.LOAD_CONST, dest_reg, (short)const_index);
        internal static Instruction LOAD_INT(byte dest_reg, short integer) => new Instruction(Opcode.LOAD_INT, dest_reg, integer);
        internal static Instruction LOAD_TRUE(byte dest_reg) => new Instruction(Opcode.LOAD_TRUE, dest_reg, 0, 0);
        internal static Instruction LOAD_FALSE(byte dest_reg) => new Instruction(Opcode.LOAD_FALSE, dest_reg, 0, 0);
        internal static Instruction LOAD_FALSE_SKIP(byte dest_reg) => new Instruction(Opcode.LOAD_FALSE_SKIP, dest_reg, 0, 0);
        internal static Instruction LOAD_BOOL(byte dest_reg, bool value) => value ? LOAD_TRUE(dest_reg) : LOAD_FALSE(dest_reg);
        internal static Instruction LOAD_NIL(byte start_reg, byte count = 1) => new Instruction(Opcode.LOAD_NIL, start_reg, count, 0);
        internal static Instruction CALL(byte func_reg, int arg_count, int resul_count)
        {
            byte b = arg_count < -1 ? (byte)0 : (byte)(arg_count + 1);
            byte c = resul_count < -1 ? (byte)0 : (byte)(resul_count + 1);
            return new Instruction(Opcode.CALL, func_reg, b, c);
        }
        internal static Instruction RETURN(byte start_reg, int count)
        {
            ushort c = count < -1 ? (ushort)0 : (ushort)(count + 1);
            return new Instruction(Opcode.RETURN, start_reg, c);
        }
        internal static Instruction JMP(int offset) => new Instruction(Opcode.JMP, offset);

        internal static Instruction CLOSE(byte start_reg) => new Instruction(Opcode.CLOSE, start_reg, 0, 0);
        internal static Instruction CLOSURE(byte dest_reg, ushort func_index) => new Instruction(Opcode.CLOSURE, dest_reg, func_index);
        internal static Instruction GET_UPVAL(byte dest_reg, byte source_upval) => new Instruction(Opcode.GET_UPVAL, dest_reg, source_upval, 0);
        internal static Instruction SET_UPVAL(byte source_reg, byte dest_upval) => new Instruction(Opcode.SET_UPVAL, source_reg, dest_upval, 0);
    }
}
