using System;

namespace TickLUA.VM.Objects
{
    public class NumberObject : LuaObject
    {
        public static NumberObject Zero { get; } = new NumberObject(0);

        public float Value { get; }

        public NumberObject(int value)
        {
            Value = value;
        }

        public NumberObject(float value)
        {
            Value = value;
        }

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

        public override string ToString()
        {
            return Value.ToString();
        }

        //public override StringObject ToStringObject() => new StringObject(Value.ToString());

        public static NumberObject IntDiv(NumberObject l, NumberObject r) => new NumberObject((float)Math.Floor(l.Value / r.Value));
        public static NumberObject Pow(NumberObject l, NumberObject r) => new NumberObject((float)Math.Pow(l.Value, r.Value));

        public static NumberObject operator +(NumberObject l, NumberObject r) => new NumberObject(l.Value + r.Value);
        public static NumberObject operator -(NumberObject l, NumberObject r) => new NumberObject(l.Value - r.Value);
        public static NumberObject operator *(NumberObject l, NumberObject r) => new NumberObject(l.Value * r.Value);

        public static NumberObject operator /(NumberObject l, NumberObject r) => new NumberObject(l.Value / r.Value);

        public static NumberObject operator %(NumberObject l, NumberObject r) => new NumberObject(l.Value % r.Value);
        public static NumberObject operator -(NumberObject n) => new NumberObject(-n.Value);

        //public static BooleanObject operator >(NumberObject l, NumberObject r) => new BooleanObject(l.Value > r.Value);
        //public static BooleanObject operator >=(NumberObject l, NumberObject r) => new BooleanObject(l.Value >= r.Value);
        //public static BooleanObject operator <(NumberObject l, NumberObject r) => new BooleanObject(l.Value < r.Value);
        //public static BooleanObject operator <=(NumberObject l, NumberObject r) => new BooleanObject(l.Value <= r.Value);
        //public static BooleanObject operator ==(NumberObject l, NumberObject r) => new BooleanObject(l.Value == r.Value);
        //public static BooleanObject operator !=(NumberObject l, NumberObject r) => new BooleanObject(l.Value != r.Value);

        //public static implicit operator BooleanObject(NumberObject num) => new BooleanObject(true);

        public static explicit operator int(NumberObject num)    => (int)num.Value;
        public static explicit operator double(NumberObject num) => (double)num.Value;
        public static explicit operator float(NumberObject num)  => (float)num.Value;
    }
}
