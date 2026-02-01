using System.Runtime.CompilerServices;
using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersMath
    {
        internal static void ADD(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l + r);
        }

        internal static void SUB(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l - r);
        }

        internal static void MUL(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l * r);
        }

        internal static void MOD(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l % r);
        }

        internal static void POW(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, NumberObject.Pow(l, r));
        }

        internal static void DIV(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l / r);
        }

        internal static void IDIV(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, NumberObject.IntDiv(l,r));
        }

        internal static void UNM(TickVM vm, StackFrame frame, Instruction instruction)
        {
            // TODO: type checks
            var v = GetRegB(frame, instruction);

            SetRegA(frame, instruction, -v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetRegA(StackFrame frame, Instruction instruction, NumberObject value)
        {
            int a = instruction.A;
            frame.Registers[a] = value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NumberObject GetRegB(StackFrame frame, Instruction instruction)
        {
            int b = instruction.B;
            return frame.Registers[b] as NumberObject;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NumberObject GetRegC(StackFrame frame, Instruction instruction)
        {
            int c = instruction.C;
            return frame.Registers[c] as NumberObject;
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

        internal static Instruction UNM(byte dest_reg, byte reg_source) => new Instruction(Opcode.UNM, dest_reg, reg_source, 0);
    }
}
