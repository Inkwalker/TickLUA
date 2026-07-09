using System;
using TickLUA.VM.Handlers;
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
        private static readonly NativeFunctionObject ErrorFunction  = new NativeFunctionObject("error", Error);
        private static readonly NativeFunctionObject AssertFunction = new NativeFunctionObject("assert", Assert);
        private static readonly NativeFunctionObject PcallFunction  = new NativeFunctionObject("pcall", Pcall);

        public static void Register(TableObject globals)
        {
            globals["next"]   = NextFunction;
            globals["pairs"]  = PairsFunction;
            globals["ipairs"] = IpairsFunction;
            globals["error"]  = ErrorFunction;
            globals["assert"] = AssertFunction;
            globals["pcall"]  = PcallFunction;
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

        private static LuaObject[] Error(NativeArgs args)
        {
            // The level argument (args[1]) is accepted but ignored: bytecode
            // carries no line info, so no position prefix is added.
            throw new RuntimeException(args[0]);
        }

        private static LuaObject[] Assert(NativeArgs args)
        {
            if ((bool)args[0].ToBooleanObject())
                return args.ToArray();

            throw new RuntimeException(args.IsNilOrNone(1)
                ? (LuaObject)new StringObject("assertion failed!")
                : args[1]);
        }

        /// <summary>
        /// pcall(f, ...) — VM-aware: a Lua closure cannot be run from inside a
        /// native, so function frame is pushed marked IsProtected and runs on later
        /// ticks; RETURN prepends true, error unwinding delivers (false, err).
        /// </summary>
        private static void Pcall(TickVM vm, StackFrame frame, byte funcReg, int argCount, int resCount)
        {
            if (argCount < 1)
                throw new RuntimeException("bad argument #1 to 'pcall' (value expected)");

            var target = frame.Registers[funcReg + 1].Value;
            int call_arg_count = argCount - 1;

            if (target is ClosureObject closure)
            {
                var new_frame = HandlersCore.BuildClosureFrame(
                    frame, closure, funcReg + 2, call_arg_count, funcReg, resCount);
                new_frame.IsProtected = true;
                vm.PushFrame(new_frame);
            }
            else if (target is NativeFunctionObject native)
            {
                if (native.VmFunction != null)
                {
                    // Protected call of another VM-aware native (e.g. pcall(pcall, f)):
                    // it never raises once its frames are pushed, so success is known
                    // now — write true, then let it run shifted one register right
                    // producing the remaining results. Its own protection handles
                    // later errors. Only its synchronous argument check can throw.
                    try
                    {
                        if (resCount != 0)
                        {
                            frame.GrowRegisters(funcReg + 1);
                            frame.Registers[funcReg].Value = BooleanObject.True;
                        }
                        // Variable (-1) and none (0) pass through; otherwise the
                        // inner call produces one fewer result (true was written).
                        int inner_res_count = resCount <= 0 ? resCount : resCount - 1;
                        native.VmFunction(vm, frame, (byte)(funcReg + 1), call_arg_count, inner_res_count);
                    }
                    catch (RuntimeException ex)
                    {
                        HandlersCore.WriteResults(frame, funcReg, resCount,
                            new LuaObject[] { BooleanObject.False, ex.ErrorValue });
                    }
                    return;
                }

                var call_args = new LuaObject[call_arg_count];
                for (int i = 0; i < call_arg_count; i++)
                {
                    call_args[i] = frame.Registers[funcReg + 2 + i].Value;
                }

                LuaObject[] results;
                try
                {
                    var native_results = native.Function(new NativeArgs(call_args, native.Name)) ?? LuaObject.NoResults;
                    results = new LuaObject[native_results.Length + 1];
                    results[0] = BooleanObject.True;
                    System.Array.Copy(native_results, 0, results, 1, native_results.Length);
                }
                catch (RuntimeException ex)
                {
                    results = new LuaObject[] { BooleanObject.False, ex.ErrorValue };
                }

                HandlersCore.WriteResults(frame, funcReg, resCount, results);
            }
            else
            {
                // A call error inside pcall is caught by pcall itself.
                HandlersCore.WriteResults(frame, funcReg, resCount, new LuaObject[]
                {
                    BooleanObject.False,
                    new StringObject($"attempt to call a {NativeArgs.TypeName(target)} value")
                });
            }
        }
    }
}
