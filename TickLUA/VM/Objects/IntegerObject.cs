using System;

namespace TickLUA.VM.Objects
{
    public class IntegerObject : LuaObject
    {
        public static IntegerObject Zero { get; } = new IntegerObject(0);

        public int Value { get; }

        public IntegerObject(int value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is IntegerObject number)
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

        public static IntegerObject IntDiv(IntegerObject l, IntegerObject r) => new IntegerObject(l.Value / r.Value);
        public static IntegerObject Pow(IntegerObject l, IntegerObject r) => new IntegerObject((int)Math.Pow(l.Value, r.Value));

        public static IntegerObject operator +(IntegerObject l, IntegerObject r) => new IntegerObject(l.Value + r.Value);
        public static IntegerObject operator -(IntegerObject l, IntegerObject r) => new IntegerObject(l.Value - r.Value);
        public static IntegerObject operator *(IntegerObject l, IntegerObject r) => new IntegerObject(l.Value * r.Value);
        
        //TODO: float type return
        //public static IntegerObject operator /(IntegerObject l, IntegerObject r) => new IntegerObject(l.Value / r.Value);
       
        public static IntegerObject operator %(IntegerObject l, IntegerObject r) => new IntegerObject(l.Value % r.Value);
        public static IntegerObject operator -(IntegerObject n) => new IntegerObject(-n.Value);
        //public static BooleanObject operator >(NumberObject l, NumberObject r) => new BooleanObject(l.Value > r.Value);
        //public static BooleanObject operator >=(NumberObject l, NumberObject r) => new BooleanObject(l.Value >= r.Value);
        //public static BooleanObject operator <(NumberObject l, NumberObject r) => new BooleanObject(l.Value < r.Value);
        //public static BooleanObject operator <=(NumberObject l, NumberObject r) => new BooleanObject(l.Value <= r.Value);
        //public static BooleanObject operator ==(NumberObject l, NumberObject r) => new BooleanObject(l.Value == r.Value);
        //public static BooleanObject operator !=(NumberObject l, NumberObject r) => new BooleanObject(l.Value != r.Value);

        //public static implicit operator BooleanObject(NumberObject num) => new BooleanObject(true);
        //public static implicit operator float(NumberObject num) => num.Value;
        public static implicit operator int(IntegerObject num) => num.Value;
    }
}
