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
            // Function operations
            { Opcode.RETURN,          HandlersCore.RETURN },
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
        };

        private readonly InstructionHandler[] instructionHandlers;

        private Stack<StackFrame> callStack = new Stack<StackFrame>();

        public bool IsFinished => callStack.Count == 0;
        public LuaObject[] ExecutionResult { get; private set; }

        public TickVM(LuaFunction bytecode)
        {
            instructionHandlers = LoadInstructionSet();
            
            CallFunction(bytecode);
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

            Execute(frame, instruction);

            if (frame.IsFinished)
            {
                callStack.Pop();
                ExecutionResult = frame.Results;
            }
        }

        private void Execute(StackFrame frame, Instruction instruction)
        {
            int opcode = (int)instruction.Opcode;
            instructionHandlers[opcode](this, frame, instruction);
        }

        private void CallFunction(LuaFunction function)
        {
            var frame = new StackFrame(function);
            callStack.Push(frame);
        }
    }
}
