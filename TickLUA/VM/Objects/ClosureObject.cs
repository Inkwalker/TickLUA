namespace TickLUA.VM.Objects
{
    internal class ClosureObject : LuaObject
    {
        public LuaFunction Function { get; set; }
        public RegisterCell[] Upvalues { get; set; }

        public ClosureObject(LuaFunction function)
        {
            Function = function;
            Upvalues = new RegisterCell[0];
        }

        public ClosureObject(LuaFunction function, RegisterCell[] upvalues)
        {
            Function = function;
            Upvalues = upvalues;
        }

        public override StringObject ToStringObject() => new StringObject("[func]");
    }
}
