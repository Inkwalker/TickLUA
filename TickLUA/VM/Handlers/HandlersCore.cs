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

            if (arg_count < 0)
                // Variable arg count: everything from func_reg + 1 up to the top,
                // left there by a preceding variable-count CALL or VARARG.
                arg_count = System.Math.Max(0, frame.Top - func_reg - 1);

            // All call flavors deliver their results the same way: into the
            // calling frame's registers starting at func_reg.
            var sink = ResultsSink.ToRegisters(frame, func_reg, res_count);

            var func_value = frame.Registers[func_reg].Value;
            if (func_value is ClosureObject closure)
            {
                var new_frame = BuildClosureFrame(frame, closure, func_reg + 1, arg_count, sink);
                vm.PushFrame(new_frame);
            }
            else if (func_value is NativeFunctionObject native)
            {
                if (native.VmFunction != null)
                {
                    // VM-aware native (e.g. pcall): manages caller registers and the
                    // call stack itself.
                    native.VmFunction(vm, frame, func_reg, arg_count, res_count);
                    return;
                }

                // Copy args out of the caller registers — results are written back
                // starting at func_reg, overwriting the argument registers.
                var args = new LuaObject[arg_count];
                for (int i = 0; i < arg_count; i++)
                {
                    args[i] = frame.Registers[func_reg + 1 + i].Value;
                }

                // Synchronous: the whole native call costs exactly one tick, no frame push.
                var results = native.Function(new NativeArgs(args, native.Name)) ?? LuaObject.NoResults;

                sink(results);
            }
            else
            {
                // Not a function: resolve __call (or fail). Args are snapshotted
                // because __call prepends the callee, shifting them all.
                var args = new LuaObject[arg_count];
                for (int i = 0; i < arg_count; i++)
                {
                    args[i] = frame.Registers[func_reg + 1 + i].Value;
                }

                Metamethods.Call(vm, func_value, args, sink);
            }
        }

        /// <summary>
        /// Builds a callee frame for a closure call: copies arguments from the caller's
        /// registers (starting at arg_start_reg), routes surplus args of a vararg
        /// function to its Varargs store, and attaches the result delivery sink.
        /// The caller pushes the returned frame.
        /// </summary>
        internal static StackFrame BuildClosureFrame(StackFrame caller, ClosureObject closure,
            int arg_start_reg, int arg_count, ResultsSinkDelegate sink)
        {
            var new_frame = new StackFrame(closure.Function, closure.Upvalues);

            // Args beyond the declared parameters of a vararg function go to its
            // Varargs store instead of registers; for a non-vararg function they
            // are simply dropped.
            int param_count = closure.Function.HasVarargs
                ? closure.Function.ParameterCount
                : new_frame.Registers.Length;

            int copy_count = System.Math.Min(arg_count, param_count);
            for (int i = 0; i < copy_count; i++)
            {
                new_frame.Registers[i].Value = caller.Registers[arg_start_reg + i].Value;
            }

            if (closure.Function.HasVarargs && arg_count > param_count)
            {
                var varargs = new LuaObject[arg_count - param_count];
                for (int i = 0; i < varargs.Length; i++)
                {
                    varargs[i] = caller.Registers[arg_start_reg + param_count + i].Value;
                }
                new_frame.Varargs = varargs;
            }

            new_frame.Sink = sink;

            return new_frame;
        }

        /// <summary>
        /// Builds a callee frame for a call whose arguments live in an array
        /// rather than in caller registers (metamethod dispatch, __call with a
        /// prepended self). Mirrors <see cref="BuildClosureFrame"/> including
        /// the vararg-overflow split.
        /// </summary>
        internal static StackFrame BuildArgsFrame(ClosureObject closure, LuaObject[] args,
            ResultsSinkDelegate sink)
        {
            var new_frame = new StackFrame(closure.Function, closure.Upvalues);

            int param_count = closure.Function.HasVarargs
                ? closure.Function.ParameterCount
                : new_frame.Registers.Length;

            int copy_count = System.Math.Min(args.Length, param_count);
            for (int i = 0; i < copy_count; i++)
            {
                new_frame.Registers[i].Value = args[i];
            }

            if (closure.Function.HasVarargs && args.Length > param_count)
            {
                var varargs = new LuaObject[args.Length - param_count];
                System.Array.Copy(args, param_count, varargs, 0, varargs.Length);
                new_frame.Varargs = varargs;
            }

            new_frame.Sink = sink;

            return new_frame;
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

            // Every frame is constructed with a sink; delivery is entirely its
            // concern — RETURN knows nothing about the caller.
            frame.Sink(results);
        }

        internal static void VARARG(TickVM vm, StackFrame frame, Instruction instruction)
        {
            byte a = instruction.A;
            int count = instruction.Bx - 1;

            var varargs = frame.Varargs;

            if (count < 0)
            {
                // Expand all varargs: record where they end for the consuming
                // variable-count CALL/RETURN/SET_LIST.
                count = varargs.Length;
                frame.GrowRegisters(a + count);
                frame.Top = a + count;
            }

            for (int i = 0; i < count; i++)
            {
                frame.Registers[a + i].Value = i < varargs.Length ? varargs[i] : NilObject.Nil;
            }
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
        internal static Instruction VARARG(byte dest_reg, int count)
        {
            ushort bx = count < -1 ? (ushort)0 : (ushort)(count + 1);
            return new Instruction(Opcode.VARARG, dest_reg, bx);
        }
        internal static Instruction JMP(int offset) => new Instruction(Opcode.JMP, offset);

        internal static Instruction CLOSE(byte start_reg) => new Instruction(Opcode.CLOSE, start_reg, 0, 0);
        internal static Instruction CLOSURE(byte dest_reg, ushort func_index) => new Instruction(Opcode.CLOSURE, dest_reg, func_index);
        internal static Instruction GET_UPVAL(byte dest_reg, byte source_upval) => new Instruction(Opcode.GET_UPVAL, dest_reg, source_upval, 0);
        internal static Instruction SET_UPVAL(byte source_reg, byte dest_upval) => new Instruction(Opcode.SET_UPVAL, source_reg, dest_upval, 0);
    }
}
