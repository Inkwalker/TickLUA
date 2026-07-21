using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// Arguments passed to a <see cref="NativeFunction"/>.
    /// Indices are 0-based (C# style); error messages report the Lua-style
    /// 1-based position, i.e. index 0 is reported as "bad argument #1".
    /// </summary>
    public readonly struct NativeArgs
    {
        private readonly LuaObject[] args;
        private readonly string functionName;

        /// <summary>Host / unit-test construction. The array is not copied.</summary>
        public NativeArgs(LuaObject[] args) : this(args, null)
        {
        }

        internal NativeArgs(LuaObject[] args, string functionName)
        {
            this.args = args;
            this.functionName = functionName;
        }

        public int Count => args == null ? 0 : args.Length;

        /// <summary>Function name used in error messages, "?" when unnamed.</summary>
        public string FunctionName => functionName ?? "?";

        /// <summary>
        /// Raw argument access. Out-of-range indices yield nil (Lua semantics), never null.
        /// </summary>
        public LuaObject this[int index]
        {
            get
            {
                if (args == null || index < 0 || index >= args.Length)
                    return NilObject.Nil;
                return args[index] ?? NilObject.Nil;
            }
        }

        /// <summary>The argument slot does not exist (index outside 0..Count-1).</summary>
        public bool IsNone(int index) => index < 0 || index >= Count;

        /// <summary>The argument is present and is nil.</summary>
        public bool IsNil(int index) => !IsNone(index) && NullOrNil(args[index]);

        public bool IsNilOrNone(int index) => IsNone(index) || NullOrNil(args[index]);

        public bool IsBoolean(int index) => this[index] is BooleanObject;

        public bool IsNumber(int index) => this[index] is NumberObject;

        /// <summary>The argument is a number without a fractional part.</summary>
        public bool IsInteger(int index) => this[index] is NumberObject num && num.IsInteger;

        public bool IsString(int index) => this[index] is StringObject;

        public bool IsTable(int index) => this[index] is TableObject;

        public bool IsFunction(int index)
        {
            var value = this[index];
            return value is ClosureObject || value is NativeFunctionObject;
        }

        /// <summary>Any present argument (nil included); errors when the slot is absent.</summary>
        public LuaObject CheckAny(int index)
        {
            if (IsNone(index))
                throw BadArgument(index, "value");
            return this[index];
        }

        public bool CheckBoolean(int index)
        {
            if (this[index] is BooleanObject value)
                return (bool)value;
            throw BadArgument(index, "boolean");
        }

        public float CheckNumber(int index)
        {
            if (this[index] is NumberObject value)
                return value.Value;
            throw BadArgument(index, "number");
        }

        public int CheckInteger(int index)
        {
            if (this[index] is NumberObject value)
            {
                if (value.IsInteger)
                    return (int)value.Value;
                throw new RuntimeException(
                    $"bad argument #{index + 1} to '{FunctionName}' (number has no integer representation)");
            }
            throw BadArgument(index, "number");
        }

        public string CheckString(int index)
        {
            if (this[index] is StringObject value)
                return value.Value;
            throw BadArgument(index, "string");
        }

        public TableObject CheckTable(int index)
        {
            if (this[index] is TableObject value)
                return value;
            throw BadArgument(index, "table");
        }

        public bool OptBoolean(int index, bool def) => IsNilOrNone(index) ? def : CheckBoolean(index);

        public float OptNumber(int index, float def) => IsNilOrNone(index) ? def : CheckNumber(index);

        public int OptInteger(int index, int def) => IsNilOrNone(index) ? def : CheckInteger(index);

        public string OptString(int index, string def) => IsNilOrNone(index) ? def : CheckString(index);

        public TableObject OptTable(int index, TableObject def) => IsNilOrNone(index) ? def : CheckTable(index);

        /// <summary>Defensive copy of the raw arguments (absent slots normalized to nil).</summary>
        public LuaObject[] ToArray()
        {
            var copy = new LuaObject[Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = this[i];
            return copy;
        }

        /// <summary>
        /// Lua type name of a value, as used in error messages — each type
        /// answers for itself through <see cref="LuaObject.TypeName"/>. A C#
        /// null is an absent value, which Lua calls nil.
        /// </summary>
        public static string TypeName(LuaObject value)
            => value == null ? "nil" : value.TypeName;

        private RuntimeException BadArgument(int index, string expected)
        {
            string got = IsNone(index) ? "no value" : TypeName(args[index]);
            return new RuntimeException(
                $"bad argument #{index + 1} to '{FunctionName}' ({expected} expected, got {got})");
        }

        private static bool NullOrNil(LuaObject value) => value == null || value is NilObject;
    }
}
