using System;

namespace TickLUA.VM.Objects
{
    /// <summary>
    /// A C# function callable from Lua.
    /// Return null or an empty array for no results.
    /// Must not re-enter the VM (no <see cref="TickVM.Tick"/> from inside a native).
    /// A thrown <see cref="RuntimeException"/> propagates out of <see cref="TickVM.Tick"/>
    /// like any other Lua runtime error; other exceptions propagate unwrapped.
    /// </summary>
    public delegate LuaObject[] NativeFunction(NativeArgs args);

    /// <summary>
    /// A VM-aware native (internal only, e.g. pcall). Reads its arguments from and
    /// writes its results to the caller's registers itself, and may push frames onto
    /// the call stack — but must never call <see cref="TickVM.Tick"/>.
    /// </summary>
    internal delegate void VmNativeFunction(TickVM vm, StackFrame frame, byte funcReg, int argCount, int resCount);

    /// <summary>
    /// Wraps a C# delegate as a first-class Lua function value.
    /// </summary>
    public class NativeFunctionObject : LuaObject
    {
        public NativeFunction Function { get; }

        /// <summary>
        /// Non-null for VM-aware natives; takes precedence over <see cref="Function"/>.
        /// </summary>
        internal VmNativeFunction VmFunction { get; }

        /// <summary>
        /// Optional name used in error messages ("bad argument #1 to 'name' ...").
        /// </summary>
        public string Name { get; }

        public NativeFunctionObject(NativeFunction function) : this(null, function)
        {
        }

        public NativeFunctionObject(string name, NativeFunction function)
        {
            if (function == null)
                throw new ArgumentNullException(nameof(function));

            Name = name;
            Function = function;
        }

        internal NativeFunctionObject(string name, VmNativeFunction vmFunction)
        {
            if (vmFunction == null)
                throw new ArgumentNullException(nameof(vmFunction));

            Name = name;
            VmFunction = vmFunction;
        }

        public override string ToString() => Name == null ? "< native func >" : $"< native func '{Name}' >";

        public override StringObject ToStringObject() => new StringObject("[native func]");
    }
}
