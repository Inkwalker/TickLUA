namespace TickLUA.VM.Objects
{
    public sealed class NilObject : LuaObject
    {
        public static NilObject Nil { get; } = new NilObject();

        public override string ToString()
        {
            return "< Nil >";
        }

        public override BooleanObject ToBooleanObject() => BooleanObject.False;

        public override StringObject ToStringObject() => new StringObject("[nil]");

        public static implicit operator BooleanObject(NilObject nil) => BooleanObject.False;
    }
}
