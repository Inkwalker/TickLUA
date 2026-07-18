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

        private static readonly NativeFunctionObject SetmetatableFunction = new NativeFunctionObject("setmetatable", Setmetatable);
        private static readonly NativeFunctionObject GetmetatableFunction = new NativeFunctionObject("getmetatable", Getmetatable);
        private static readonly NativeFunctionObject RawgetFunction   = new NativeFunctionObject("rawget", Rawget);
        private static readonly NativeFunctionObject RawsetFunction   = new NativeFunctionObject("rawset", Rawset);
        private static readonly NativeFunctionObject RawequalFunction = new NativeFunctionObject("rawequal", Rawequal);
        private static readonly NativeFunctionObject RawlenFunction   = new NativeFunctionObject("rawlen", Rawlen);

        public static void Register(TableObject globals, TableObject loadedModules)
        {
            globals["next"]   = NextFunction;
            globals["pairs"]  = PairsFunction;
            globals["ipairs"] = IpairsFunction;
            globals["error"]  = ErrorFunction;
            globals["assert"] = AssertFunction;
            globals["pcall"]  = PcallFunction;

            globals["setmetatable"] = SetmetatableFunction;
            globals["getmetatable"] = GetmetatableFunction;
            globals["rawget"]   = RawgetFunction;
            globals["rawset"]   = RawsetFunction;
            globals["rawequal"] = RawequalFunction;
            globals["rawlen"]   = RawlenFunction;

            StdLibCoroutine.Register(globals);
            StdLibPackage.Register(globals, loadedModules);
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

        private static LuaObject[] Setmetatable(NativeArgs args)
        {
            var value = args.CheckAny(0);
            if (!(value is IMetatable target))
                throw new RuntimeException(
                    $"bad argument #1 to '{args.FunctionName}' (table expected, got {NativeArgs.TypeName(value)})");
            if (!args.IsNil(1) && !args.IsTable(1))
                throw new RuntimeException(
                    $"bad argument #2 to '{args.FunctionName}' (nil or table expected)");

            if (target.Metatable != null && target.Metatable.Contains(Metamethods.MetatableKey))
                throw new RuntimeException("cannot change a protected metatable");

            target.Metatable = args[1] as TableObject; // null when nil
            return new LuaObject[] { value };
        }

        private static LuaObject[] Getmetatable(NativeArgs args)
        {
            var metatable = (args.CheckAny(0) as IMetatable)?.Metatable;
            if (metatable == null)
                return new LuaObject[] { NilObject.Nil };

            // A __metatable field masks the real metatable.
            if (metatable.Contains(Metamethods.MetatableKey))
                return new LuaObject[] { metatable[Metamethods.MetatableKey] };

            return new LuaObject[] { metatable };
        }

        private static LuaObject[] Rawget(NativeArgs args)
        {
            var table = args.CheckTable(0);
            return new LuaObject[] { table[args.CheckAny(1)] };
        }

        private static LuaObject[] Rawset(NativeArgs args)
        {
            var table = args.CheckTable(0);
            var key = args.CheckAny(1);
            if (key is NilObject)
                throw new RuntimeException("table index is nil");

            table[key] = args.CheckAny(2);
            return new LuaObject[] { table };
        }

        private static LuaObject[] Rawequal(NativeArgs args)
        {
            var a = args.CheckAny(0);
            var b = args.CheckAny(1);
            return new LuaObject[] { BooleanObject.FromBool(a.Equals(b)) };
        }

        private static LuaObject[] Rawlen(NativeArgs args)
        {
            if (args[0] is IHasLen measurable)
                return new LuaObject[] { measurable.Len() };

            throw new RuntimeException(
                $"bad argument #1 to '{args.FunctionName}' (table or string expected)");
        }

        /// <summary>
        /// pcall(f, ...) — VM-aware: a Lua closure cannot be run from inside a
        /// native, so its frame is pushed and runs on later ticks. Its sinks
        /// carry the pcall protocol: a normal return gets true prepended, an
        /// unwinding error is delivered as (false, err) via the ErrorSink.
        /// </summary>
        private static void Pcall(TickVM vm, StackFrame frame, byte funcReg, int argCount, int resCount)
        {
            if (argCount < 1)
                throw new RuntimeException("bad argument #1 to 'pcall' (value expected)");

            var target = frame.Registers[funcReg + 1].Value;
            int call_arg_count = argCount - 1;

            // All branches deliver the (ok, ...) tuple to the same place: the
            // caller's registers at funcReg.
            var inner = ResultsSink.ToRegisters(frame, funcReg, resCount);

            if (target is ClosureObject closure)
            {
                var new_frame = HandlersCore.BuildClosureFrame(
                    frame, closure, funcReg + 2, call_arg_count, ResultsSink.PcallSuccess(inner));
                new_frame.ErrorSink = ResultsSink.PcallCatch(inner);
                vm.PushFrame(new_frame);
            }
            else if (target is NativeFunctionObject native)
            {
                if (native.VmArgsFunction != null)
                {
                    // The args form takes explicit sinks, so the pcall protocol
                    // rides along: results get true prepended, and — crucially for
                    // error-propagating natives like a coroutine.wrap wrapper —
                    // a later asynchronous error is delivered here as (false, err)
                    // instead of unwinding past this pcall.
                    var vm_args = new LuaObject[call_arg_count];
                    for (int i = 0; i < call_arg_count; i++)
                    {
                        vm_args[i] = frame.Registers[funcReg + 2 + i].Value;
                    }

                    try
                    {
                        native.VmArgsFunction(vm, vm_args,
                            ResultsSink.PcallSuccess(inner), ResultsSink.PcallCatch(inner));
                    }
                    catch (RuntimeException ex)
                    {
                        ResultsSink.PcallCatch(inner)(ex.ErrorValue);
                    }
                    return;
                }

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
                        ResultsSink.PcallCatch(inner)(ex.ErrorValue);
                    }
                    return;
                }

                var call_args = new LuaObject[call_arg_count];
                for (int i = 0; i < call_arg_count; i++)
                {
                    call_args[i] = frame.Registers[funcReg + 2 + i].Value;
                }

                try
                {
                    var native_results = native.Function(new NativeArgs(call_args, native.Name)) ?? LuaObject.NoResults;
                    ResultsSink.PcallSuccess(inner)(native_results);
                }
                catch (RuntimeException ex)
                {
                    ResultsSink.PcallCatch(inner)(ex.ErrorValue);
                }
            }
            else
            {
                // Not a function: resolve __call under protection, so both a
                // missing metamethod and errors inside the handler become
                // (false, err) — a call error inside pcall is caught by pcall.
                var call_args = new LuaObject[call_arg_count];
                for (int i = 0; i < call_arg_count; i++)
                {
                    call_args[i] = frame.Registers[funcReg + 2 + i].Value;
                }

                try
                {
                    Metamethods.Call(vm, target, call_args,
                        ResultsSink.PcallSuccess(inner), ResultsSink.PcallCatch(inner));
                }
                catch (RuntimeException ex)
                {
                    // __call resolution itself failed (e.g. no metamethod).
                    ResultsSink.PcallCatch(inner)(ex.ErrorValue);
                }
            }
        }
    }
}
