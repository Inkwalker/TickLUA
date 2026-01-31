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
