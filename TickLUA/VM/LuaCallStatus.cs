namespace TickLUA.VM
{
    /// <summary>
    /// Lifecycle of a host-started call (see <see cref="TickVM.StartFunction"/>).
    /// The host polls <see cref="LuaCall.Status"/> — typically once per tick —
    /// rather than being called back, so nothing ever runs inside a tick on the
    /// host's behalf.
    /// </summary>
    public enum LuaCallStatus
    {
        /// <summary>The body is executing, or is queued to resume on the next tick.</summary>
        Running,

        /// <summary>
        /// The body called coroutine.yield and is parked. The yielded values are
        /// in <see cref="LuaCall.Result"/>; <see cref="LuaCall.Resume"/> continues it.
        /// </summary>
        Paused,

        /// <summary>The body returned. Its return values are in <see cref="LuaCall.Result"/>.</summary>
        Completed,

        /// <summary>
        /// The body raised an error that no pcall caught. The error is in
        /// <see cref="LuaCall.Error"/>; it also propagates out of
        /// <see cref="TickVM.Tick"/> as usual.
        /// </summary>
        Faulted,

        /// <summary>The host called <see cref="LuaCall.Cancel"/>; the body will not run again.</summary>
        Cancelled,
    }
}
