using System.Collections.Generic;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    public class LuaFunction
    {
        public List<uint> Instructions { get; } = new List<uint>();
        public List<LuaObject> Constants { get; } = new List<LuaObject>();
        public List<LuaFunction> NestedFunctions { get; } = new List<LuaFunction>();
        public int RegisterCount { get; set; }
    }
}
