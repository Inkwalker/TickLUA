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

        // Execution always operates on the current coroutine's stack; the main
        // chunk runs on an implicit main coroutine. Resume/yield switch which
        // coroutine is current, and with it the stack every handler sees.
        private readonly CoroutineObject mainCoroutine = new CoroutineObject();
        private CoroutineObject currentCoroutine;

        private Stack<StackFrame> CallStack => currentCoroutine.Stack;

        internal CoroutineObject CurrentCoroutine => currentCoroutine;
        internal CoroutineObject MainCoroutine => mainCoroutine;

        // Main cannot yield, so its stack only empties when the main chunk
        // returns; suspended coroutines left behind are simply abandoned.
        public bool IsFinished => mainCoroutine.Stack.Count == 0;

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

        public LuaObject[] ExecutionResult { get; private set; }

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

        public TickVM(LuaFunction bytecode, params LuaObject[] args)
            : this(bytecode, null, args)
        {
        }

        public TickVM(LuaFunction bytecode, TickVMOptions options, params LuaObject[] args)
        {
            if (bytecode.CompilerVersion != LuaFunction.CurrentCompilerVersion)
                throw new Serialization.BytecodeFormatException(
                    $"Bytecode compiler version {bytecode.CompilerVersion} is not compatible with this VM " +
                    $"(expected {LuaFunction.CurrentCompilerVersion})");

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
            currentCoroutine = mainCoroutine;

            // The main chunk is compiled with _ENV as upvalue #0; its cell holds
            // the globals table.
            Globals = new TableObject();
            LoadedModules = new TableObject();
            StdLib.Register(Globals, LoadedModules);
            var upvalues = new RegisterCell[] { new RegisterCell { Value = Globals } };
            var frame = new StackFrame(bytecode, upvalues);

            // The main chunk has no caller: its return becomes the VM's result.
            frame.Sink = ResultsSink.ToExecutionResult(this);

            // Host-supplied values become the main chunk's varargs; scripts bind
            // them via "local print, add = ..." (the main chunk is compiled with
            // HasVarargs = true and no parameters).
            // Per the Lua spec they are also published in the global 'arg' table
            // at indices 1..n.
            var arg_table = new TableObject();
            if (args != null && args.Length > 0)
            {
                var varargs = new LuaObject[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    varargs[i] = args[i] ?? NilObject.Nil;
                    arg_table[i + 1] = varargs[i];
                }
                frame.Varargs = varargs;
            }
            Globals["arg"] = arg_table;

            PushFrame(frame);
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
            if (IsFinished) return;

            var frame = CallStack.Peek();
            var instruction = frame.Step();

            // Hand this VM's ledger to the write choke points for the duration
            // of the instruction (see MemoryLedger.Current); save/restore so a
            // native that ticks another VM cannot leave a stale ledger behind.
            var previous_ledger = MemoryLedger.Current;
            MemoryLedger.Current = ledger;
            try
            {
                Execute(frame, instruction);

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

                if (currentCoroutine == mainCoroutine)
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
                if (ledger != null)
                    foreach (var f in co.Stack)
                        ledger.ChargeFrame(f, -1);
                co.Stack.Clear();
                SwitchBack(co, CoroutineStatus.Dead);

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
            if (co == mainCoroutine)
                throw new RuntimeException("attempt to yield from outside a coroutine");

            co.PendingYieldSink = resumeArgsSink;
            SwitchBack(co, CoroutineStatus.Suspended);
            co.PendingResumeSink(values);
        }

        /// <summary>
        /// The body returned: the root frame is already popped (RETURN did it),
        /// so just die and deliver the results to the resumer.
        /// </summary>
        private void FinishCoroutine(CoroutineObject co, LuaObject[] results)
        {
            SwitchBack(co, CoroutineStatus.Dead);
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

        internal void SetExecutionResult(params LuaObject[] result)
        {
            ExecutionResult = result;
        }
    }
}