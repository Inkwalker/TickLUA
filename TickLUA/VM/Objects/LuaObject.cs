namespace TickLUA.VM.Objects
{
    /// <summary>
    /// Base class for all TickLUA data types
    /// </summary>
    public abstract class LuaObject
    {
        /// <summary>Name of the environment upvalue holding the globals table (Lua 5.2+).</summary>
        public const string ENV = "_ENV";

        #region Metatable constants
        public const string BASE = "__base";
        public const string INIT = "__init";
        public const string INDEX_GET = "__index";
        public const string INDEX_SET = "__newindex";
        public const string CALL = "__call";
        public const string LEN = "__len";
        public const string UNM = "__unm";

        public const string ADD = "__add";
        public const string SUB = "__sub";
        public const string DIV = "__div";
        public const string IDIV = "__idiv";
        public const string MUL = "__mul";
        public const string MOD = "__mod";
        public const string POW = "__pow";

        public const string BAND = "__band";
        public const string BOR = "__bor";
        public const string BXOR = "__bxor";
        public const string BNOT = "__bnot";
        public const string SHL = "__shl";
        public const string SHR = "__shr";

        public const string CONCAT = "__concat";

        public const string LESS = "__lt";
        public const string LESS_EQ = "__le";
        public const string EQUALS = "__eq";

        public const string TOSTRING = "__tostring";

        public const string METATABLE = "__metatable";
        #endregion

        /// <summary>Allocation-free "no results" return value for native functions.</summary>
        public static readonly LuaObject[] NoResults = new LuaObject[0];

        public LuaObject() { }

        public static LuaObject From(bool value) => BooleanObject.FromBool(value);
        public static LuaObject From(int value) => new NumberObject(value);
        public static LuaObject From(float value) => new NumberObject(value);
        public static LuaObject From(string value) => value == null ? (LuaObject)NilObject.Nil : new StringObject(value);

        public override string ToString()
        {
            return $"< object >";
        }

        /// <summary>
        /// The value's Lua type name — what <c>type(v)</c> reports, and what
        /// error messages name it by. The built-in types return the eight
        /// standard names; a host type reports its own C# type name unless it
        /// overrides this, so scripts can tell one host type from another
        /// instead of seeing every one of them as "userdata".
        /// </summary>
        public virtual string TypeName => GetType().Name;

        public virtual BooleanObject ToBooleanObject() => BooleanObject.True;

        public abstract StringObject ToStringObject();

        /// <summary>
        /// Approximate number of bytes this value adds to each slot that
        /// references it, used by the memory limit
        /// (<see cref="TickVMOptions.MaxMemoryBytes"/>). Containers report
        /// only their own header — their contents are billed separately as
        /// they are written. Values that are fixed-size and already bounded
        /// by the slots holding them (numbers, booleans, nil) report 0.
        /// Precision is not required; the estimate only has to scale with
        /// the value's real footprint.
        /// </summary>
        public abstract long ShallowMemoryCost();

        public static bool NullOrNil(LuaObject obj) => obj == null || obj == NilObject.Nil;
    }
}
