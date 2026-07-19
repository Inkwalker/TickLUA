using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM.Debugging
{
    /// <summary>
    /// An immutable point-in-time snapshot of one Lua call-stack frame, built
    /// by <see cref="DebugSession.GetCallStack"/>. Holds no reference to VM
    /// internals: names and lines are copied, and variable values are the
    /// public <see cref="LuaObject"/>s themselves. The snapshot does not track
    /// later execution — take a new one after ticking. Note that values are
    /// live references: drilling into a <see cref="TableObject"/> shows its
    /// current contents, same as <see cref="TickVM.Globals"/>. Treat them as
    /// read-only; mutating tables from the host bypasses the VM's memory
    /// accounting (the same accepted gap as host writes to Globals).
    /// </summary>
    public class DebugFrame
    {
        public string FunctionName { get; }

        /// <summary>
        /// The source line the frame is stopped on: for the innermost frame
        /// the pending instruction's line, for caller frames the line of the
        /// call in progress.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Named locals visible at the paused position, innermost shadowing
        /// winner per name. A local paused inside its own initializer is
        /// already listed but not yet assigned.
        /// </summary>
        public IReadOnlyDictionary<string, LuaObject> Locals { get; }

        /// <summary>
        /// The frame's named upvalues (includes _ENV on functions that
        /// reference globals).
        /// </summary>
        public IReadOnlyDictionary<string, LuaObject> Upvalues { get; }

        internal DebugFrame(string functionName, int line,
            IReadOnlyDictionary<string, LuaObject> locals,
            IReadOnlyDictionary<string, LuaObject> upvalues)
        {
            FunctionName = functionName;
            Line = line;
            Locals = locals;
            Upvalues = upvalues;
        }
    }
}
