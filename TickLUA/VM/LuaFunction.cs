using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    public class LuaFunction
    {
        internal List<Instruction> Instructions { get; }
        internal List<LuaObject> Constants { get; }
        public List<LuaFunction> NestedFunctions { get; } = new List<LuaFunction>();
        internal int RegisterCount { get; set; }

        internal LuaFunction(
            IEnumerable<Instruction> instructions,
            IEnumerable<LuaObject> constants,
            int registerCount
        )
        {
            Instructions = new List<Instruction>(instructions);
            Constants = new List<LuaObject>(constants);

            RegisterCount = registerCount;
        }

        internal LuaFunction(int registerCount)
        {
            Instructions = new List<Instruction>();
            Constants = new List<LuaObject>();

            RegisterCount = registerCount;
        }
    }
}
