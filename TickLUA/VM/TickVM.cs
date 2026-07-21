using System;
using System.Collections.Generic;
using TickLUA.VM.Handlers;
using System.Linq;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// Host-supplied source reader backing require/dofile: receives the
    /// name/path exactly as the script wrote it and returns Lua source text,
    /// or null when there is no such module/file.
    /// </summary>
    public delegate string ModuleReaderDelegate(string path);

    public class TickVM
    {
        internal delegate void InstructionHandler(TickVM vm, StackFrame frame, Instruction instruction);

        private readonly Dictionary<Opcode, InstructionHandler> instructionSet = new Dictionary<Opcode, InstructionHandler>
        {
            // Core operations
            { Opcode.NOP,             HandlersCore.NOP },
            { Opcode.MOVE,            HandlersCore.MOVE },
            { Opcode.LOAD_CONST,      HandlersCore.LOAD_CONST },
            { Opcode.LOAD_INT,        HandlersCore.LOAD_INT },
            { Opcode.LOAD_TRUE,       HandlersCore.LOAD_TRUE },
            { Opcode.LOAD_FALSE,      HandlersCore.LOAD_FALSE },
            { Opcode.LOAD_FALSE_SKIP, HandlersCore.LOAD_FALSE_SKIP },
            { Opcode.LOAD_NIL,        HandlersCore.LOAD_NIL },
            { Opcode.JMP,             HandlersCore.JMP },
            // Math operations
            { Opcode.ADD,             HandlersMath.ADD },
            { Opcode.SUB,             HandlersMath.SUB },
            { Opcode.MUL,             HandlersMath.MUL },
            { Opcode.MOD,             HandlersMath.MOD },
            { Opcode.POW,             HandlersMath.POW },
            { Opcode.DIV,             HandlersMath.DIV },
            { Opcode.IDIV,            HandlersMath.IDIV },
            { Opcode.CONCAT,          HandlersMath.CONCAT },
            { Opcode.UNM,             HandlersMath.UNM },
            // Functions
            { Opcode.GET_UPVAL,       HandlersCore.GET_UPVAL },
            { Opcode.SET_UPVAL,       HandlersCore.SET_UPVAL },
            { Opcode.CLOSE,           HandlersCore.CLOSE },
            { Opcode.CLOSURE,         HandlersCore.CLOSURE },
            { Opcode.CALL,            HandlersCore.CALL },
            { Opcode.TAILCALL,        HandlersCore.TAILCALL },
            { Opcode.RETURN,          HandlersCore.RETURN },
            { Opcode.VARARG,          HandlersCore.VARARG },
            // Logic
            { Opcode.TEST,            HandlersLogic.TEST },
            { Opcode.TESTSET,         HandlersLogic.TESTSET },
            { Opcode.EQ,              HandlersLogic.EQ },
            { Opcode.LT,              HandlersLogic.LT },
            { Opcode.LE,              HandlersLogic.LE },
            { Opcode.NOT,             HandlersLogic.NOT },
            // Tables
            { Opcode.NEW_TABLE,       HandlersTable.NEW_TABLE },
            { Opcode.SET_LIST,        HandlersTable.SET_LIST },
            { Opcode.SET_TABLE,       HandlersTable.SET_TABLE },
            { Opcode.SET_FIELD,       HandlersTable.SET_FIELD },
            { Opcode.GET_TABLE,       HandlersTable.GET_TABLE },
            { Opcode.GET_FIELD,       HandlersTable.GET_FIELD },
            { Opcode.LEN,             HandlersTable.LEN },
            // Loops
            { Opcode.FORPREP,         HandlersLoops.FORPREP },
            { Opcode.FORLOOP,         HandlersLoops.FORLOOP },
            { Opcode.TFORCALL,        HandlersLoops.TFORCALL },
            { Opcode.TFORLOOP,        HandlersLoops.TFORLOOP },
        };

        private readonly InstructionHandler[] instructionHandlers;

        // Execution always operates on the current coroutine's stack; every call
        // gets its own, the main chunk included. Resume/yield switch which
        // coroutine is current, and with it the stack every handler sees.
        //
        // The idle root is the one coroutine that never runs Lua code: it is
        // what is current when nothing is executing, and it terminates every
        // resumer chain (see SwitchBack).
        private readonly CoroutineObject idleCoroutine = new CoroutineObject();
        private CoroutineObject currentCoroutine;

        // Every call's coroutine, so the memory ledger's correction scan can
        // reach one that is merely suspended — nothing else references it.
        // Pruned of dead entries as the scan walks it.
        private readonly List<CoroutineObject> liveCoroutines = new List<CoroutineObject>();

        private Stack<StackFrame> CallStack => currentCoroutine.Stack;

        /// <summary>
        /// A resume or cancel the host requested through a <see cref="LuaCall"/>,
        /// parked until the next tick can apply it safely.
        /// </summary>
        private struct PendingHostOp
        {
            public CoroutineObject Coroutine;
            public LuaObject[] Args;
            public bool IsCancel;
        }

        private readonly Queue<PendingHostOp> pendingHostOps = new Queue<PendingHostOp>();

        internal CoroutineObject CurrentCoroutine => currentCoroutine;

        /// <summary>The coroutine running the main chunk; null until <see cref="Load"/>.</summary>
        internal CoroutineObject MainCoroutine => MainCall?.Coroutine;

        // Every call — the main chunk included — runs on its own coroutine, so
        // "finished" means there is simply nothing to execute: control is back
        // at the idle root and no queued resume or cancel is waiting for the
        // next tick. That covers a VM with nothing loaded, one whose calls have
        // all settled, and one whose calls are parked at a yield: none of them
        // has an instruction to run until the host asks for more.
        public bool IsFinished => currentCoroutine == idleCoroutine
            && pendingHostOps.Count == 0;

        // Per-coroutine frame budget; int.MaxValue when no limit was configured.
        private readonly int maxCallStackDepth;

        // Approximate memory accounting; null when no memory limit was
        // configured (all choke points then no-op).
        private readonly MemoryLedger ledger;

        /// <summary>
        /// The ledger's running estimate of memory retained by script-reachable
        /// values, in bytes. An upper-bound approximation (see
        /// <see cref="TickVMOptions.MaxMemoryBytes"/>); 0 when no memory limit
        /// is configured.
        /// </summary>
        public long EstimatedMemoryBytes => ledger?.Total ?? 0;

        /// <summary>
        /// Current number of frames on the call stack.
        /// </summary>
        internal int CallStackDepth => CallStack.Count;

        /// <summary>
        /// The configured memory limit
        /// (<see cref="TickVMOptions.MaxMemoryBytes"/>), or null when
        /// unlimited.
        /// </summary>
        internal long? MaxMemoryBytes => ledger?.MaxBytes;

        /// <summary>
        /// The configured per-coroutine call stack limit
        /// (<see cref="TickVMOptions.MaxCallStackDepth"/>), or null when
        /// unlimited.
        /// </summary>
        internal int? MaxCallStackDepth
            => maxCallStackDepth == int.MaxValue ? (int?)null : maxCallStackDepth;

        /// <summary>
        /// The main chunk's call, or null until <see cref="Load"/> is called.
        /// </summary>
        public LuaCall MainCall { get; private set; }

        /// <summary>
        /// What the main chunk returned — a shortcut to
        /// <see cref="MainCall"/>.<see cref="LuaCall.Result"/>. Null until
        /// <see cref="Load"/>, and empty until the chunk returns.
        /// </summary>
        public LuaObject[] ExecutionResult => MainCall?.Result;

        /// <summary>
        /// The main chunk this VM loaded. Retained for the debugger, which
        /// validates debug info over the whole function tree.
        /// </summary>
        internal LuaFunction RootFunction { get; private set; }

        /// <summary>
        /// The globals table, exposed to scripts as _ENV. Hosts can register
        /// native functions and other globals here before ticking.
        /// </summary>
        public TableObject Globals { get; }

        /// <summary>
        /// Reads module source for require/dofile. Unset by default; while
        /// null, any require/dofile call raises a Lua error. Set before ticking.
        /// </summary>
        public ModuleReaderDelegate ModuleReader { get; set; }

        /// <summary>
        /// Backing store of package.loaded — require's module cache. Hosts can
        /// preload entries to expose modules without going through the reader.
        /// </summary>
        public TableObject LoadedModules { get; }

        public TickVM()
            : this(null)
        {
        }

        /// <summary>
        /// Builds an empty VM: standard library registered, nothing loaded. Set
        /// up <see cref="Globals"/> and <see cref="ModuleReader"/> as needed,
        /// then call <see cref="Load"/> to start a chunk.
        /// </summary>
        public TickVM(TickVMOptions options)
        {
            if (options?.MaxCallStackDepth is int depth && depth < 1)
                throw new ArgumentOutOfRangeException(nameof(options),
                    "MaxCallStackDepth must be at least 1 (null for unlimited)");
            maxCallStackDepth = options?.MaxCallStackDepth ?? int.MaxValue;

            if (options?.MaxMemoryBytes is long max_memory)
            {
                if (max_memory < 1)
                    throw new ArgumentOutOfRangeException(nameof(options),
                        "MaxMemoryBytes must be at least 1 (null for unlimited)");
                ledger = new MemoryLedger(this, max_memory);
            }

            instructionHandlers = LoadInstructionSet();
            currentCoroutine = idleCoroutine;

            Globals = new TableObject();
            LoadedModules = new TableObject();
            StdLib.Register(Globals, LoadedModules);
        }

        /// <summary>
        /// Starts the VM's main chunk, running it as a call like any other (see
        /// <see cref="StartFunction"/>): it executes on subsequent
        /// <see cref="Tick"/> calls and reports through the returned
        /// <see cref="LuaCall"/>. <see cref="ExecutionResult"/> is a shortcut to
        /// that handle's <see cref="LuaCall.Result"/>.
        ///
        /// <para>Host-supplied <paramref name="args"/> become the chunk's
        /// varargs — scripts bind them with <c>local print, add = ...</c> — and
        /// per the Lua spec are also published in the global <c>arg</c> table at
        /// indices 1..n.</para>
        ///
        /// <para>A VM loads exactly one chunk: a second call throws, since two
        /// chunks would fight over <see cref="Globals"/> and <c>arg</c>. Run
        /// further entry points with <see cref="StartFunction"/> instead.</para>
        /// </summary>
        public LuaCall Load(LuaFunction bytecode, params LuaObject[] args)
        {
            if (bytecode == null)
                throw new ArgumentNullException(nameof(bytecode));
            if (MainCall != null)
                throw new InvalidOperationException(
                    "This VM has already loaded a chunk; use StartFunction to call into it");

            if (bytecode.CompilerVersion != LuaFunction.CurrentCompilerVersion)
                throw new Serialization.BytecodeFormatException(
                    $"Bytecode compiler version {bytecode.CompilerVersion} is not compatible with this VM " +
                    $"(expected {LuaFunction.CurrentCompilerVersion})");

            RootFunction = bytecode;

            var arg_table = new TableObject();
            var call_args = NormalizeArgs(args);
            for (int i = 0; i < call_args.Length; i++)
                arg_table[i + 1] = call_args[i];
            Globals["arg"] = arg_table;

            // The chunk is compiled with HasVarargs and no parameters, so
            // BuildArgsFrame routes every argument into its varargs.
            MainCall = CreateCall(ClosureInGlobalEnv(bytecode));
            BeginCall(MainCall, call_args);
            return MainCall;
        }

        /// <summary>
        /// Wraps a compiled function as a closure over the globals table, which
        /// chunks reference as _ENV (upvalue #0). Shared by <see cref="Load"/>
        /// and the require/dofile loader.
        /// </summary>
        internal ClosureObject ClosureInGlobalEnv(LuaFunction function)
        {
            var upvalues = new RegisterCell[] { new RegisterCell { Value = Globals } };
            return new ClosureObject(function, upvalues);
        }

        /// <summary>
        /// Creates the handle and coroutine for a call without running anything.
        /// Separate from <see cref="BeginCall"/> so callers hold the handle
        /// before execution can throw — a native body runs synchronously and
        /// its error would otherwise escape with the handle never handed back.
        /// </summary>
        private LuaCall CreateCall(LuaObject callee)
        {
            var co = new CoroutineObject(callee);
            var call = new LuaCall(this, co);
            co.HostCall = call;
            liveCoroutines.Add(co);
            return call;
        }

        /// <summary>
        /// Starts a created call — the one path both <see cref="Load"/> and
        /// <see cref="StartFunction"/> run through. Callers validate that the
        /// callee is startable.
        /// </summary>
        private void BeginCall(LuaCall call, LuaObject[] args)
        {
            // Same ledger handoff as Tick: a native body runs synchronously
            // right here, and the root frame is charged on push.
            var previous_ledger = MemoryLedger.Current;
            MemoryLedger.Current = ledger;
            try
            {
                // Null error sink = wrap semantics: an unhandled body error
                // unwinds into the resumer's stack, so with nothing else running
                // it reaches the host as a rethrow out of Tick. The handle learns
                // of the outcome through co.HostCall either way.
                ResumeCoroutine(call.Coroutine, args, ResultsSink.Discard, null);
            }
            finally
            {
                MemoryLedger.Current = previous_ledger;
            }
        }

        private InstructionHandler[] LoadInstructionSet()
        {
            // We create an array of instruction handlers for fast lookup by opcode.
            // It's about 100x faster than using a dictionary.

            int array_size = Enum.GetValues(typeof(Opcode)).Cast<int>().Max() + 1;
            var instructionHandlers = new InstructionHandler[array_size];
            foreach (var kvp in instructionSet)
            {
                instructionHandlers[(int)kvp.Key] = kvp.Value;
            }
            return instructionHandlers;
        }

        public void Tick()
        {
            // Hand this VM's ledger to the write choke points for the duration
            // of the instruction (see MemoryLedger.Current); save/restore so a
            // native that ticks another VM cannot leave a stale ledger behind.
            var previous_ledger = MemoryLedger.Current;
            MemoryLedger.Current = ledger;
            try
            {
                // Before the IsFinished check, not after: a paused host call
                // leaves main's stack empty, so the VM already reports finished
                // and an early return would strand the queued op forever.
                DrainPendingHostOps();

                if (IsFinished) return;

                var frame = CallStack.Peek();
                Execute(frame, frame.Step());

                if (ledger != null)
                    ledger.EnforceLimit();
            }
            catch (RuntimeException ex)
            {
                if (!TryHandleRuntimeError(ex))
                    throw;
            }
            finally
            {
                MemoryLedger.Current = previous_ledger;
            }
        }

        /// <summary>
        /// Applies the resumes and cancels the host asked for since the last
        /// tick (see <see cref="LuaCall.Resume"/> / <see cref="LuaCall.Cancel"/>).
        /// Running here rather than at request time keeps those methods free of
        /// VM work, so a native can call them mid-tick without re-entering the
        /// interpreter. Draining the whole queue means two calls resumed before
        /// the same tick nest, the second parking the first as its resumer —
        /// exactly what two back-to-back StartFunction calls already do.
        /// </summary>
        private void DrainPendingHostOps()
        {
            while (pendingHostOps.Count > 0)
            {
                var op = pendingHostOps.Dequeue();
                if (op.IsCancel)
                    CancelHostCall(op.Coroutine);
                else if (op.Coroutine.Status == CoroutineStatus.Suspended)
                    // Suspended is the only resumable state; anything else means
                    // a cancel queued behind the resume already killed it.
                    ResumeCoroutine(op.Coroutine, op.Args, ResultsSink.Discard, null);
            }
        }

        internal void QueueHostResume(CoroutineObject co, LuaObject[] args)
        {
            pendingHostOps.Enqueue(new PendingHostOp
            {
                Coroutine = co,
                Args = NormalizeArgs(args),
            });
        }

        internal void QueueHostCancel(CoroutineObject co)
        {
            pendingHostOps.Enqueue(new PendingHostOp { Coroutine = co, IsCancel = true });
        }

        /// <summary>
        /// Kills a host-started call. A cancelled call takes with it every
        /// coroutine it resumed: tearing down only the call itself would leave a
        /// live child pointing at a dead resumer, which would corrupt the switch
        /// back when the child returns.
        /// </summary>
        private void CancelHostCall(CoroutineObject co)
        {
            if (co.Status == CoroutineStatus.Dead)
                return;

            bool on_active_chain = false;
            for (var c = currentCoroutine; c != null; c = c.Resumer)
            {
                if (c == co)
                {
                    on_active_chain = true;
                    break;
                }
            }

            if (!on_active_chain)
            {
                // Suspended and not current — the ordinary paused case. A
                // coroutine that yielded has no running child, so there is no
                // chain to unwind and nobody to hand control back to.
                DiscardCoroutineStack(co);
                co.Status = CoroutineStatus.Dead;
                return;
            }

            // Unwind the descendants co resumed, innermost first.
            while (currentCoroutine != co)
            {
                var child = currentCoroutine;
                DiscardCoroutineStack(child);
                child.Status = CoroutineStatus.Dead;
                currentCoroutine = child.Resumer;
            }

            DiscardCoroutineStack(co);
            SwitchBack(co, CoroutineStatus.Dead);
        }

        /// <summary>
        /// Drops a coroutine's frames, refunding what the ledger charged for them.
        /// </summary>
        private void DiscardCoroutineStack(CoroutineObject co)
        {
            if (ledger != null)
                foreach (var f in co.Stack)
                    ledger.ChargeFrame(f, -1);
            co.Stack.Clear();
        }

        /// <summary>
        /// Copies host-supplied arguments, substituting nil for nulls so the VM
        /// never sees a null register value.
        /// </summary>
        private static LuaObject[] NormalizeArgs(LuaObject[] args)
        {
            if (args == null || args.Length == 0)
                return LuaObject.NoResults;

            var copy = new LuaObject[args.Length];
            for (int i = 0; i < args.Length; i++)
                copy[i] = args[i] ?? NilObject.Nil;
            return copy;
        }

        /// <summary>
        /// Starts a call to a function held in the globals table, letting the
        /// host invoke script-defined entry points (an init or update
        /// callback, say) after the main chunk has set them up. The function
        /// runs as a fresh coroutine — with the same _ENV the main chunk saw —
        /// on subsequent <see cref="Tick"/> calls; tick until the returned
        /// handle reports <see cref="LuaCall.IsFinished"/>.
        ///
        /// <para>The <see cref="LuaCall"/> is the call's deferred result: poll it
        /// each tick for the body's return values, for values it yielded (which
        /// pause the call until <see cref="LuaCall.Resume"/>), or for an error.
        /// An unhandled error also propagates out of <see cref="Tick"/> as a
        /// <see cref="RuntimeException"/>, like an error in the main chunk, so
        /// hosts that ignore the handle behave as before.</para>
        ///
        /// <para>A plain native body runs synchronously, so its call is already
        /// finished by the time this returns. Starting a call while the VM is
        /// mid-execution parks the current execution, which continues after the
        /// started call completes.</para>
        /// </summary>
        public LuaCall StartFunction(string globalName, params LuaObject[] args)
        {
            LuaCall call;
            if (!TryStartFunction(globalName, out call, args))
                throw new ArgumentException(
                    $"global '{globalName}' is not a startable function (got {NativeArgs.TypeName(Globals[globalName])})",
                    nameof(globalName));
            return call;
        }

        /// <summary>
        /// Like <see cref="StartFunction"/>, but returns false instead of
        /// throwing when the global is missing or not a startable function —
        /// for optional entry points the script may or may not define. Use the
        /// <see cref="TryStartFunction(string, out LuaCall, LuaObject[])"/>
        /// overload to also get the call's handle.
        /// </summary>
        public bool TryStartFunction(string globalName, params LuaObject[] args)
        {
            LuaCall call;
            return TryStartFunction(globalName, out call, args);
        }

        /// <summary>
        /// Like <see cref="TryStartFunction(string, LuaObject[])"/>, but also
        /// hands back the started call's handle; <paramref name="call"/> is null
        /// when the global is missing or not startable.
        /// </summary>
        public bool TryStartFunction(string globalName, out LuaCall call, params LuaObject[] args)
        {
            if (globalName == null)
                throw new ArgumentNullException(nameof(globalName));

            call = null;

            // Same startability rule as coroutine.create: a closure, or a
            // plain native. VM-aware natives (pcall and friends) read fixed
            // caller registers and cannot run on a fresh stack.
            var callee = Globals[globalName];
            bool startable = callee is ClosureObject
                || (callee is NativeFunctionObject native && native.VmFunction == null);
            if (!startable)
                return false;

            call = CreateCall(callee);
            BeginCall(call, NormalizeArgs(args));
            return true;
        }

        /// <summary>
        /// Unwinds to the nearest error boundary and delivers the error value
        /// there. A boundary is either a pcall-protected frame on the current
        /// stack, or the base of a coroutine: a resumed coroutine reports
        /// (false, error value) to its resumer, while a wrapped one dies and
        /// lets the error keep unwinding in the resumer's stack. Returns false
        /// when no boundary exists — the error is unhandled and the main stack
        /// is left intact so the host can inspect it after the rethrow.
        /// </summary>
        private bool TryHandleRuntimeError(RuntimeException ex)
        {
            while (true)
            {
                bool any_protected = false;
                foreach (var f in CallStack)
                {
                    if (f.IsProtected)
                    {
                        any_protected = true;
                        break;
                    }
                }

                if (any_protected)
                {
                    StackFrame boundary;
                    do
                    {
                        boundary = PopFrame();
                    } while (!boundary.IsProtected);

                    // The boundary frame's error sink carries the pcall protocol; the
                    // VM core knows nothing about where or how the error is reported.
                    boundary.ErrorSink(ex.ErrorValue);
                    return true;
                }

                if (currentCoroutine == idleCoroutine)
                {
                    // Error escapes every boundary: record the Lua call stack (still
                    // intact — nothing has been popped) so the host sees where it happened.
                    ex.CaptureTraceback(BuildTraceback());
                    return false;
                }

                var co = currentCoroutine;
                if (co.PendingResumeErrorSink == null)
                    // Wrap mode: the coroutine's frames are about to be discarded,
                    // so capture them now (innermost capture wins on re-capture).
                    ex.CaptureTraceback(BuildTraceback());
                DiscardCoroutineStack(co);
                SwitchBack(co, CoroutineStatus.Dead);

                // A host-started call reports the error through its handle as
                // well; the unwind below still delivers it as usual.
                co.HostCall?.OnFaulted(ex);

                if (co.PendingResumeErrorSink != null)
                {
                    // Resume mode: resume itself is a protected call — deliver
                    // (false, err) into the resumer and stop unwinding.
                    co.PendingResumeErrorSink(ex.ErrorValue);
                    return true;
                }
                // Wrap mode: keep unwinding in the resumer's stack.
            }
        }

        /// <summary>
        /// Makes a suspended coroutine current so subsequent ticks execute it.
        /// The first resume builds the body's root frame from <paramref name="args"/>;
        /// later resumes deliver them as the parked yield's results. The sinks
        /// say where yielded/returned values and body errors go — a null
        /// <paramref name="errorSink"/> (wrap semantics) lets errors propagate
        /// into the resumer's stack instead. Callers validate status.
        /// </summary>
        internal void ResumeCoroutine(CoroutineObject co, LuaObject[] args,
            ResultsSinkDelegate resultsSink, ErrorSinkDelegate errorSink)
        {
            co.PendingResumeSink = resultsSink;
            co.PendingResumeErrorSink = errorSink;
            co.Resumer = currentCoroutine;
            currentCoroutine.Status = CoroutineStatus.Normal;
            co.Status = CoroutineStatus.Running;

            if (!co.Started)
            {
                co.Started = true;
                currentCoroutine = co;
                if (co.Body is ClosureObject closure)
                {
                    // The root sink reads PendingResumeSink at delivery time, so a
                    // later resume from a different caller redirects the results.
                    // PushFrame targets the current coroutine's stack (co, as of
                    // above) and enforces the depth limit; an overflow here
                    // unwinds like a body error — resume reports (false, err).
                    var root = HandlersCore.BuildArgsFrame(closure, args,
                        results => FinishCoroutine(co, results));
                    PushFrame(root);
                }
                else
                {
                    // Plain native body: runs synchronously within this tick.
                    var native = (NativeFunctionObject)co.Body;
                    try
                    {
                        var results = native.Function(new NativeArgs(args, native.Name)) ?? LuaObject.NoResults;
                        FinishCoroutine(co, results);
                    }
                    catch (RuntimeException ex)
                    {
                        SwitchBack(co, CoroutineStatus.Dead);
                        co.HostCall?.OnFaulted(ex);
                        if (co.PendingResumeErrorSink != null)
                            co.PendingResumeErrorSink(ex.ErrorValue);
                        else
                            // Wrap mode: rethrow inside Tick's try, now in the
                            // resumer's context, so its own protection applies.
                            throw;
                    }
                }
            }
            else
            {
                co.PendingYieldSink(args);
                currentCoroutine = co;
            }
        }

        /// <summary>
        /// Suspends the current coroutine: parks <paramref name="resumeArgsSink"/>
        /// (where the next resume's arguments land — the yield call site's result
        /// registers), switches back to the resumer, and delivers the yielded
        /// values there. The coroutine's frames stay intact for the next resume.
        /// </summary>
        internal void YieldCurrent(LuaObject[] values, ResultsSinkDelegate resumeArgsSink)
        {
            var co = currentCoroutine;
            if (co.Resumer == null)
                // Only the idle root has no resumer, and no script can run
                // there — this is a structural safety net, not a language rule.
                // Every call, the main chunk included, is free to yield.
                throw new RuntimeException("attempt to yield from outside a coroutine");

            co.PendingYieldSink = resumeArgsSink;
            SwitchBack(co, CoroutineStatus.Suspended);
            co.HostCall?.OnYielded(values);
            co.PendingResumeSink(values);
        }

        /// <summary>
        /// The body returned: the root frame is already popped (RETURN did it),
        /// so just die and deliver the results to the resumer.
        /// </summary>
        private void FinishCoroutine(CoroutineObject co, LuaObject[] results)
        {
            SwitchBack(co, CoroutineStatus.Dead);
            co.HostCall?.OnFinished(results);
            co.PendingResumeSink(results);
        }

        private void SwitchBack(CoroutineObject co, CoroutineStatus newStatus)
        {
            co.Status = newStatus;
            currentCoroutine = co.Resumer;
            currentCoroutine.Status = CoroutineStatus.Running;
        }

        /// <summary>
        /// Snapshots the Lua functions currently on the call stack, innermost first,
        /// each tagged with the source line it was executing. Native functions push no
        /// frame, so only Lua frames appear.
        /// </summary>
        private RuntimeException.TracebackFrame[] BuildTraceback()
        {
            var frames = new RuntimeException.TracebackFrame[CallStack.Count];
            int i = 0;
            foreach (var frame in CallStack)
                frames[i++] = new RuntimeException.TracebackFrame(frame.Function.Name, LineOf(frame));
            return frames;
        }

        /// <summary>
        /// The source line a frame is stopped on. Step() advances PC past an
        /// instruction before executing it, so the in-progress instruction — the one
        /// that threw, or the pending CALL for a caller frame — is at PC - 1.
        /// </summary>
        private static int LineOf(StackFrame frame)
        {
            var lines = frame.Function.Meta?.Lines;
            int index = frame.PC - 1;
            if (lines == null || index < 0 || index >= lines.Count)
                return 0;
            return lines[index];
        }

        private void Execute(StackFrame frame, Instruction instruction)
        {
            int opcode = (int)instruction.Opcode;
            instructionHandlers[opcode](this, frame, instruction);
        }

        internal void PushFrame(StackFrame frame)
        {
            // Depth is checked per coroutine: CallStack is the active
            // coroutine's stack, and each coroutine gets the full budget.
            if (CallStack.Count >= maxCallStackDepth)
                throw new RuntimeException("stack overflow");
            ledger?.ChargeFrame(frame, +1);
            CallStack.Push(frame);
        }

        internal StackFrame PopFrame()
        {
            var frame = CallStack.Pop();
            ledger?.ChargeFrame(frame, -1);
            return frame;
        }

        /// <summary>
        /// Every live call's coroutine, for the memory ledger's correction scan.
        /// Dead entries are dropped as it goes: a finished or cancelled call
        /// retains nothing.
        /// </summary>
        internal List<CoroutineObject> CollectLiveCoroutines()
        {
            liveCoroutines.RemoveAll(co => co.Status == CoroutineStatus.Dead);
            return liveCoroutines;
        }
    }
}