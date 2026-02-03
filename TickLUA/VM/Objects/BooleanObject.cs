namespace TickLUA.VM.Objects
{
    public class BooleanObject : LuaObject
    {
        public static BooleanObject True { get; } = new BooleanObject();
        public static BooleanObject False { get; } = new BooleanObject();

        private BooleanObject()
        {
        }

        public static BooleanObject FromBool(bool value) => value ? True : False;

        public override string ToString()
        {
            return (bool)this? "true" : "false";
        }

        public override BooleanObject ToBooleanObject() => this;
        public override StringObject ToStringObject() => new StringObject(ToString());

        public static BooleanObject operator !(BooleanObject o) => (bool)o? False : True;

        public static explicit operator NumberObject(BooleanObject value) => new NumberObject((bool)value ? 1 : 0);

        public static explicit operator bool(BooleanObject value) => value == True;
    }
}
