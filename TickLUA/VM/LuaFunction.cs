using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    public class LuaFunction
    {
        internal List<Instruction> Instructions { get; }
        internal List<LuaObject> Constants { get; }
        internal int RegisterCount { get; set; }
        public List<LuaFunction> NestedFunctions { get; } = new List<LuaFunction>();

        public Metadata Meta { get; }

        internal LuaFunction(
            IEnumerable<Instruction> instructions,
            IEnumerable<LuaObject> constants,
            Metadata meta,
            int registerCount
        )
        {
            Instructions = new List<Instruction>(instructions);
            Constants = new List<LuaObject>(constants);

            RegisterCount = registerCount;

            Meta = meta;
        }

        internal LuaFunction(int registerCount)
        {
            Instructions = new List<Instruction>();
            Constants = new List<LuaObject>();

            RegisterCount = registerCount;

            Meta = new Metadata();
        }

        public class Metadata
        {
            public List<ushort> Lines { get; } = new List<ushort>();
        }
    }
}
