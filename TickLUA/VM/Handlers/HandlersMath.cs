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

            if (l is NumberObject nl && r is NumberObject nr)
                SetRegA(frame, instruction, nl + nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.AddKey);
        }

        internal static void SUB(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (l is NumberObject nl && r is NumberObject nr)
                SetRegA(frame, instruction, nl - nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.SubKey);
        }

        internal static void MUL(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (l is NumberObject nl && r is NumberObject nr)
                SetRegA(frame, instruction, nl * nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.MulKey);
        }

        internal static void MOD(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (l is NumberObject nl && r is NumberObject nr)
                SetRegA(frame, instruction, nl % nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.ModKey);
        }

        internal static void POW(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (l is NumberObject nl && r is NumberObject nr)
                SetRegA(frame, instruction, NumberObject.Pow(nl, nr));
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.PowKey);
        }

        internal static void DIV(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (l is NumberObject nl && r is NumberObject nr)
                SetRegA(frame, instruction, nl / nr);
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.DivKey);
        }

        internal static void IDIV(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (l is NumberObject nl && r is NumberObject nr)
                SetRegA(frame, instruction, NumberObject.IntDiv(nl, nr));
            else
                Metamethods.Math(vm, frame, instruction.A, l, r, Metamethods.IdivKey);
        }

        internal static void CONCAT(TickVM vm, StackFrame frame, Instruction instruction)
        {
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            if (l is StringObject sl && r is StringObject sr)
            {
                frame.Registers[instruction.A].Value = sl + sr;
                return;
            }

            var handler = Metamethods.GetHandler(l, Metamethods.ConcatKey)
                       ?? Metamethods.GetHandler(r, Metamethods.ConcatKey);
            if (handler == null)
            {
                // Number-to-string coercion is not implemented yet, so a number
                // operand is reported like any other non-string.
                var offender = l is StringObject ? r : l;
                throw new RuntimeException(
                    $"attempt to concatenate a {NativeArgs.TypeName(offender)} value");
            }

            Metamethods.Call(vm, frame, handler, new LuaObject[] { l, r }, instruction.A, 1);
        }

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
            Metamethods.Call(vm, frame, handler, new LuaObject[] { v, v }, instruction.A, 1);
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
