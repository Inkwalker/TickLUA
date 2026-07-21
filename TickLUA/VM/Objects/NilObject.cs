namespace TickLUA.VM.Objects
{
    public sealed class NilObject : LuaObject
    {
        public static NilObject Nil { get; } = new NilObject();

        public override string ToString()
        {
            return "< Nil >";
        }

        public override string TypeName => "nil";

        public override BooleanObject ToBooleanObject() => BooleanObject.False;

        /// <summary>
        /// "nil", not a bracketed debug form: this is what tostring(nil) hands
        /// to a script, and Lua spells it that way.
        /// </summary>
        public override StringObject ToStringObject() => new StringObject("nil");

        // Singleton.
        public override long ShallowMemoryCost() => 0;

        public static implicit operator BooleanObject(NilObject nil) => BooleanObject.False;
    }
}
