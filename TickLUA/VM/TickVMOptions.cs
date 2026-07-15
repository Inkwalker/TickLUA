namespace TickLUA.VM
{
    /// <summary>
    /// Resource limits for a VM instance, supplied to the <see cref="TickVM"/>
    /// constructor. Every limit is unlimited by default.
    /// </summary>
    public class TickVMOptions
    {
        /// <summary>
        /// Maximum number of frames on the active coroutine's call stack; each
        /// coroutine has its own stack and is measured against the limit
        /// separately. Exceeding it raises a "stack overflow" Lua error, which
        /// pcall can catch. Tail calls replace their frame and never grow the
        /// stack. Null means unlimited.
        /// </summary>
        public int? MaxCallStackDepth { get; set; }

        /// <summary>
        /// Approximate cap, in bytes, on the memory retained by values the
        /// script can reach (strings, tables, closures, coroutines, call
        /// frames). Exceeding it raises a "not enough memory" Lua error, which
        /// pcall can catch. Null means unlimited.
        ///
        /// Accounting is deliberately approximate — it bounds real usage
        /// within a small constant factor rather than measuring it exactly:
        /// values are billed per referencing slot (a string stored in two
        /// tables counts twice), numbers/booleans are free (fixed-size and
        /// bounded by the slots that hold them), and bytecode is excluded.
        /// The running estimate can drift above real usage when structures
        /// become garbage; a reachability scan corrects it before the limit
        /// is enforced, so only genuinely retained data triggers the error.
        /// Data the host injects (e.g. into Globals) is counted from the
        /// first correction scan onward.
        /// </summary>
        public long? MaxMemoryBytes { get; set; }
    }
}
