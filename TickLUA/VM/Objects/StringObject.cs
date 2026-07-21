using System;

namespace TickLUA.VM.Objects
{
    public sealed class StringObject : LuaObject, IHasLen, IIndexable
    {
        // Rough x64 memory-accounting base cost (see LuaObject.ShallowMemoryCost):
        // the wrapper object plus the .NET string header; characters add
        // 2 bytes each (UTF-16).
        internal const long BaseMemoryCost = 48;

        public string Value { get; }

        public LuaObject this[LuaObject index] 
        {
            get
            {
                if (index is NumberObject i && i.Value > 0 && i.Value <= Value.Length)
                {
                    int str_i = (int)i.Value - 1;
                    return new StringObject(Value[str_i]);
                }

                return NilObject.Nil;
            }
            set => throw new RuntimeException("Attempt to modify a string");
        }

        public StringObject(string str)
        {
            Value = str;
        }

        public StringObject(char ch)
        {
            Value = ch.ToString();
        }

        public bool Contains(LuaObject index)
        {
            return index is NumberObject i && i.Value > 0 && i.Value <= Value.Length;
        }

        public NumberObject Len()
        {
            return new NumberObject(Value.Length);
        }

        public override string ToString()
        {
            return $"\"{Value}\"";
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is StringObject))
            {
                return false;
            }

            var str = obj as StringObject;

            return Value.Equals(str.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string TypeName => "string";

        public override StringObject ToStringObject() => this;

        public override long ShallowMemoryCost() => BaseMemoryCost + 2L * Value.Length;

        public static explicit operator string(StringObject str) => str.Value;
        public static StringObject operator +(StringObject l, StringObject r) => new StringObject(l.Value + r.Value);
    }
}
