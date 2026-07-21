using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// Receives the results of a completed call. Every frame delivers its
    /// results exclusively through its sink (see <see cref="StackFrame.Sink"/>);
    /// the sink captures its target when the frame is built, so a returning
    /// frame never touches its caller's state directly.
    /// </summary>
    internal delegate void ResultsSinkDelegate(LuaObject[] results);

    /// <summary>
    /// Receives the error value when an error unwinds to a protected frame
    /// (see <see cref="StackFrame.ErrorSink"/>).
    /// </summary>
    internal delegate void ErrorSinkDelegate(LuaObject error);

    /// <summary>
    /// Factories for the standard result deliveries: caller registers,
    /// conditional branch skips, pcall wrapping, top-level execution result.
    /// </summary>
    internal static class ResultsSink
    {
        /// <summary>
        /// Standard delivery: writes results into the target's registers at
        /// start_reg, truncating or nil-padding to res_count (&lt; 0 = all
        /// results, recording the end in <see cref="StackFrame.Top"/>).
        /// </summary>
        internal static ResultsSinkDelegate ToRegisters(StackFrame target, byte start_reg, int res_count)
            => results => WriteResults(target, start_reg, res_count, results);

        /// <summary>
        /// Delivery for calls whose results are ignored (__newindex handlers).
        /// </summary>
        internal static readonly ResultsSinkDelegate Discard = results => { };

        /// <summary>
        /// Delivery for comparison metamethods (EQ/LT/LE): skips the caller's
        /// next instruction when the boolean-coerced first result differs from
        /// the expected flag encoded in the comparison instruction.
        /// </summary>
        internal static ResultsSinkDelegate Branch(StackFrame caller, bool expected)
            => results =>
            {
                var first = results.Length > 0 ? results[0] : NilObject.Nil;
                if ((bool)first.ToBooleanObject() != expected)
                    caller.PC++;
            };

        /// <summary>
        /// pcall success: reports success by prepending true to the results,
        /// then delivers through the inner sink.
        /// </summary>
        internal static ResultsSinkDelegate PcallSuccess(ResultsSinkDelegate inner)
            => results =>
            {
                var prepended = new LuaObject[results.Length + 1];
                prepended[0] = BooleanObject.True;
                System.Array.Copy(results, 0, prepended, 1, results.Length);
                inner(prepended);
            };

        /// <summary>
        /// pcall catch: packages an error as (false, error value) and delivers
        /// it through the inner sink.
        /// </summary>
        internal static ErrorSinkDelegate PcallCatch(ResultsSinkDelegate inner)
            => error => inner(new LuaObject[] { BooleanObject.False, error });

        private static void WriteResults(StackFrame frame, byte start_reg, int res_count, LuaObject[] results)
        {
            int expected_count = res_count;
            if (expected_count < 0)
            {
                // Caller wanted all results: record where they end for the consuming
                // variable-count CALL/RETURN/SET_LIST.
                expected_count = results.Length;
                frame.Top = start_reg + results.Length;
            }

            frame.GrowRegisters(start_reg + expected_count);

            for (int i = 0; i < expected_count; i++)
            {
                frame.Registers[start_reg + i].Value =
                    i < results.Length && results[i] != null ? results[i] : NilObject.Nil;
            }
        }
    }
}
