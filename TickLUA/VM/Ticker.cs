using System;
using TickLUA.VM.Debugging;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// A small host-side driver over <see cref="TickVM.Tick"/> that advances
    /// the VM in useful strides: a batch of ticks, one source line, or all the
    /// way to completion. The open-ended methods take a tick limit and return
    /// <see cref="TickerResult.LimitReached"/> when it runs out — the VM is
    /// cooperative, so this is how a script's infinite loop stays
    /// interruptible. Call the method again to keep going.
    ///
    /// The ticker keeps no execution state of its own (each call captures its
    /// reference point on entry), so mixing it with direct Tick() calls or a
    /// <see cref="DebugSession"/> is safe. Unlike a debug session it needs no
    /// debug info: line stepping only uses instruction line numbers, which
    /// survive bytecode stripping.
    /// </summary>
    public class Ticker
    {
        public const int DefaultMaxTicks = 100_000;

        private readonly TickVM vm;

        public Ticker(TickVM vm)
        {
            if (vm == null)
                throw new ArgumentNullException(nameof(vm));

            this.vm = vm;
        }

        public bool IsFinished => vm.IsFinished;

        /// <summary>
        /// The source line the VM is paused on (the pending instruction's
        /// line); 0 when finished or when line info is absent.
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
        /// Advance the VM by up to <paramref name="ticks"/> instructions,
        /// stopping early only when the main chunk returns. The count is the
        /// goal, not a guard, so running it in full is
        /// <see cref="TickerResult.Advanced"/>, never
        /// <see cref="TickerResult.LimitReached"/>.
        /// </summary>
        public TickerResult Tick(int ticks = 1)
        {
            if (ticks < 1)
                throw new ArgumentOutOfRangeException(nameof(ticks),
                    "Tick count must be at least 1");

            for (int i = 0; i < ticks; i++)
            {
                if (vm.IsFinished)
                    return TickerResult.Finished;
                vm.Tick();
            }

            return vm.IsFinished ? TickerResult.Finished : TickerResult.Advanced;
        }

        /// <summary>
        /// Run until execution reaches a different source line, entering Lua
        /// calls. Also stops when the current frame is left (call, return,
        /// coroutine switch) or when a one-line loop starts its next iteration
        /// (backward jump on the same line) — same reference-point rules as
        /// <see cref="DebugSession.StepLine"/>.
        /// </summary>
        public TickerResult TickLine(int maxTicks = DefaultMaxTicks)
        {
            if (vm.IsFinished)
                return TickerResult.Finished;

            var start_frame = PendingFrame();
            int start_line = DebugQuery.NextLine(start_frame);
            int prev_pc = start_frame.PC;

            for (int i = 0; i < maxTicks; i++)
            {
                vm.Tick();
                if (vm.IsFinished)
                    return TickerResult.Finished;

                var frame = PendingFrame();
                if (frame != start_frame)
                    return TickerResult.Advanced;
                if (DebugQuery.NextLine(frame) != start_line)
                    return TickerResult.Advanced;
                if (frame.PC <= prev_pc)
                    return TickerResult.Advanced;
                prev_pc = frame.PC;
            }

            return TickerResult.LimitReached;
        }

        /// <summary>
        /// Run until there is nothing left to run: the main chunk (see
        /// <see cref="TickVM.Load"/>) and any calls started along the way, all
        /// the way to completion. A main chunk that yields is resumed with no
        /// arguments rather than being mistaken for finished.
        ///
        /// <para>A host call left <see cref="LuaCallStatus.Paused"/> is not
        /// resumed — nothing here knows what to feed it. Drive those with
        /// <see cref="TickCallToEnd"/>.</para>
        /// </summary>
        public TickerResult TickToEnd(int maxTicks = DefaultMaxTicks)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                // A parked main chunk leaves the VM with nothing to tick, which
                // is not the same as the chunk being done.
                var main = vm.MainCall;
                if (main != null && main.Status == LuaCallStatus.Paused)
                    main.Resume();

                if (vm.IsFinished)
                    return TickerResult.Finished;

                vm.Tick();
            }

            return vm.IsFinished ? TickerResult.Finished : TickerResult.LimitReached;
        }

        /// <summary>
        /// Starts the global function <paramref name="globalName"/> and runs it
        /// to completion, resuming it with no arguments each time it yields —
        /// the "just run this entry point" driver, for entry points that use
        /// coroutine.yield only to stay interruptible rather than to talk to the
        /// host.
        ///
        /// <para>Returns the call's handle: <see cref="LuaCall.IsFinished"/>
        /// tells you whether it ran out, and
        /// <see cref="LuaCall.Result"/>/<see cref="LuaCall.Error"/> carry the
        /// outcome. A call left unfinished by the tick limit is still live —
        /// hand it back to <see cref="TickCallToEnd"/> to keep going.</para>
        ///
        /// <para>Ticking stops as soon as the call settles, so a main chunk this
        /// parked is left where it was for the host to drive on.</para>
        ///
        /// <para>Throws <see cref="ArgumentException"/> when the global is
        /// missing or not startable; use
        /// <see cref="TickVM.TryStartFunction(string, out LuaCall, LuaObject[])"/>
        /// plus <see cref="TickCallToEnd"/> for optional entry points.</para>
        /// </summary>
        public LuaCall RunFunction(string globalName, params LuaObject[] args)
        {
            return RunFunction(globalName, DefaultMaxTicks, args);
        }

        /// <summary>
        /// <see cref="RunFunction(string, LuaObject[])"/> with an explicit tick
        /// limit.
        /// </summary>
        public LuaCall RunFunction(string globalName, int maxTicks, params LuaObject[] args)
        {
            var call = vm.StartFunction(globalName, args);
            TickCallToEnd(call, maxTicks);
            return call;
        }

        /// <summary>
        /// Runs an already-started call to completion, resuming it with no
        /// arguments each time it yields. Returns
        /// <see cref="TickerResult.Finished"/> once the call settles — completed,
        /// faulted or cancelled — or <see cref="TickerResult.LimitReached"/> when
        /// the budget runs out first; call it again to keep going.
        ///
        /// <para>Unlike <see cref="TickToEnd"/> this tracks the call, not the
        /// main chunk, so it stops the moment the call is done even if the VM has
        /// other work parked. A host that needs to see the yielded values, or to
        /// resume with arguments, should drive the call itself instead —
        /// see <see cref="LuaCall.Resume"/>.</para>
        /// </summary>
        public TickerResult TickCallToEnd(LuaCall call, int maxTicks = DefaultMaxTicks)
        {
            if (call == null)
                throw new ArgumentNullException(nameof(call));
            if (call.VM != vm)
                throw new ArgumentException("The call belongs to a different VM", nameof(call));
            if (maxTicks < 1)
                throw new ArgumentOutOfRangeException(nameof(maxTicks),
                    "Tick count must be at least 1");

            for (int i = 0; i < maxTicks; i++)
            {
                if (call.IsFinished)
                    return TickerResult.Finished;

                // Yielded values are dropped: this driver is for calls that
                // yield to stay interruptible, not to exchange data.
                if (call.Status == LuaCallStatus.Paused)
                    call.Resume();

                vm.Tick();
            }

            return call.IsFinished ? TickerResult.Finished : TickerResult.LimitReached;
        }

        private StackFrame PendingFrame()
        {
            var stack = vm.CurrentCoroutine.Stack;
            return stack.Count > 0 ? stack.Peek() : null;
        }
    }
}
