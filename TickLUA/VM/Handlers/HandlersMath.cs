using TickLUA.VM.Objects;

namespace TickLUA.VM.Handlers
{
    internal static class HandlersMath
    {
        internal static void ADD(TickVM vm, StackFrame frame, uint instruction)
        {
            int result_reg = Instruction.GetA(instruction);
            int l_reg      = Instruction.GetB(instruction);
            int r_reg      = Instruction.GetC(instruction);

            // TODO: type checks
            // TODO: metatables
            var l = frame.Registers[l_reg] as IntegerObject;
            var r = frame.Registers[r_reg] as IntegerObject;

            var result = l + r;

            frame.Registers[result_reg] = result;
        }
    }
}
