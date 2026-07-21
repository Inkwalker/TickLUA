using System.Collections.Generic;

namespace TickLUA.VM.Objects
{
    /// <summary>
    /// Lifecycle of a coroutine, mirroring the strings coroutine.status reports.
    /// </summary>
    internal enum CoroutineStatus
    {
        Suspended,
        Running,
        Normal,
        Dead,
    }

    /// <summary>
    /// A Lua thread: a coroutine owning its own call stack. The VM executes
    /// exactly one coroutine at a time (see <see cref="TickVM.CurrentCoroutine"/>);
    /// resume/yield switch which stack the tick loop operates on. The main chunk
    /// runs on an implicit main coroutine that has no body.
    /// </summary>
    internal sealed class CoroutineObject : LuaObject
    {
        // Rough x64 memory-accounting cost (see LuaObject.ShallowMemoryCost):
        // the coroutine object with its sinks and empty stack — frames bill
        // themselves on push/pop.
        internal const long HeaderMemoryCost = 128;

        /// <summary>The coroutine body: a ClosureObject or a plain native.</summary>
        internal LuaObject Body { get; }

        internal Stack<StackFrame> Stack { get; } = new Stack<StackFrame>();

        internal CoroutineStatus Status { get; set; }

        /// <summary>The body's root frame has been built (first resume happened).</summary>
        internal bool Started { get; set; }

        /// <summary>The coroutine to switch back to on yield/return/error; set on every resume.</summary>
        internal CoroutineObject Resumer { get; set; }

        /// <summary>
        /// Where yielded or returned values go, set by each resume. A field rather
        /// than the root frame's sink because successive resumes can come from
        /// different resumers.
        /// </summary>
        internal ResultsSinkDelegate PendingResumeSink { get; set; }

        /// <summary>
        /// Where a body error goes as (false, err). Null means wrap mode: the
        /// error propagates into the resumer's own stack instead.
        /// </summary>
        internal ErrorSinkDelegate PendingResumeErrorSink { get; set; }

        /// <summary>
        /// Set at each yield: delivers the next resume's arguments into the
        /// parked yield call site's result registers.
        /// </summary>
        internal ResultsSinkDelegate PendingYieldSink { get; set; }

        /// <summary>
        /// Set when this coroutine backs a host-started call (see
        /// <see cref="TickVM.StartFunction"/>). The VM reports returns, yields
        /// and errors here, which is why the handle stays accurate no matter who
        /// drove the resume — unlike <see cref="PendingResumeSink"/>, which every
        /// resume replaces.
        /// </summary>
        internal LuaCall HostCall { get; set; }

        internal CoroutineObject(LuaObject body)
        {
            Body = body;
            Status = CoroutineStatus.Suspended;
        }

        /// <summary>The main coroutine: no body, born running.</summary>
        internal CoroutineObject()
        {
            Status = CoroutineStatus.Running;
        }

        public override string ToString() => "< thread >";

        public override string TypeName => "thread";

        public override StringObject ToStringObject() => new StringObject("[thread]");

        // Header only: the frames bill themselves on push/pop.
        public override long ShallowMemoryCost() => HeaderMemoryCost;
    }
}
