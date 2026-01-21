using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersMath
    {
        internal static void ADD(TickVM vm, StackFrame frame, uint instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l + r);
        }

        internal static void SUB(TickVM vm, StackFrame frame, uint instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l - r);
        }

        internal static void MUL(TickVM vm, StackFrame frame, uint instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l * r);
        }

        internal static void MOD(TickVM vm, StackFrame frame, uint instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l % r);
        }

        internal static void POW(TickVM vm, StackFrame frame, uint instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, NumberObject.Pow(l, r));
        }

        internal static void DIV(TickVM vm, StackFrame frame, uint instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, l / r);
        }

        internal static void IDIV(TickVM vm, StackFrame frame, uint instruction)
        {
            // TODO: type checks
            var l = GetRegB(frame, instruction);
            var r = GetRegC(frame, instruction);

            SetRegA(frame, instruction, NumberObject.IntDiv(l,r));
        }

        private static void SetRegA(StackFrame frame, uint instruction, NumberObject value)
        {
            int a = Instruction.GetA(instruction);
            frame.Registers[a] = value;
        }
        private static NumberObject GetRegB(StackFrame frame, uint instruction)
        {
            int b = Instruction.GetB(instruction);
            return frame.Registers[b] as NumberObject;
        }
        private static NumberObject GetRegC(StackFrame frame, uint instruction)
        {
            int c = Instruction.GetC(instruction);
            return frame.Registers[c] as NumberObject;
        }
    }
}
