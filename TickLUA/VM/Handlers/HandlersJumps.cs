namespace TickLUA.VM.Handlers
{
    internal static class HandlersJumps
    {
        internal static void JMP(TickVM vm, StackFrame frame, uint instruction)
        {
            int delta = Instruction.GetAxSigned(instruction);
            frame.PC += delta;
        }
    }
}
