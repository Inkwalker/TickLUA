namespace TickLUA.VM.Objects
{
    internal sealed class ClosureObject : LuaObject
    {
        // Rough x64 memory-accounting costs (see LuaObject.ShallowMemoryCost):
        // the closure object, and one captured upvalue cell (cell + array
        // slot) — cells can outlive their frame, so the closure carries them.
        internal const long HeaderMemoryCost = 64;
        internal const long UpvalueMemoryCost = 32;

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

        public override string TypeName => "function";

        public override StringObject ToStringObject() => new StringObject("[func]");

        // Includes the captured cells, which can outlive their frame; the
        // values inside them bill themselves on write.
        public override long ShallowMemoryCost() =>
            HeaderMemoryCost + UpvalueMemoryCost * Upvalues.Length;
    }
}
