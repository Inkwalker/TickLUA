using System;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// A handle on a host-started call (see <see cref="TickVM.StartFunction"/>):
    /// the deferred result of a script function the host invoked. The call runs
    /// across subsequent <see cref="TickVM.Tick"/> calls, so the handle starts
    /// out <see cref="LuaCallStatus.Running"/> and settles later — poll
    /// <see cref="Status"/> each tick.
    ///
    /// <para>Neither <see cref="Resume"/> nor <see cref="Cancel"/> executes
    /// anything: both record the request and let the next tick act on it, so
    /// they are safe to call from anywhere, including a native function running
    /// inside a tick.</para>
    ///
    /// <example>
    /// <code>
    /// var call = vm.StartFunction("update", new NumberObject(dt));
    /// while (!call.IsFinished)
    /// {
    ///     vm.Tick();
    ///     if (call.Status == LuaCallStatus.Paused)
    ///         call.Resume(HandleYield(call.Result));
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public sealed class LuaCall : IDisposable
    {
        // The coroutine running the body. It points back here through
        // CoroutineObject.HostCall, which is how the VM reports outcomes.
        private readonly CoroutineObject coroutine;

        internal CoroutineObject Coroutine => coroutine;

        private bool disposed;

        internal LuaCall(TickVM vm, CoroutineObject coroutine)
        {
            VM = vm;
            this.coroutine = coroutine;
            Status = LuaCallStatus.Running;
            Result = LuaObject.NoResults;
        }

        /// <summary>
        /// The VM running this call — the one <see cref="TickVM.StartFunction"/>
        /// was called on. Saves passing the VM alongside the handle when the two
        /// travel together (a callback that only receives the call, say); ticking
        /// is still the host's job, and this handle does none of it.
        /// </summary>
        public TickVM VM { get; }

        /// <summary>Where the call currently stands.</summary>
        public LuaCallStatus Status { get; private set; }

        /// <summary>
        /// The call's values, read according to <see cref="Status"/>: the
        /// arguments the body passed to coroutine.yield while
        /// <see cref="LuaCallStatus.Paused"/>, the body's return values once
        /// <see cref="LuaCallStatus.Completed"/>, and empty otherwise. Never null.
        /// </summary>
        public LuaObject[] Result { get; private set; }

        /// <summary>
        /// The unhandled error that killed the call; null unless
        /// <see cref="Status"/> is <see cref="LuaCallStatus.Faulted"/>. The same
        /// error also propagates out of <see cref="TickVM.Tick"/>, so a host that
        /// ignores this handle still sees it.
        /// </summary>
        public RuntimeException Error { get; private set; }

        /// <summary>
        /// True once the call has settled — completed, faulted or cancelled —
        /// and can no longer change. A <see cref="LuaCallStatus.Paused"/> call is
        /// not finished: it is waiting for <see cref="Resume"/>.
        /// </summary>
        public bool IsFinished
            => Status == LuaCallStatus.Completed
            || Status == LuaCallStatus.Faulted
            || Status == LuaCallStatus.Cancelled;

        /// <summary>
        /// Continues a paused call, delivering <paramref name="args"/> as the
        /// results of the coroutine.yield the body is parked on. The body does
        /// not advance here — it resumes on the next <see cref="TickVM.Tick"/>.
        /// Throws when the call is not <see cref="LuaCallStatus.Paused"/>.
        /// </summary>
        public void Resume(params LuaObject[] args)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(LuaCall));

            if (!TryResume(args))
                throw new InvalidOperationException(
                    $"cannot resume a {Status} call (only a Paused call can be resumed)");
        }

        /// <summary>
        /// Like <see cref="Resume"/>, but returns false instead of throwing when
        /// the call is not <see cref="LuaCallStatus.Paused"/> — including when the
        /// handle has been disposed.
        /// </summary>
        public bool TryResume(params LuaObject[] args)
        {
            if (disposed || Status != LuaCallStatus.Paused)
                return false;

            Status = LuaCallStatus.Running;
            Result = LuaObject.NoResults;
            VM.QueueHostResume(coroutine, args);
            return true;
        }

        /// <summary>
        /// Stops the call: the body runs no further instructions and the
        /// coroutine dies, taking any coroutines it resumed with it. Idempotent,
        /// and a no-op once the call has finished.
        ///
        /// <para>The status changes immediately, but the coroutine's frames (and
        /// the memory they are charged for under
        /// <see cref="TickVMOptions.MaxMemoryBytes"/>) are released on the next
        /// <see cref="TickVM.Tick"/>.</para>
        /// </summary>
        public void Cancel()
        {
            if (IsFinished)
                return;

            Status = LuaCallStatus.Cancelled;
            Result = LuaObject.NoResults;
            VM.QueueHostCancel(coroutine);
        }

        /// <summary>
        /// Drops the handle: <see cref="Cancel"/>s the call if it has not
        /// finished, then detaches from the coroutine so the VM stops reporting
        /// here. Idempotent.
        ///
        /// <para>Disposing is optional — nothing here is unmanaged, and an
        /// abandoned handle is simply collected. It matters under
        /// <see cref="TickVMOptions.MaxMemoryBytes"/>: a paused call the host
        /// walks away from keeps its frames charged against the budget until the
        /// ledger's next correction scan, and disposing hands that budget back
        /// on the following tick.</para>
        ///
        /// <para>Mind the scope if you reach for <c>using</c>: leaving the block
        /// while the call is still <see cref="LuaCallStatus.Running"/> or
        /// <see cref="LuaCallStatus.Paused"/> cancels it. The tick loop that
        /// drives the call to completion belongs inside the block.</para>
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            Cancel();
            disposed = true;

            // Drop the coroutine's edge back to this handle. A script that
            // captured the coroutine (coroutine.running()) can otherwise keep a
            // detached handle alive, and there is no longer anything to report.
            coroutine.HostCall = null;
        }

        // --- Reported by the VM from the coroutine's lifecycle points. A
        // settled call ignores them: a cancel wins over anything still in
        // flight for that coroutine.

        internal void OnFinished(LuaObject[] results)
        {
            if (IsFinished)
                return;
            Status = LuaCallStatus.Completed;
            Result = results ?? LuaObject.NoResults;
        }

        internal void OnYielded(LuaObject[] values)
        {
            if (IsFinished)
                return;
            Status = LuaCallStatus.Paused;
            Result = values ?? LuaObject.NoResults;
        }

        internal void OnFaulted(RuntimeException error)
        {
            if (IsFinished)
                return;
            Status = LuaCallStatus.Faulted;
            Result = LuaObject.NoResults;
            Error = error;
        }
    }
}
