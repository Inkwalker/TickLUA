using System.Runtime.CompilerServices;
using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersMath
    {
        internal static void ADD(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (TryMath(l, r, out var nl, out var nr))
                SetRegA(frame, instruction, nl + nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.AddKey);
        }

        internal static void SUB(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (TryMath(l, r, out var nl, out var nr))
                SetRegA(frame, instruction, nl - nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.SubKey);
        }

        internal static void MUL(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (TryMath(l, r, out var nl, out var nr))
                SetRegA(frame, instruction, nl * nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.MulKey);
        }

        internal static void MOD(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (TryMath(l, r, out var nl, out var nr))
                SetRegA(frame, instruction, nl % nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.ModKey);
        }

        internal static void POW(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (TryMath(l, r, out var nl, out var nr))
                SetRegA(frame, instruction, NumberObject.Pow(nl, nr));
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.PowKey);
        }

        internal static void DIV(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (TryMath(l, r, out var nl, out var nr))
                SetRegA(frame, instruction, nl / nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.DivKey);
        }

        internal static void IDIV(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (TryMath(l, r, out var nl, out var nr))
                SetRegA(frame, instruction, NumberObject.IntDiv(nl, nr));
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.IdivKey);
        }

        /// <summary>
        /// Resolves both arithmetic operands to numbers, coercing numeric
        /// strings (Lua converts a string that looks like a number before
        /// consulting metamethods). Returns false if either operand is not a
        /// number and not a numeric string, leaving metamethod dispatch to
        /// handle it and report errors against the original operands.
        /// </summary>
        private static bool TryMath(LuaObject l, LuaObject r, out NumberObject nl, out NumberObject nr)
        {
            nl = AsMathNumber(l);
            nr = AsMathNumber(r);
            return nl != null && nr != null;
        }

        private static NumberObject AsMathNumber(LuaObject o)
        {
            if (o is NumberObject n)
                return n;
            if (o is StringObject s && NumberObject.TryParse(s.Value, out var parsed))
                return parsed;
            return null;
        }

        internal static void CONCAT(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            // Strings and numbers concatenate directly; a number operand is
            // coerced to its string form (all numbers are floats, so integral
            // values render without a trailing ".0", matching reference Lua).
            if (IsConcatable(l) && IsConcatable(r))
            {
                frame.Registers[instruction.A].Value =
                    new StringObject(l.ToStringObject().Value + r.ToStringObject().Value);
                return;
            }

            var handler = Metamethods.GetHandler(l, Metamethods.ConcatKey)
                       ?? Metamethods.GetHandler(r, Metamethods.ConcatKey);
            if (handler == null)
            {
                var offender = IsConcatable(l) ? r : l;
                throw new RuntimeException(
                    $"attempt to concatenate a {NativeArgs.TypeName(offender)} value");
            }

            Metamethods.Call(vm, handler, new LuaObject[] { l, r },
                ResultsSink.ToRegisters(frame, instruction.A, 1));
        }

        /// <summary>
        /// Concatenation operands that need no metamethod: strings and numbers.
        /// </summary>
        private static bool IsConcatable(LuaObject o) => o is StringObject || o is NumberObject;

        internal static void UNM(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var v = GetRegB(frame, instruction);

            if (v is NumberObject n)
            {
                SetRegA(frame, instruction, -n);
                return;
            }

            var handler = Metamethods.GetHandler(v, Metamethods.UnmKey);
            if (handler == null)
                throw new RuntimeException(
                    $"attempt to perform arithmetic on a {NativeArgs.TypeName(v)} value");

            // Per Lua, the handler receives the operand twice.
            Metamethods.Call(vm, handler, new LuaObject[] { v, v },
                ResultsSink.ToRegisters(frame, instruction.A, 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetRegA(StackFrame frame, Instruction instruction, NumberObject value)
        {
            int a = instruction.A;
            frame.Registers[a].Value = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LuaObject GetRegB(StackFrame frame, Instruction instruction)
        {
            int b = instruction.B;
            return frame.Registers[b].Value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static LuaObject GetRegC(StackFrame frame, Instruction instruction)
        {
            int c = instruction.C;
            return frame.Registers[c].Value;
        }
    }
}

namespace TickLUA.VM
{
    internal partial struct Instruction
    {
        internal static Instruction ADD(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.ADD, dest_reg, left_reg, right_reg);
        internal static Instruction SUB(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.SUB, dest_reg, left_reg, right_reg);
        internal static Instruction MUL(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.MUL, dest_reg, left_reg, right_reg);
        internal static Instruction MOD(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.MOD, dest_reg, left_reg, right_reg);
        internal static Instruction POW(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.POW, dest_reg, left_reg, right_reg);
        internal static Instruction DIV(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.DIV, dest_reg, left_reg, right_reg);
        internal static Instruction IDIV(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.IDIV, dest_reg, left_reg, right_reg);

        internal static Instruction CONCAT(byte dest_reg, byte left_reg, byte right_reg) => new Instruction(Opcode.CONCAT, dest_reg, left_reg, right_reg);

        internal static Instruction UNM(byte dest_reg, byte reg_source) => new Instruction(Opcode.UNM, dest_reg, reg_source, 0);
    }
}
