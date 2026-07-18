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
        public const ushort CurrentCompilerVersion = 1;

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
