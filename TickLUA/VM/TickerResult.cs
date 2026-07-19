namespace TickLUA.VM
{
    /// <summary>
    /// Outcome of a <see cref="Ticker"/> call.
    /// </summary>
    public enum TickerResult
    {
        /// <summary>The requested stride completed: execution is paused
        /// mid-script, ready for the next call.</summary>
        Advanced,

        /// <summary>The main chunk returned; the VM has no more work and its
        /// results are in <see cref="TickVM.ExecutionResult"/>.</summary>
        Finished,

        /// <summary>The tick limit ran out before the stride completed. The VM
        /// is paused in a consistent state — call again to keep going.</summary>
        LimitReached,
    }
}
