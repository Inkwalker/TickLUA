using TickLUA.Compilers;
using TickLUA.Compilers.LUA;
using TickLUA.VM.Handlers;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// require/dofile and the package table. Module source comes from the
    /// host's <see cref="TickVM.ModuleReader"/> delegate — the VM never touches
    /// the filesystem itself. Both are VM-aware natives: a chunk cannot run
    /// inside a native, so its frame is pushed and runs on later ticks. The
    /// args forms exist so pcall(require, ...) can hand over its error sink;
    /// a runtime error inside the module body then lands in the pcall instead
    /// of unwinding past it.
    /// </summary>
    internal static class StdLibPackage
    {
        private static readonly NativeFunctionObject RequireFunction =
            new NativeFunctionObject("require", Require, RequireArgs);
        private static readonly NativeFunctionObject DofileFunction =
            new NativeFunctionObject("dofile", Dofile, DofileArgs);

        /// <summary>
        /// Parked in the loaded table while a module's chunk is running, and
        /// left there if the chunk errors: a require that finds it reports a
        /// load loop. Scripts can observe it through package.loaded after a
        /// pcall-caught failure, but require never returns it.
        /// </summary>
        private sealed class LoadingSentinel : LuaObject
        {
            public override string ToString() => "< module loading >";
            public override StringObject ToStringObject() => new StringObject("[module loading]");
        }

        private static readonly LuaObject Loading = new LoadingSentinel();

        public static void Register(TableObject globals, TableObject loadedModules)
        {
            globals["require"] = RequireFunction;
            globals["dofile"] = DofileFunction;

            var package = new TableObject();
            package["loaded"] = loadedModules;
            globals["package"] = package;
        }

        private static void Require(TickVM vm, StackFrame frame, byte funcReg, int argCount, int resCount)
        {
            var name = CheckName(frame, funcReg, argCount, "require");
            RequireCore(vm, name, ResultsSink.ToRegisters(frame, funcReg, resCount), null);
        }

        private static void RequireArgs(TickVM vm, LuaObject[] args,
            ResultsSinkDelegate sink, ErrorSinkDelegate errorSink)
        {
            var name = new NativeArgs(args, "require").CheckString(0);
            RequireCore(vm, name, sink, errorSink);
        }

        private static void RequireCore(TickVM vm, string name,
            ResultsSinkDelegate sink, ErrorSinkDelegate errorSink)
        {
            var loaded = vm.LoadedModules;
            var cached = loaded[name];

            if (ReferenceEquals(cached, Loading))
                throw new RuntimeException($"loop or previous error loading module '{name}'");

            if (!(cached is NilObject))
            {
                sink(new LuaObject[] { cached });
                return;
            }

            var closure = LoadChunk(vm, name, $"module '{name}' not found");

            // Parked before the chunk runs; a nested require of the same name
            // hits the sentinel above instead of looping forever.
            loaded[name] = Loading;

            // Per Lua, the module name arrives in the chunk as '...'.
            var module_frame = HandlersCore.BuildArgsFrame(closure,
                new LuaObject[] { new StringObject(name) },
                results =>
                {
                    var result = results.Length > 0 && results[0] != null
                        ? results[0] : (LuaObject)NilObject.Nil;

                    if (!(result is NilObject))
                        loaded[name] = result;
                    else if (ReferenceEquals(loaded[name], Loading))
                        // Returned nothing and didn't write package.loaded itself.
                        loaded[name] = BooleanObject.True;

                    sink(new LuaObject[] { loaded[name] });
                });
            module_frame.ErrorSink = errorSink;
            vm.PushFrame(module_frame);
        }

        private static void Dofile(TickVM vm, StackFrame frame, byte funcReg, int argCount, int resCount)
        {
            var path = CheckName(frame, funcReg, argCount, "dofile");
            DofileCore(vm, path, ResultsSink.ToRegisters(frame, funcReg, resCount), null);
        }

        private static void DofileArgs(TickVM vm, LuaObject[] args,
            ResultsSinkDelegate sink, ErrorSinkDelegate errorSink)
        {
            var path = new NativeArgs(args, "dofile").CheckString(0);
            DofileCore(vm, path, sink, errorSink);
        }

        private static void DofileCore(TickVM vm, string path,
            ResultsSinkDelegate sink, ErrorSinkDelegate errorSink)
        {
            var closure = LoadChunk(vm, path, $"cannot open '{path}'");

            // The caller's sink receives the chunk's results directly, so all
            // return values flow through, however many the call site wants.
            var file_frame = HandlersCore.BuildArgsFrame(closure, LuaObject.NoResults, sink);
            file_frame.ErrorSink = errorSink;
            vm.PushFrame(file_frame);
        }

        /// <summary>
        /// Reads the source through the host delegate, compiles it and wraps it
        /// in a closure sharing the VM's globals as _ENV — the same wiring the
        /// main chunk gets in the TickVM constructor.
        /// </summary>
        private static ClosureObject LoadChunk(TickVM vm, string name, string notFoundMessage)
        {
            if (vm.ModuleReader == null)
                throw new RuntimeException($"attempt to load '{name}' (no module reader installed)");

            var source = vm.ModuleReader(name);
            if (source == null)
                throw new RuntimeException(notFoundMessage);

            LuaFunction function;
            try
            {
                function = LuaCompiler.Compile(source, name);
            }
            catch (CompilationException ex)
            {
                throw new RuntimeException(
                    $"error loading module '{name}': {ex.Message} (line {ex.Line}, column {ex.Column})");
            }

            var upvalues = new RegisterCell[] { new RegisterCell { Value = vm.Globals } };
            return new ClosureObject(function, upvalues);
        }

        private static string CheckName(StackFrame frame, byte funcReg, int argCount, string funcName)
        {
            var args = new LuaObject[argCount];
            for (int i = 0; i < argCount; i++)
                args[i] = frame.Registers[funcReg + 1 + i].Value;

            return new NativeArgs(args, funcName).CheckString(0);
        }
    }
}
