using System;
using System.Globalization;

namespace TickLUA.VM.Objects
{
    public sealed class NumberObject : LuaObject
    {
        public static NumberObject Zero { get; } = new NumberObject(0);

        public float Value { get; }

        public bool IsInteger => Value == (int)Value;

        public NumberObject(int value)
        {
            Value = value;
        }

        public NumberObject(float value)
        {
            Value = value;
        }

        public override string TypeName => "number";

        public override bool Equals(object obj)
        {
            if (obj is NumberObject number)
            {
                return Value == number.Value;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Always invariant: number-to-string is script-visible, and the host's
        /// regional settings must not change what a script computes. A comma
        /// decimal separator would also be rejected by <see cref="TryParse"/>,
        /// which is invariant, breaking the round trip on the same machine.
        /// </summary>
        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override StringObject ToStringObject()
            => new StringObject(Value.ToString(CultureInfo.InvariantCulture));

        // Fixed-size and bounded by the slots that hold it.
        public override long ShallowMemoryCost() => 0;

        /// <summary>
        /// Canonical Lua string-to-number conversion, shared by numeric literals
        /// and by string coercion in arithmetic. Accepts decimal integers/floats
        /// (with optional sign, exponent, and surrounding whitespace) and hex
        /// integers/floats (0x…, with optional binary 'p' exponent). Returns
        /// false for anything that is not a well-formed Lua numeral.
        /// </summary>
        public static bool TryParse(string s, out NumberObject result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(s))
                return false;

            string trimmed = s.Trim();

            // Hex numerals carry an optional sign outside the 0x prefix.
            int sign = 1;
            string body = trimmed;
            if (body.StartsWith("+"))
                body = body.Substring(1);
            else if (body.StartsWith("-"))
            {
                sign = -1;
                body = body.Substring(1);
            }

            if (body.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    result = new NumberObject((float)(sign * ParseLuaHexFloat(body)));
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
            {
                result = new NumberObject(f);
                return true;
            }

            return false;
        }

        private static double ParseLuaHexFloat(string s)
        {
            // split "0x1.8p10"
            int p = s.IndexOf('p');
            string mantissaPart = p > -1 ? s.Substring(2, p - 2) : s;
            int exponent = p > -1 ? int.Parse(s.Substring(p + 1), CultureInfo.InvariantCulture) : 0;

            double mantissa = 0.0;

            // split integer and fractional hex parts
            var parts = mantissaPart.Split('.');
            mantissa += Convert.ToInt64(parts[0], 16);

            if (parts.Length == 2)
            {
                double frac = 0;
                double scale = 1.0 / 16;
                foreach (char c in parts[1])
                {
                    frac += HexDigit(c) * scale;
                    scale /= 16;
                }
                mantissa += frac;
            }

            return mantissa * Math.Pow(2, exponent);
        }

        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;
            throw new FormatException("Invalid hex digit: " + c);
        }

        public static NumberObject IntDiv(NumberObject l, NumberObject r) => new NumberObject((float)Math.Floor(l.Value / r.Value));
        public static NumberObject Pow(NumberObject l, NumberObject r) => new NumberObject((float)Math.Pow(l.Value, r.Value));

        public static NumberObject operator +(NumberObject l, NumberObject r) => new NumberObject(l.Value + r.Value);
        public static NumberObject operator -(NumberObject l, NumberObject r) => new NumberObject(l.Value - r.Value);
        public static NumberObject operator *(NumberObject l, NumberObject r) => new NumberObject(l.Value * r.Value);

        public static NumberObject operator /(NumberObject l, NumberObject r) => new NumberObject(l.Value / r.Value);

        public static NumberObject operator %(NumberObject l, NumberObject r) => new NumberObject(l.Value % r.Value);
        public static NumberObject operator -(NumberObject n) => new NumberObject(-n.Value);

        public static bool operator >(NumberObject l, NumberObject r) => l.Value > r.Value;
        public static bool operator >=(NumberObject l, NumberObject r) => l.Value >= r.Value;
        public static bool operator <(NumberObject l, NumberObject r) => l.Value < r.Value;
        public static bool operator <=(NumberObject l, NumberObject r) => l.Value <= r.Value;
        public static bool operator ==(NumberObject l, LuaObject r)
        {
            if (r is NumberObject nr)
                return l.Value == nr.Value;
            else
                return ReferenceEquals(l, r);
        }
        public static bool operator !=(NumberObject l, LuaObject r)
        {
            if (r is NumberObject nr)
                return l.Value != nr.Value;
            else
                return !ReferenceEquals(l, r);
        }

        public static implicit operator BooleanObject(NumberObject num) => BooleanObject.True;

        public static explicit operator int(NumberObject num)    => (int)num.Value;
        public static explicit operator double(NumberObject num) => (double)num.Value;
        public static explicit operator float(NumberObject num)  => (float)num.Value;
    }
}
