using System;
using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM.Debugging
{
    /// <summary>
    /// The host's debugging surface over a <see cref="TickVM"/>: stepping,
    /// breakpoints, and stack/variable inspection. Nothing internal to the VM
    /// crosses this boundary — only names, line numbers, and
    /// <see cref="LuaObject"/> values.
    ///
    /// The session is a thin driver over <see cref="TickVM.Tick"/> and keeps
    /// no execution state of its own (each step call captures its reference
    /// point on entry), so mixing session calls with direct Tick() is safe.
    /// All multi-tick methods take a tick budget and return
    /// <see cref="StepResult.BudgetExhausted"/> when it runs out — the VM is
    /// cooperative, so this is how an infinite Lua loop stays interruptible.
    /// Call the method again to keep going.
    ///
    /// Stepping is at Lua level: a CALL of a native function executes the
    /// whole native inside one tick, so natives are stepped over by
    /// construction. Inspection and stepping follow the coroutine the VM is
    /// currently executing.
    /// </summary>
    public class DebugSession
    {
        public const int DefaultTickBudget = 100_000;

        private readonly TickVM vm;

        private readonly HashSet<int> breakpointLines = new HashSet<int>();
        private readonly HashSet<(string function, int line)> namedBreakpoints
            = new HashSet<(string function, int line)>();

        /// <exception cref="InvalidOperationException">
        /// The VM's bytecode was serialized with <c>stripDebugInfo</c>, so
        /// variable inspection is impossible. Check
        /// <see cref="LuaFunction.HasDebugInfo"/> before creating a session.
        /// </exception>
        public DebugSession(TickVM vm)
        {
            if (vm == null)
                throw new ArgumentNullException(nameof(vm));

            this.vm = vm;
            ValidateDebugInfo(vm.RootFunction);
        }

        private static void ValidateDebugInfo(LuaFunction function)
        {
            if (!function.HasDebugInfo)
                throw new InvalidOperationException(
                    $"Function '{function.Name}' was serialized without local-variable debug info; " +
                    "the debugger cannot inspect variables in stripped bytecode.");

            foreach (var nested in function.NestedFunctions)
                ValidateDebugInfo(nested);
        }

        public bool IsFinished => vm.IsFinished;

        /// <summary>
        /// The source line the VM is paused on (the pending instruction's
        /// line); 0 when finished.
        /// </summary>
        public int CurrentLine
        {
            get
            {
                var frame = PendingFrame();
                return frame == null ? 0 : DebugQuery.NextLine(frame);
            }
        }

        /// <summary>
        /// Name of the function the VM is paused in; null when finished.
        /// </summary>
        public string CurrentFunctionName => PendingFrame()?.Function.Name;

        /// <summary>
        /// The VM's running estimate of memory retained by script-reachable
        /// values, in bytes (same figure as
        /// <see cref="TickVM.EstimatedMemoryBytes"/>). Accounting only runs
        /// when a memory limit is configured — 0 when
        /// <see cref="MaxMemoryBytes"/> is null.
        /// </summary>
        public long MemoryBytes => vm.EstimatedMemoryBytes;

        /// <summary>
        /// The configured memory limit
        /// (<see cref="TickVMOptions.MaxMemoryBytes"/>); null when unlimited.
        /// </summary>
        public long? MaxMemoryBytes => vm.MaxMemoryBytes;

        /// <summary>
        /// Number of frames on the currently executing coroutine's call stack
        /// (matches <see cref="GetCallStack"/>.Count while paused).
        /// </summary>
        public int CallStackDepth => vm.CallStackDepth;

        /// <summary>
        /// The configured per-coroutine call stack limit
        /// (<see cref="TickVMOptions.MaxCallStackDepth"/>); null when
        /// unlimited.
        /// </summary>
        public int? MaxCallStackDepth => vm.MaxCallStackDepth;

        #region Stepping

        /// <summary>
        /// Execute exactly one instruction (one <see cref="TickVM.Tick"/>).
        /// </summary>
        public StepResult StepInstruction()
        {
            if (vm.IsFinished)
                return StepResult.Finished;

            vm.Tick();
            return vm.IsFinished ? StepResult.Finished : StepResult.Stepped;
        }

        /// <summary>
        /// Run until execution reaches a different source line, entering Lua
        /// calls (step into). Also stops when the current frame is left (call,
        /// return, coroutine switch) or when a one-line loop starts its next
        /// iteration (backward jump on the same line).
        /// </summary>
        public StepResult StepLine(int maxTicks = DefaultTickBudget)
        {
            if (vm.IsFinished)
                return StepResult.Finished;

            var start_frame = PendingFrame();
            int start_line = DebugQuery.NextLine(start_frame);
            int prev_pc = start_frame.PC;

            for (int i = 0; i < maxTicks; i++)
            {
                vm.Tick();
                if (vm.IsFinished)
                    return StepResult.Finished;
                if (AtBreakpoint())
                    return StepResult.BreakpointHit;

                var frame = PendingFrame();
                if (frame != start_frame)
                    return StepResult.Stepped;
                if (DebugQuery.NextLine(frame) != start_line)
                    return StepResult.Stepped;
                if (frame.PC <= prev_pc)
                    return StepResult.Stepped;
                prev_pc = frame.PC;
            }

            return StepResult.BudgetExhausted;
        }

        /// <summary>
        /// Run until execution reaches a different source line in the current
        /// function, without descending into calls. A resumed coroutine counts
        /// as a call and is skipped; stepping over a yield waits until the
        /// coroutine is resumed again. Returning out of the current frame
        /// (return, error unwind, tail call replacement) also completes the
        /// step. If the reference coroutine dies mid-step, degrades to
        /// line-stepping from wherever execution surfaces.
        /// </summary>
        public StepResult StepOver(int maxTicks = DefaultTickBudget)
        {
            if (vm.IsFinished)
                return StepResult.Finished;

            var start_co = vm.CurrentCoroutine;
            int start_depth = start_co.Stack.Count;
            var start_frame = PendingFrame();
            int start_line = DebugQuery.NextLine(start_frame);
            int prev_pc = start_frame.PC;

            for (int i = 0; i < maxTicks; i++)
            {
                vm.Tick();
                if (vm.IsFinished)
                    return StepResult.Finished;
                if (AtBreakpoint())
                    return StepResult.BreakpointHit;

                if (vm.CurrentCoroutine != start_co)
                {
                    if (start_co.Status == CoroutineStatus.Dead)
                    {
                        // The reference coroutine is gone (its body returned or
                        // an error unwound through it): re-anchor where
                        // execution surfaced and finish as a line step.
                        start_co = vm.CurrentCoroutine;
                        start_depth = start_co.Stack.Count;
                        start_frame = PendingFrame();
                        start_line = DebugQuery.NextLine(start_frame);
                        prev_pc = start_frame.PC;
                    }
                    continue;
                }

                int depth = start_co.Stack.Count;
                if (depth > start_depth)
                    continue;
                if (depth < start_depth)
                    return StepResult.Stepped;

                var frame = PendingFrame();
                if (frame != start_frame)
                    return StepResult.Stepped;
                if (DebugQuery.NextLine(frame) != start_line)
                    return StepResult.Stepped;
                if (frame.PC <= prev_pc)
                    return StepResult.Stepped;
                prev_pc = frame.PC;
            }

            return StepResult.BudgetExhausted;
        }

        /// <summary>
        /// Run until the current function returns to its caller (the current
        /// coroutine's stack gets shallower than it is now). If the current
        /// coroutine dies, stops where execution surfaces in the resumer.
        /// </summary>
        public StepResult StepOut(int maxTicks = DefaultTickBudget)
        {
            if (vm.IsFinished)
                return StepResult.Finished;

            var start_co = vm.CurrentCoroutine;
            int start_depth = start_co.Stack.Count;

            for (int i = 0; i < maxTicks; i++)
            {
                vm.Tick();
                if (vm.IsFinished)
                    return StepResult.Finished;
                if (AtBreakpoint())
                    return StepResult.BreakpointHit;

                if (vm.CurrentCoroutine != start_co)
                {
                    if (start_co.Status == CoroutineStatus.Dead)
                        return StepResult.Stepped;
                    continue;
                }

                if (start_co.Stack.Count < start_depth)
                    return StepResult.Stepped;
            }

            return StepResult.BudgetExhausted;
        }

        /// <summary>
        /// Run until a breakpoint's line is about to execute, or the VM
        /// finishes. Always makes progress: continuing from a paused
        /// breakpoint executes past it and stops on the next arrival.
        /// </summary>
        public StepResult Continue(int maxTicks = DefaultTickBudget)
        {
            if (vm.IsFinished)
                return StepResult.Finished;

            for (int i = 0; i < maxTicks; i++)
            {
                vm.Tick();
                if (vm.IsFinished)
                    return StepResult.Finished;
                if (AtBreakpoint())
                    return StepResult.BreakpointHit;
            }

            return StepResult.BudgetExhausted;
        }

        #endregion

        #region Breakpoints

        /// <summary>Break when the given source line is reached in any function.</summary>
        public void AddBreakpoint(int line) => breakpointLines.Add(line);

        /// <summary>
        /// Break only in the named function (e.g. "main" or "main.foo", as
        /// reported by <see cref="DebugFrame.FunctionName"/>).
        /// </summary>
        public void AddBreakpoint(string functionName, int line) => namedBreakpoints.Add((functionName, line));

        public void RemoveBreakpoint(int line) => breakpointLines.Remove(line);

        public void RemoveBreakpoint(string functionName, int line) => namedBreakpoints.Remove((functionName, line));

        public void ClearBreakpoints()
        {
            breakpointLines.Clear();
            namedBreakpoints.Clear();
        }

        /// <summary>
        /// Whether the pending instruction is the entry of a breakpointed line
        /// (see <see cref="DebugQuery.IsLineEntry"/>: fires once per arrival,
        /// including each loop iteration, not once per instruction).
        /// </summary>
        private bool AtBreakpoint()
        {
            if (breakpointLines.Count == 0 && namedBreakpoints.Count == 0)
                return false;

            var frame = PendingFrame();
            if (frame == null)
                return false;

            if (!DebugQuery.IsLineEntry(frame.Function, frame.PC))
                return false;

            int line = frame.Function.Meta.Lines[frame.PC];
            return breakpointLines.Contains(line)
                || namedBreakpoints.Contains((frame.Function.Name, line));
        }

        #endregion

        #region Inspection

        /// <summary>
        /// Snapshot of the currently executing coroutine's Lua call stack,
        /// innermost frame first. Native functions push no frame, so only Lua
        /// frames appear. See <see cref="DebugFrame"/> for snapshot semantics.
        /// </summary>
        public IReadOnlyList<DebugFrame> GetCallStack()
        {
            var stack = vm.CurrentCoroutine.Stack;
            var result = new List<DebugFrame>(stack.Count);

            bool innermost = true;
            foreach (var frame in stack)
            {
                // The innermost frame is paused before its pending instruction
                // (PC); callers are in the middle of their CALL (PC - 1).
                int pc = innermost ? frame.PC : frame.PC - 1;
                int line = innermost ? DebugQuery.NextLine(frame) : DebugQuery.InProgressLine(frame);

                result.Add(BuildFrame(frame, pc, line));
                innermost = false;
            }

            return result;
        }

        private static DebugFrame BuildFrame(StackFrame frame, int pc, int line)
        {
            var locals = new Dictionary<string, LuaObject>();
            foreach (var visible in DebugQuery.GetVisibleLocals(frame.Function, pc))
            {
                byte register = visible.Value;
                locals[visible.Key] = register < frame.Registers.Length
                    ? frame.Registers[register].Value
                    : NilObject.Nil;
            }

            var upvalues = new Dictionary<string, LuaObject>();
            int upvalue_count = Math.Min(frame.Function.Upvalues.Count, frame.Upvalues?.Length ?? 0);
            for (int i = 0; i < upvalue_count; i++)
            {
                string name = frame.Function.Upvalues[i].Name;
                if (name != null)
                    upvalues[name] = frame.Upvalues[i].Value;
            }

            return new DebugFrame(frame.Function.Name, line, locals, upvalues);
        }

        private StackFrame PendingFrame()
        {
            var stack = vm.CurrentCoroutine.Stack;
            return stack.Count > 0 ? stack.Peek() : null;
        }

        #endregion
    }
}
