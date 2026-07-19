namespace TickLUA.VM.Debugging
{
    /// <summary>
    /// Outcome of a <see cref="DebugSession"/> stepping call.
    /// </summary>
    public enum StepResult
    {
        /// <summary>The step completed: execution is paused at a new location.</summary>
        Stepped,

        /// <summary>Execution is paused with a breakpoint's line pending (the
        /// breakpoint instruction has not executed yet).</summary>
        BreakpointHit,

        /// <summary>The main chunk returned; the VM has no more work.</summary>
        Finished,

        /// <summary>The tick budget ran out before the step condition was met.
        /// The VM is paused in a consistent state — call again to keep going.</summary>
        BudgetExhausted,
    }
}
