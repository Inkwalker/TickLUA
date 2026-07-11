using System;
using System.Collections.Generic;
using TickLUA.VM.Handlers;
using System.Linq;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
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

        private Stack<StackFrame> callStack = new Stack<StackFrame>();

        public bool IsFinished => callStack.Count == 0;
        public LuaObject[] ExecutionResult { get; private set; }

        /// <summary>
        /// The globals table, exposed to scripts as _ENV. Hosts can register
        /// native functions and other globals here before ticking.
        /// </summary>
        public TableObject Globals { get; }

        public TickVM(LuaFunction bytecode, params LuaObject[] args)
        {
            instructionHandlers = LoadInstructionSet();

            // The main chunk is compiled with _ENV as upvalue #0; its cell holds
            // the globals table.
            Globals = new TableObject();
            StdLib.Register(Globals);
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

            var frame = callStack.Peek();
            var instruction = frame.Step();

            try
            {
                Execute(frame, instruction);
            }
            catch (RuntimeException ex)
            {
                if (!TryHandleRuntimeError(ex))
                    throw;
            }
        }

        /// <summary>
        /// Unwinds the call stack to the nearest pcall boundary and delivers
        /// (false, error value) there. Returns false when no protected frame
        /// exists — the error is unhandled and the call stack is left intact
        /// so the host can inspect it after the rethrow.
        /// </summary>
        private bool TryHandleRuntimeError(RuntimeException ex)
        {
            bool any_protected = false;
            foreach (var f in callStack)
            {
                if (f.IsProtected)
                {
                    any_protected = true;
                    break;
                }
            }
            if (!any_protected)
            {
                // Error escapes every pcall boundary: record the Lua call stack (still
                // intact — nothing has been popped) so the host sees where it happened.
                ex.CaptureTraceback(BuildTraceback());
                return false;
            }

            StackFrame boundary;
            do
            {
                boundary = callStack.Pop();
            } while (!boundary.IsProtected);

            // The boundary frame's error sink carries the pcall protocol; the
            // VM core knows nothing about where or how the error is reported.
            boundary.ErrorSink(ex.ErrorValue);
            return true;
        }

        /// <summary>
        /// Snapshots the Lua functions currently on the call stack, innermost first,
        /// each tagged with the source line it was executing. Native functions push no
        /// frame, so only Lua frames appear.
        /// </summary>
        private RuntimeException.TracebackFrame[] BuildTraceback()
        {
            var frames = new RuntimeException.TracebackFrame[callStack.Count];
            int i = 0;
            foreach (var frame in callStack)
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
            callStack.Push(frame);
        }

        internal StackFrame PopFrame()
        {
            return callStack.Pop();
        }

        internal void SetExecutionResult(params LuaObject[] result)
        {
            ExecutionResult = result;
        }
    }
}