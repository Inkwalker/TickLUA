using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    public class LuaFunction
    {
        public List<uint> Instructions { get; }
        public List<LuaObject> Constants { get; }
        public List<LuaFunction> NestedFunctions { get; } = new List<LuaFunction>();
        public int RegisterCount { get; set; }

        public LuaFunction(
            IEnumerable<uint> instructions,
            IEnumerable<LuaObject> constants,
            int registerCount
        )
        {
            Instructions = new List<uint>(instructions);
            Constants = new List<LuaObject>(constants);

            RegisterCount = registerCount;
        }
    }
}
