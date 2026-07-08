using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// Built-in standard library functions, registered into every VM's _ENV table.
    /// </summary>
    internal static class StdLib
    {
        private static readonly NativeFunctionObject NextFunction   = new NativeFunctionObject("next", Next);
        private static readonly NativeFunctionObject PairsFunction  = new NativeFunctionObject("pairs", Pairs);
        private static readonly NativeFunctionObject IpairsFunction = new NativeFunctionObject("ipairs", Ipairs);
        private static readonly NativeFunctionObject IpairsIterator = new NativeFunctionObject("ipairs_iterator", IpairsStep);

        public static void Register(TableObject globals)
        {
            globals["next"]   = NextFunction;
            globals["pairs"]  = PairsFunction;
            globals["ipairs"] = IpairsFunction;
        }

        private static LuaObject[] Next(NativeArgs args)
        {
            var table = args.CheckTable(0);
            var key = args.IsNilOrNone(1) ? null : args[1];

            if (table.TryNext(key, out var next_key, out var next_value))
                return new LuaObject[] { next_key, next_value };

            return new LuaObject[] { NilObject.Nil };
        }

        private static LuaObject[] Pairs(NativeArgs args)
        {
            var table = args.CheckTable(0);
            return new LuaObject[] { NextFunction, table, NilObject.Nil };
        }

        private static LuaObject[] Ipairs(NativeArgs args)
        {
            var table = args.CheckTable(0);
            return new LuaObject[] { IpairsIterator, table, new NumberObject(0) };
        }

        private static LuaObject[] IpairsStep(NativeArgs args)
        {
            var table = args.CheckTable(0);
            int index = args.CheckInteger(1) + 1;

            var value = table[index];
            if (value is NilObject)
                return null;

            return new LuaObject[] { new NumberObject(index), value };
        }
    }
}
