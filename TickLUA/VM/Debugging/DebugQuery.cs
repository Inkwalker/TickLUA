using System.Collections.Generic;

namespace TickLUA.VM.Debugging
{
    /// <summary>
    /// The one place that interprets debug metadata (Meta.Locals, Meta.Lines).
    /// The public debugger facade contains no interpretation logic of its own.
    /// </summary>
    internal static class DebugQuery
    {
        /// <summary>
        /// Named locals visible at instruction <paramref name="pc"/>, as a
        /// name → register map. Entries are applied in declaration order, so
        /// for a shadowed name the innermost (last-declared) local wins.
        /// </summary>
        public static Dictionary<string, byte> GetVisibleLocals(LuaFunction function, int pc)
        {
            var visible = new Dictionary<string, byte>();

            foreach (var local in function.Meta.Locals)
            {
                if (local.StartPC <= pc && pc < local.EndPC)
                    visible[local.Name] = local.Register;
            }

            return visible;
        }

        /// <summary>
        /// The line of the instruction the frame will execute on its next tick.
        /// Step() pre-increments PC, so between ticks the pending instruction
        /// is at PC itself. This is the debugger's "current line" for a paused
        /// (innermost) frame. 0 when out of range.
        /// </summary>
        public static int NextLine(StackFrame frame) => LineAt(frame.Function, frame.PC);

        /// <summary>
        /// The line of the instruction the frame is in the middle of — the one
        /// at PC - 1 (see <see cref="NextLine"/> for the PC convention). This
        /// is the right line for caller frames, whose pending CALL has already
        /// been stepped past. Matches the traceback's convention. 0 when out
        /// of range.
        /// </summary>
        public static int InProgressLine(StackFrame frame) => LineAt(frame.Function, frame.PC - 1);

        /// <summary>
        /// Whether <paramref name="pc"/> is the first instruction of its line
        /// run. Breakpoints trigger only on line entries, so arriving at a line
        /// fires once (and again on each loop iteration) rather than once per
        /// instruction on that line.
        /// </summary>
        public static bool IsLineEntry(LuaFunction function, int pc)
        {
            var lines = function.Meta.Lines;
            if (pc < 0 || pc >= lines.Count)
                return false;
            if (lines[pc] == 0)
                return false; // synthetic instruction: never a line entry
            if (pc == 0)
                return true;
            return LineAt(function, pc - 1) != lines[pc];
        }

        private static int LineAt(LuaFunction function, int pc)
        {
            var lines = function.Meta?.Lines;
            if (lines == null || pc < 0 || pc >= lines.Count)
                return 0;

            // A raw 0 means the instruction was emitted without a source token
            // of its own; it belongs to the statement in progress, so inherit
            // the nearest preceding real line.
            for (int i = pc; i >= 0; i--)
            {
                if (lines[i] != 0)
                    return lines[i];
            }
            return 0;
        }
    }
}
