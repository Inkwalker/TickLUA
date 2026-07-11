using TickLUA.VM.Handlers;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// Receives the results of a metamethod call in place of the normal
    /// register write (see <see cref="StackFrame.Sink"/>).
    /// </summary>
    internal delegate void ResultsSinkDelegate(TickVM vm, StackFrame caller, LuaObject[] results);

    /// <summary>
    /// Metamethod lookup and dispatch, shared by the instruction handlers and
    /// the stdlib. Table indexers stay raw; all metatable semantics live here.
    /// </summary>
    internal static class Metamethods
    {
        /// <summary>
        /// Cap on __index/__newindex/__call chains that resolve to another table
        /// within a single tick, so cyclic metatables fail instead of hanging.
        /// </summary>
        internal const int MaxChainDepth = 100;

        #region Cached metamethod name keys (avoid a StringObject allocation per lookup)
        internal static readonly StringObject IndexGetKey = new StringObject(LuaObject.INDEX_GET);
        internal static readonly StringObject IndexSetKey = new StringObject(LuaObject.INDEX_SET);
        internal static readonly StringObject CallKey     = new StringObject(LuaObject.CALL);
        internal static readonly StringObject LenKey      = new StringObject(LuaObject.LEN);
        internal static readonly StringObject UnmKey      = new StringObject(LuaObject.UNM);

        internal static readonly StringObject AddKey  = new StringObject(LuaObject.ADD);
        internal static readonly StringObject SubKey  = new StringObject(LuaObject.SUB);
        internal static readonly StringObject MulKey  = new StringObject(LuaObject.MUL);
        internal static readonly StringObject DivKey  = new StringObject(LuaObject.DIV);
        internal static readonly StringObject ModKey  = new StringObject(LuaObject.MOD);
        internal static readonly StringObject PowKey  = new StringObject(LuaObject.POW);
        internal static readonly StringObject IdivKey = new StringObject(LuaObject.IDIV);

        internal static readonly StringObject ConcatKey = new StringObject(LuaObject.CONCAT);

        internal static readonly StringObject LessKey   = new StringObject(LuaObject.LESS);
        internal static readonly StringObject LessEqKey = new StringObject(LuaObject.LESS_EQ);
        internal static readonly StringObject EqualsKey = new StringObject(LuaObject.EQUALS);

        internal static readonly StringObject MetatableKey = new StringObject(LuaObject.METATABLE);
        #endregion

        /// <summary>
        /// The metamethod handler for an event on a value, or null when the value
        /// has no metatable or the metatable lacks the field. The lookup on the
        /// metatable itself is raw. Only tables carry metatables for now.
        /// </summary>
        internal static LuaObject GetHandler(LuaObject obj, StringObject event_key)
        {
            var metatable = (obj as TableObject)?.Metatable;
            if (metatable == null)
                return null;

            var handler = metatable[event_key];
            return handler is NilObject ? null : handler;
        }

        /// <summary>
        /// Calls a value with an explicit argument array, resolving __call chains
        /// for non-function callers. A closure caller runs on later ticks via a
        /// pushed frame; a native runs inline this tick. Results go to the
        /// caller's registers at results_start_reg (res_count &lt; 0 = all), or to
        /// <paramref name="sink"/> when given. With <paramref name="protect"/> the
        /// call behaves like pcall: errors become (false, err) results.
        /// </summary>
        internal static void Call(TickVM vm, StackFrame caller, LuaObject callee, LuaObject[] args,
            byte results_start_reg, int res_count, ResultsSinkDelegate sink = null, bool protect = false)
        {
            for (int depth = 0; depth < MaxChainDepth; depth++)
            {
                if (callee is ClosureObject closure)
                {
                    var new_frame = HandlersCore.BuildArgsFrame(closure, args, results_start_reg, res_count);
                    new_frame.Sink = sink;
                    new_frame.IsProtected = protect;
                    vm.PushFrame(new_frame);
                    return;
                }

                if (callee is NativeFunctionObject native)
                {
                    if (native.VmFunction != null)
                        // A VM-aware native reads its arguments from fixed caller
                        // register positions; an argument array cannot be routed to it.
                        throw new RuntimeException(
                            $"'{native.Name}' cannot be called through a metamethod");

                    LuaObject[] results;
                    if (protect)
                    {
                        try
                        {
                            results = InvokeNative(native, args);
                            results = PrependTrue(results);
                        }
                        catch (RuntimeException ex)
                        {
                            results = new LuaObject[] { BooleanObject.False, ex.ErrorValue };
                        }
                    }
                    else
                    {
                        results = InvokeNative(native, args);
                    }

                    if (sink != null)
                        sink(vm, caller, results);
                    else
                        HandlersCore.WriteResults(caller, results_start_reg, res_count, results);
                    return;
                }

                // Not directly callable: resolve one __call layer, prepending the
                // callee itself as the first argument (Lua 5.4 semantics).
                var handler = GetHandler(callee, CallKey);
                if (handler == null)
                    throw new RuntimeException(
                        $"attempt to call a {NativeArgs.TypeName(callee)} value");

                var new_args = new LuaObject[args.Length + 1];
                new_args[0] = callee;
                System.Array.Copy(args, 0, new_args, 1, args.Length);
                args = new_args;
                callee = handler;
            }

            throw new RuntimeException("'__call' chain too long; possible loop");
        }

        private static LuaObject[] InvokeNative(NativeFunctionObject native, LuaObject[] args)
        {
            return native.Function(new NativeArgs(args, native.Name)) ?? LuaObject.NoResults;
        }

        private static LuaObject[] PrependTrue(LuaObject[] results)
        {
            var prepended = new LuaObject[results.Length + 1];
            prepended[0] = BooleanObject.True;
            System.Array.Copy(results, 0, prepended, 1, results.Length);
            return prepended;
        }

        /// <summary>
        /// Full table-read semantics for t[k]: raw hit, then the __index chain.
        /// A table handler restarts the lookup on it (honoring its own metatable);
        /// a function handler is called with (table, key) and its result lands in
        /// dest_reg (possibly on a later tick). Strings keep their raw indexer.
        /// </summary>
        internal static void Index(TickVM vm, StackFrame frame, LuaObject target, LuaObject key, byte dest_reg)
        {
            for (int depth = 0; depth < MaxChainDepth; depth++)
            {
                if (target is TableObject table)
                {
                    var raw = table[key];
                    if (!(raw is NilObject))
                    {
                        frame.Registers[dest_reg].Value = raw;
                        return;
                    }

                    var handler = GetHandler(table, IndexGetKey);
                    if (handler == null)
                    {
                        frame.Registers[dest_reg].Value = NilObject.Nil;
                        return;
                    }

                    if (handler is ClosureObject || handler is NativeFunctionObject)
                    {
                        Call(vm, frame, handler, new LuaObject[] { table, key }, dest_reg, 1);
                        return;
                    }

                    // Any other handler value is indexed in turn (Lua semantics);
                    // non-indexable values error on the next iteration.
                    target = handler;
                    continue;
                }

                if (target is IIndexable indexable)
                {
                    // Strings: raw read-only indexing, no metatable support yet.
                    frame.Registers[dest_reg].Value = indexable[key];
                    return;
                }

                throw new RuntimeException($"attempt to index a {NativeArgs.TypeName(target)} value");
            }

            throw new RuntimeException("'__index' chain too long; possible loop");
        }

        /// <summary>
        /// Full table-write semantics for t[k] = v: raw set when the key is
        /// present or there is no __newindex; otherwise a table handler restarts
        /// the write on it and a function handler is called with (table, key, value).
        /// </summary>
        internal static void NewIndex(TickVM vm, StackFrame frame, LuaObject target, LuaObject key, LuaObject value)
        {
            for (int depth = 0; depth < MaxChainDepth; depth++)
            {
                if (target is TableObject table)
                {
                    if (table.Contains(key))
                    {
                        table[key] = value;
                        return;
                    }

                    var handler = GetHandler(table, IndexSetKey);
                    if (handler == null)
                    {
                        if (key is NilObject)
                            throw new RuntimeException("table index is nil");

                        table[key] = value;
                        return;
                    }

                    if (handler is ClosureObject || handler is NativeFunctionObject)
                    {
                        Call(vm, frame, handler, new LuaObject[] { table, key, value }, 0, 0);
                        return;
                    }

                    target = handler;
                    continue;
                }

                throw new RuntimeException($"attempt to index a {NativeArgs.TypeName(target)} value");
            }

            throw new RuntimeException("'__newindex' chain too long; possible loop");
        }

        /// <summary>
        /// Metamethod fallback for a binary arithmetic op whose fast path failed:
        /// handler from the left operand, then the right; called with (l, r),
        /// first result to dest_reg. Errors naming the non-number operand.
        /// </summary>
        internal static void Math(TickVM vm, StackFrame frame, byte dest_reg,
            LuaObject l, LuaObject r, StringObject event_key)
        {
            var handler = GetHandler(l, event_key) ?? GetHandler(r, event_key);
            if (handler == null)
            {
                var offender = l is NumberObject ? r : l;
                throw new RuntimeException(
                    $"attempt to perform arithmetic on a {NativeArgs.TypeName(offender)} value");
            }

            Call(vm, frame, handler, new LuaObject[] { l, r }, dest_reg, 1);
        }

        internal static bool ToBool(LuaObject value) => (bool)value.ToBooleanObject();

        /// <summary>
        /// Result sinks for comparison metamethods: EQ/LT/LE skip the next
        /// caller instruction when the (boolean-coerced) result differs from
        /// the expected flag encoded in the comparison instruction.
        /// </summary>
        internal static readonly ResultsSinkDelegate BranchExpectTrue = (vm, caller, results) =>
        {
            if (!ToBool(results.Length > 0 ? results[0] : NilObject.Nil))
                caller.PC++;
        };

        internal static readonly ResultsSinkDelegate BranchExpectFalse = (vm, caller, results) =>
        {
            if (ToBool(results.Length > 0 ? results[0] : NilObject.Nil))
                caller.PC++;
        };
    }
}
