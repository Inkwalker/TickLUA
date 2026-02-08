using System;
using System.Collections.Generic;
using System.Text;

namespace TickLUA.VM.Objects
{
    internal class ClosureObject : LuaObject
    {
        public RegisterCell[] Upvalues { get; set; }

        public ClosureObject(int upvalue_count)
        {
            Upvalues = new RegisterCell[upvalue_count];
        }

        public ClosureObject(RegisterCell[] upvalues)
        {
            Upvalues = upvalues;
        }

        public override StringObject ToStringObject() => new StringObject("[func]");
    }
}
