using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    public class LuaFunction
    {
        internal List<Instruction> Instructions { get; }
        internal List<LuaObject> Constants { get; }
        internal int RegisterCount { get; set; }
        internal List<UpvalueDef> Upvalues { get; }
        public List<LuaFunction> NestedFunctions { get; } = new List<LuaFunction>();
        public Metadata Meta { get; }

        internal LuaFunction(
            IEnumerable<Instruction> instructions,
            IEnumerable<LuaObject> constants,
            IEnumerable<UpvalueDef> upvalues,
            Metadata meta,
            int registerCount
        )
        {
            Instructions = new List<Instruction>(instructions);
            Constants = new List<LuaObject>(constants);
            Upvalues = new List<UpvalueDef>(upvalues);

            RegisterCount = registerCount;

            Meta = meta;
        }

        internal LuaFunction(int registerCount)
        {
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
            public bool IsLocal { get; set; }
            public byte Index { get; set; }

            public UpvalueDef(string name, bool isLocal, byte index)
            {
                Name = name;
                IsLocal = isLocal;
                Index = index;
            }

            public UpvalueDef(bool isLocal, byte index)
            {
                Name = null;
                IsLocal = isLocal;
                Index = index;
            }
        }
    }
}
