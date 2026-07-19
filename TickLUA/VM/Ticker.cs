using System;
using TickLUA.VM.Debugging;

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
        /// Run until the main chunk returns.
        /// </summary>
        public TickerResult TickToEnd(int maxTicks = DefaultMaxTicks)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                if (vm.IsFinished)
                    return TickerResult.Finished;
                vm.Tick();
            }

            return vm.IsFinished ? TickerResult.Finished : TickerResult.LimitReached;
        }

        private StackFrame PendingFrame()
        {
            var stack = vm.CurrentCoroutine.Stack;
            return stack.Count > 0 ? stack.Peek() : null;
        }
    }
}
