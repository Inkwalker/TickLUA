using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// The coroutine library. resume and yield are VM-aware: they switch which
    /// coroutine's stack the tick loop executes rather than running anything
    /// synchronously (see <see cref="TickVM.ResumeCoroutine"/> /
    /// <see cref="TickVM.YieldCurrent"/>).
    /// </summary>
    internal static class StdLibCoroutine
    {
        private static readonly NativeFunctionObject CreateFunction = new NativeFunctionObject("create", Create);
        private static readonly NativeFunctionObject ResumeFunction = new NativeFunctionObject("resume", Resume, ResumeArgs);
        private static readonly NativeFunctionObject YieldFunction  = new NativeFunctionObject("yield", Yield, YieldArgs);
        private static readonly NativeFunctionObject StatusFunction = new NativeFunctionObject("status", Status);
        private static readonly NativeFunctionObject WrapFunction   = new NativeFunctionObject("wrap", Wrap);
        private static readonly NativeFunctionObject RunningFunction = new NativeFunctionObject("running", Running);
        private static readonly NativeFunctionObject IsYieldableFunction = new NativeFunctionObject("isyieldable", IsYieldable);

        public static void Register(TableObject globals)
        {
            var coroutine = new TableObject();
            coroutine["create"] = CreateFunction;
            coroutine["resume"] = ResumeFunction;
            coroutine["yield"]  = YieldFunction;
            coroutine["status"] = StatusFunction;
            coroutine["wrap"]   = WrapFunction;
            coroutine["running"] = RunningFunction;
            coroutine["isyieldable"] = IsYieldableFunction;
            globals["coroutine"] = coroutine;
        }

        private static LuaObject[] Create(NativeArgs args)
        {
            return new LuaObject[] { new CoroutineObject(CheckBody(args)) };
        }

        /// <summary>
        /// A coroutine body must be independently runnable: a closure, or a plain
        /// native. VM-aware natives read fixed caller registers and cannot be
        /// started on a fresh stack.
        /// </summary>
        private static LuaObject CheckBody(NativeArgs args)
        {
            var body = args.CheckAny(0);
            bool callable = body is ClosureObject
                || (body is NativeFunctionObject native && native.VmFunction == null);
            if (!callable)
                throw new RuntimeException(
                    $"bad argument #1 to '{args.FunctionName}' (function expected, got {NativeArgs.TypeName(body)})");
            return body;
        }

        /// <summary>
        /// coroutine.resume(co, ...) — reuses the pcall sinks: yielded or returned
        /// values arrive prefixed with true, a body error as (false, err).
        /// </summary>
        private static void Resume(TickVM vm, StackFrame frame, byte funcReg, int argCount, int resCount)
        {
            var inner = ResultsSink.ToRegisters(frame, funcReg, resCount);

            var co = CheckCoroutine(frame, funcReg, argCount, "resume");
            if (!CheckResumable(co, out var complaint))
            {
                inner(new LuaObject[] { BooleanObject.False, complaint });
                return;
            }

            var args = CollectArgs(frame, funcReg + 2, argCount - 1);
            vm.ResumeCoroutine(co, args, ResultsSink.PcallSuccess(inner), ResultsSink.PcallCatch(inner));
        }

        private static void Yield(TickVM vm, StackFrame frame, byte funcReg, int argCount, int resCount)
        {
            var values = CollectArgs(frame, funcReg + 1, argCount);
            vm.YieldCurrent(values, ResultsSink.ToRegisters(frame, funcReg, resCount));
        }

        /// <summary>
        /// resume called without a register context (metamethod dispatch).
        /// </summary>
        private static void ResumeArgs(TickVM vm, LuaObject[] args, ResultsSinkDelegate sink, ErrorSinkDelegate errorSink)
        {
            if (!(args.Length >= 1 && args[0] is CoroutineObject co))
                throw new RuntimeException(
                    $"bad argument #1 to 'resume' (coroutine expected, got {(args.Length >= 1 ? NativeArgs.TypeName(args[0]) : "no value")})");

            if (!CheckResumable(co, out var complaint))
            {
                sink(new LuaObject[] { BooleanObject.False, complaint });
                return;
            }

            var body_args = new LuaObject[args.Length - 1];
            System.Array.Copy(args, 1, body_args, 0, body_args.Length);
            vm.ResumeCoroutine(co, body_args, ResultsSink.PcallSuccess(sink), ResultsSink.PcallCatch(sink));
        }

        /// <summary>
        /// yield called without a register context (pcall, metamethod dispatch,
        /// TFORCALL): the caller's sink is both where the next resume's arguments
        /// land and, before that, simply parked — same protocol as the register form.
        /// </summary>
        private static void YieldArgs(TickVM vm, LuaObject[] args, ResultsSinkDelegate sink, ErrorSinkDelegate errorSink)
        {
            vm.YieldCurrent(args, sink);
        }

        private static LuaObject[] Status(NativeArgs args)
        {
            if (!(args.CheckAny(0) is CoroutineObject co))
                throw new RuntimeException(
                    $"bad argument #1 to '{args.FunctionName}' (coroutine expected, got {NativeArgs.TypeName(args[0])})");

            string status;
            switch (co.Status)
            {
                case CoroutineStatus.Running: status = "running"; break;
                case CoroutineStatus.Normal:  status = "normal"; break;
                case CoroutineStatus.Dead:    status = "dead"; break;
                default:                      status = "suspended"; break;
            }
            return new LuaObject[] { new StringObject(status) };
        }

        /// <summary>
        /// coroutine.wrap(f) — returns a function that resumes a fresh coroutine.
        /// Unlike resume it delivers raw results (no true prefix) and lets body
        /// errors propagate into the caller (null error sink = wrap mode).
        /// </summary>
        private static LuaObject[] Wrap(NativeArgs args)
        {
            var co = new CoroutineObject(CheckBody(args));

            // Two entry points onto the same coroutine: the register form for
            // ordinary calls, the args form for metamethod/TFORCALL dispatch —
            // which is what makes "for v in coroutine.wrap(f)" work.
            return new LuaObject[] { new NativeFunctionObject("wrap",
                (vm, frame, funcReg, argCount, resCount) =>
                {
                    if (!CheckResumable(co, out var complaint))
                        throw new RuntimeException(complaint);
                    var call_args = CollectArgs(frame, funcReg + 1, argCount);
                    vm.ResumeCoroutine(co, call_args,
                        ResultsSink.ToRegisters(frame, funcReg, resCount), null);
                },
                (vm, call_args, sink, errorSink) =>
                {
                    if (!CheckResumable(co, out var complaint))
                        throw new RuntimeException(complaint);
                    vm.ResumeCoroutine(co, call_args, sink, errorSink);
                }) };
        }

        private static void Running(TickVM vm, StackFrame frame, byte funcReg, int argCount, int resCount)
        {
            var current = vm.CurrentCoroutine;
            ResultsSink.ToRegisters(frame, funcReg, resCount)(new LuaObject[]
            {
                current,
                BooleanObject.FromBool(current == vm.MainCoroutine),
            });
        }

        private static void IsYieldable(TickVM vm, StackFrame frame, byte funcReg, int argCount, int resCount)
        {
            ResultsSink.ToRegisters(frame, funcReg, resCount)(new LuaObject[]
            {
                BooleanObject.FromBool(vm.CurrentCoroutine != vm.MainCoroutine),
            });
        }

        private static CoroutineObject CheckCoroutine(StackFrame frame, byte funcReg, int argCount, string name)
        {
            var value = argCount >= 1 ? frame.Registers[funcReg + 1].Value : NilObject.Nil;
            if (value is CoroutineObject co)
                return co;
            throw new RuntimeException(
                $"bad argument #1 to '{name}' (coroutine expected, got {(argCount >= 1 ? NativeArgs.TypeName(value) : "no value")})");
        }

        private static bool CheckResumable(CoroutineObject co, out StringObject complaint)
        {
            if (co.Status == CoroutineStatus.Suspended)
            {
                complaint = null;
                return true;
            }
            complaint = new StringObject(co.Status == CoroutineStatus.Dead
                ? "cannot resume dead coroutine"
                : "cannot resume non-suspended coroutine");
            return false;
        }

        private static LuaObject[] CollectArgs(StackFrame frame, int start_reg, int count)
        {
            if (count <= 0)
                return LuaObject.NoResults;

            var args = new LuaObject[count];
            for (int i = 0; i < count; i++)
            {
                args[i] = frame.Registers[start_reg + i].Value;
            }
            return args;
        }
    }
}
