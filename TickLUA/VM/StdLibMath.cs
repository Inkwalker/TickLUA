using System;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// The math library. Every function is a plain native: they compute and
    /// return, never touching the call stack. Results are computed in double
    /// precision and narrowed on the way out, so a script sees the same float
    /// domain the VM's arithmetic operators work in.
    /// </summary>
    internal static class StdLibMath
    {
        private static readonly NativeFunctionObject AbsFunction   = new NativeFunctionObject("abs", Abs);
        private static readonly NativeFunctionObject AcosFunction  = new NativeFunctionObject("acos", Acos);
        private static readonly NativeFunctionObject AsinFunction  = new NativeFunctionObject("asin", Asin);
        private static readonly NativeFunctionObject AtanFunction  = new NativeFunctionObject("atan", Atan);
        private static readonly NativeFunctionObject Atan2Function = new NativeFunctionObject("atan2", Atan2);
        private static readonly NativeFunctionObject CeilFunction  = new NativeFunctionObject("ceil", Ceil);
        private static readonly NativeFunctionObject CosFunction   = new NativeFunctionObject("cos", Cos);
        private static readonly NativeFunctionObject DegFunction   = new NativeFunctionObject("deg", Deg);
        private static readonly NativeFunctionObject ExpFunction   = new NativeFunctionObject("exp", Exp);
        private static readonly NativeFunctionObject FloorFunction = new NativeFunctionObject("floor", Floor);
        private static readonly NativeFunctionObject LogFunction   = new NativeFunctionObject("log", Log);
        private static readonly NativeFunctionObject Log10Function = new NativeFunctionObject("log10", Log10);
        private static readonly NativeFunctionObject MaxFunction   = new NativeFunctionObject("max", Max);
        private static readonly NativeFunctionObject MinFunction   = new NativeFunctionObject("min", Min);
        private static readonly NativeFunctionObject ModfFunction  = new NativeFunctionObject("modf", Modf);
        private static readonly NativeFunctionObject PowFunction   = new NativeFunctionObject("pow", Pow);
        private static readonly NativeFunctionObject RadFunction   = new NativeFunctionObject("rad", Rad);
        private static readonly NativeFunctionObject SinFunction   = new NativeFunctionObject("sin", Sin);
        private static readonly NativeFunctionObject SqrtFunction  = new NativeFunctionObject("sqrt", Sqrt);
        private static readonly NativeFunctionObject TanFunction   = new NativeFunctionObject("tan", Tan);

        /// <summary>
        /// Fixed by design: an unseeded VM replays the same sequence every run,
        /// matching reference Lua and keeping a tick-by-tick replay reproducible.
        /// A host that wants variety calls math.randomseed itself.
        /// </summary>
        private const int DefaultRandomSeed = 0;

        public static void Register(TableObject globals)
        {
            var math = new TableObject();

            math["abs"]   = AbsFunction;
            math["acos"]  = AcosFunction;
            math["asin"]  = AsinFunction;
            math["atan"]  = AtanFunction;
            math["atan2"] = Atan2Function;
            math["ceil"]  = CeilFunction;
            math["cos"]   = CosFunction;
            math["deg"]   = DegFunction;
            math["exp"]   = ExpFunction;
            math["floor"] = FloorFunction;
            math["log"]   = LogFunction;
            math["log10"] = Log10Function;
            math["max"]   = MaxFunction;
            math["min"]   = MinFunction;
            math["modf"]  = ModfFunction;
            math["pow"]   = PowFunction;
            math["rad"]   = RadFunction;
            math["sin"]   = SinFunction;
            math["sqrt"]  = SqrtFunction;
            math["tan"]   = TanFunction;

            // The generator is per-VM, not shared static state: two VMs in the
            // same host must not be able to shift each other's sequence, and a
            // VM reseeded by its script must not disturb its neighbours.
            var random = new RandomSource();
            math["random"]     = new NativeFunctionObject("random", random.Random);
            math["randomseed"] = new NativeFunctionObject("randomseed", random.RandomSeed);

            math["pi"]   = new NumberObject((float)Math.PI);
            math["huge"] = new NumberObject(float.PositiveInfinity);

            globals["math"] = math;
        }

        private static LuaObject[] Abs(NativeArgs args)   => Number(Math.Abs(args.CheckNumber(0)));
        private static LuaObject[] Acos(NativeArgs args)  => Number(Math.Acos(args.CheckNumber(0)));
        private static LuaObject[] Asin(NativeArgs args)  => Number(Math.Asin(args.CheckNumber(0)));
        private static LuaObject[] Atan(NativeArgs args)  => Number(Math.Atan(args.CheckNumber(0)));
        private static LuaObject[] Ceil(NativeArgs args)  => Number(Math.Ceiling(args.CheckNumber(0)));
        private static LuaObject[] Cos(NativeArgs args)   => Number(Math.Cos(args.CheckNumber(0)));
        private static LuaObject[] Exp(NativeArgs args)   => Number(Math.Exp(args.CheckNumber(0)));
        private static LuaObject[] Floor(NativeArgs args) => Number(Math.Floor(args.CheckNumber(0)));
        private static LuaObject[] Log(NativeArgs args)   => Number(Math.Log(args.CheckNumber(0)));
        private static LuaObject[] Log10(NativeArgs args) => Number(Math.Log10(args.CheckNumber(0)));
        private static LuaObject[] Sin(NativeArgs args)   => Number(Math.Sin(args.CheckNumber(0)));
        private static LuaObject[] Sqrt(NativeArgs args)  => Number(Math.Sqrt(args.CheckNumber(0)));
        private static LuaObject[] Tan(NativeArgs args)   => Number(Math.Tan(args.CheckNumber(0)));

        private static LuaObject[] Atan2(NativeArgs args)
            => Number(Math.Atan2(args.CheckNumber(0), args.CheckNumber(1)));

        private static LuaObject[] Pow(NativeArgs args)
            => Number(Math.Pow(args.CheckNumber(0), args.CheckNumber(1)));

        private static LuaObject[] Deg(NativeArgs args)
            => Number(args.CheckNumber(0) * (180.0 / Math.PI));

        private static LuaObject[] Rad(NativeArgs args)
            => Number(args.CheckNumber(0) * (Math.PI / 180.0));

        private static LuaObject[] Max(NativeArgs args)
        {
            double best = args.CheckNumber(0);
            for (int i = 1; i < args.Count; i++)
            {
                double value = args.CheckNumber(i);
                if (value > best)
                    best = value;
            }
            return Number(best);
        }

        private static LuaObject[] Min(NativeArgs args)
        {
            double best = args.CheckNumber(0);
            for (int i = 1; i < args.Count; i++)
            {
                double value = args.CheckNumber(i);
                if (value < best)
                    best = value;
            }
            return Number(best);
        }

        /// <summary>
        /// modf(x) — integral part (truncated towards zero) and fractional part,
        /// both keeping x's sign. An infinite argument has no fractional part;
        /// subtracting would give NaN, so it is answered directly.
        /// </summary>
        private static LuaObject[] Modf(NativeArgs args)
        {
            double x = args.CheckNumber(0);
            double integral = x >= 0 ? Math.Floor(x) : Math.Ceiling(x);
            double fraction = double.IsInfinity(x) ? 0.0 : x - integral;

            return new LuaObject[]
            {
                new NumberObject((float)integral),
                new NumberObject((float)fraction)
            };
        }

        private static LuaObject[] Number(double value)
            => new LuaObject[] { new NumberObject((float)value) };

        /// <summary>
        /// Per-VM generator behind math.random / math.randomseed. Instance
        /// methods rather than statics so each VM's closure carries its own
        /// stream (see <see cref="Register"/>).
        /// </summary>
        private sealed class RandomSource
        {
            private Random random = new Random(DefaultRandomSeed);

            /// <summary>
            /// random() → float in [0,1); random(m) → integer in [1,m];
            /// random(m,n) → integer in [m,n].
            /// </summary>
            public LuaObject[] Random(NativeArgs args)
            {
                if (args.Count == 0)
                    return Number(random.NextDouble());
                if (args.Count > 2)
                    throw new RuntimeException($"wrong number of arguments to '{args.FunctionName}'");

                int low = 1;
                int high = args.CheckInteger(0);

                if (args.Count > 1)
                {
                    low = high;
                    high = args.CheckInteger(1);
                }

                if (low > high)
                    throw new RuntimeException(
                        $"bad argument #{args.Count} to '{args.FunctionName}' (interval is empty)");

                return new LuaObject[] { new NumberObject(NextInRange(low, high)) };
            }

            /// <summary>
            /// Seeds from the argument's bit pattern rather than its numeric
            /// value: every float maps to a distinct seed, including ones far
            /// outside int range, where a cast would be lossy or undefined.
            /// </summary>
            public LuaObject[] RandomSeed(NativeArgs args)
            {
                float seed = args.CheckNumber(0);
                random = new Random(BitConverter.ToInt32(BitConverter.GetBytes(seed), 0));
                return LuaObject.NoResults;
            }

            /// <summary>
            /// Uniform integer in [low,high]. Random.Next's exclusive upper
            /// bound cannot express high == int.MaxValue, so the range is
            /// scaled in long/double instead.
            /// </summary>
            private int NextInRange(int low, int high)
            {
                long span = (long)high - low + 1;
                return (int)(low + (long)(random.NextDouble() * span));
            }
        }
    }
}
