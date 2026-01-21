namespace TickLUA.VM.Objects
{
    /// <summary>
    /// Base class for all TickLUA data types
    /// </summary>
    public abstract class LuaObject
    {
        #region Metatable constants
        public const string ENV = "_env";
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
        #endregion

        //public TableObject Metatable { get; set; }

        public LuaObject() { }

        public override string ToString()
        {
            return $"< object >";
        }

        public virtual BooleanObject ToBooleanObject() => BooleanObject.True;

        //public abstract StringObject ToStringObject();

        public static bool NullOrNil(LuaObject obj) => obj == null || obj == NilObject.Nil;


    }
}
