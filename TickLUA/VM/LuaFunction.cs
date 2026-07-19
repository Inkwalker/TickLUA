using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    public class LuaFunction
    {
        /// <summary>
        /// Version of the bytecode format this compiler/VM pair produces and
        /// understands. Bump on any change to the instruction encoding, opcode
        /// set, constant serialization, or function layout.
        /// </summary>
        public const ushort CurrentCompilerVersion = 2;

        /// <summary>
        /// Version of the compiler that produced this bytecode. Freshly
        /// compiled functions carry <see cref="CurrentCompilerVersion"/>;
        /// deserialized ones carry the version stored in the stream. The VM
        /// refuses to execute bytecode whose version does not match its own.
        /// </summary>
        public ushort CompilerVersion { get; internal set; } = CurrentCompilerVersion;

        public string Name { get; private set; }
        internal List<Instruction> Instructions { get; }
        internal List<LuaObject> Constants { get; }
        internal int RegisterCount { get; set; }
        internal bool HasVarargs { get; set; }
        internal int ParameterCount { get; set; }
        internal List<UpvalueDef> Upvalues { get; }
        public List<LuaFunction> NestedFunctions { get; } = new List<LuaFunction>();
        public Metadata Meta { get; }
        public int InstructionCount => Instructions.Count;

        /// <summary>
        /// Whether <see cref="Metadata.Locals"/> carries local-variable debug
        /// info. Always true for freshly compiled functions; false only on
        /// functions deserialized from a stream written with
        /// <c>stripDebugInfo</c>. An empty Locals list alone is ambiguous (a
        /// function may simply declare no locals), so debuggability is tracked
        /// explicitly. Line info (<see cref="Metadata.Lines"/>) survives
        /// stripping — tracebacks keep real line numbers either way.
        /// </summary>
        public bool HasDebugInfo { get; internal set; } = true;

        internal LuaFunction(
            string name,
            IEnumerable<Instruction> instructions,
            IEnumerable<LuaObject> constants,
            IEnumerable<UpvalueDef> upvalues,
            Metadata meta,
            int registerCount
        )
        {
            Name = name;
            Instructions = new List<Instruction>(instructions);
            Constants = new List<LuaObject>(constants);
            Upvalues = new List<UpvalueDef>(upvalues);

            RegisterCount = registerCount;

            Meta = meta;
        }

        internal LuaFunction(string name, int registerCount)
        {
            Name = name;
            Instructions = new List<Instruction>();
            Constants = new List<LuaObject>();
            Upvalues = new List<UpvalueDef>();

            RegisterCount = registerCount;

            Meta = new Metadata();
        }

        public class Metadata
        {
            public List<ushort> Lines { get; } = new List<ushort>();

            /// <summary>
            /// Named locals with the register each occupies and the instruction
            /// range where it is visible, in declaration order. Shadowing: for
            /// a name matching several entries at a PC, the last entry wins.
            /// Empty when <see cref="LuaFunction.HasDebugInfo"/> is false.
            /// </summary>
            public List<LocalVarInfo> Locals { get; } = new List<LocalVarInfo>();

            /// <summary>
            /// A named local's lifetime: visible (and resolvable by the
            /// compiler) at instruction pc iff StartPC &lt;= pc &lt; EndPC.
            /// StartPC is the naming point — for "local x = expr" that is
            /// before the initializer's code, matching this compiler's
            /// resolution semantics.
            /// </summary>
            public class LocalVarInfo
            {
                public string Name { get; }
                public byte Register { get; }
                public int StartPC { get; }
                public int EndPC { get; internal set; }

                public LocalVarInfo(string name, byte register, int startPC, int endPC)
                {
                    Name = name;
                    Register = register;
                    StartPC = startPC;
                    EndPC = endPC;
                }
            }
        }

        internal class UpvalueDef
        {
            public string Name { get; set; }

            /// <summary>
            /// Is defined in the parent function as a local variable?
            /// </summary>
            public bool IsLocalToParent { get; set; }
            public byte Index { get; set; }

            public UpvalueDef(string name, bool isLocal, byte index)
            {
                Name = name;
                IsLocalToParent = isLocal;
                Index = index;
            }

            public UpvalueDef(bool isLocal, byte index)
            {
                Name = null;
                IsLocalToParent = isLocal;
                Index = index;
            }
        }
    }
}
