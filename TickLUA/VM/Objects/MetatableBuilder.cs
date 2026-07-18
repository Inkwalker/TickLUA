namespace TickLUA.VM.Objects
{
    /// <summary>
    /// Fluent helper for building metatables for host ("user") types out of
    /// native function delegates. Each static entry point starts a chain, each
    /// chained call assigns one standard metamethod, and the finished chain
    /// converts implicitly to the <see cref="TableObject"/> the VM consumes:
    /// <code>obj.Metatable = MetatableBuilder.Call(funcA).Add(funcB);</code>
    /// Only the metamethods the VM actually dispatches are exposed.
    /// </summary>
    public static class MetatableBuilder
    {
        public static Chain Index(NativeFunction handler)    => new Chain().Index(handler);
        public static Chain NewIndex(NativeFunction handler) => new Chain().NewIndex(handler);
        public static Chain Call(NativeFunction handler)     => new Chain().Call(handler);
        public static Chain Len(NativeFunction handler)      => new Chain().Len(handler);
        public static Chain Unm(NativeFunction handler)      => new Chain().Unm(handler);

        public static Chain Add(NativeFunction handler)  => new Chain().Add(handler);
        public static Chain Sub(NativeFunction handler)  => new Chain().Sub(handler);
        public static Chain Mul(NativeFunction handler)  => new Chain().Mul(handler);
        public static Chain Div(NativeFunction handler)  => new Chain().Div(handler);
        public static Chain Mod(NativeFunction handler)  => new Chain().Mod(handler);
        public static Chain Pow(NativeFunction handler)  => new Chain().Pow(handler);
        public static Chain IDiv(NativeFunction handler) => new Chain().IDiv(handler);

        public static Chain Concat(NativeFunction handler) => new Chain().Concat(handler);

        public static Chain Eq(NativeFunction handler) => new Chain().Eq(handler);
        public static Chain Lt(NativeFunction handler) => new Chain().Lt(handler);
        public static Chain Le(NativeFunction handler) => new Chain().Le(handler);

        /// <summary>
        /// A metatable under construction. Every setter overwrites its slot and
        /// returns the same chain; assigning the chain where a
        /// <see cref="TableObject"/> is expected yields the built metatable.
        /// The delegates are wrapped in <see cref="NativeFunctionObject"/>s
        /// named after their event, so argument errors read
        /// "bad argument #1 to '__call' ...".
        /// </summary>
        public sealed class Chain
        {
            private readonly TableObject table = new TableObject();

            internal Chain() { }

            public Chain Index(NativeFunction handler)    => Set(LuaObject.INDEX_GET, handler);
            public Chain NewIndex(NativeFunction handler) => Set(LuaObject.INDEX_SET, handler);
            public Chain Call(NativeFunction handler)     => Set(LuaObject.CALL, handler);
            public Chain Len(NativeFunction handler)      => Set(LuaObject.LEN, handler);
            public Chain Unm(NativeFunction handler)      => Set(LuaObject.UNM, handler);

            public Chain Add(NativeFunction handler)  => Set(LuaObject.ADD, handler);
            public Chain Sub(NativeFunction handler)  => Set(LuaObject.SUB, handler);
            public Chain Mul(NativeFunction handler)  => Set(LuaObject.MUL, handler);
            public Chain Div(NativeFunction handler)  => Set(LuaObject.DIV, handler);
            public Chain Mod(NativeFunction handler)  => Set(LuaObject.MOD, handler);
            public Chain Pow(NativeFunction handler)  => Set(LuaObject.POW, handler);
            public Chain IDiv(NativeFunction handler) => Set(LuaObject.IDIV, handler);

            public Chain Concat(NativeFunction handler) => Set(LuaObject.CONCAT, handler);

            public Chain Eq(NativeFunction handler) => Set(LuaObject.EQUALS, handler);
            public Chain Lt(NativeFunction handler) => Set(LuaObject.LESS, handler);
            public Chain Le(NativeFunction handler) => Set(LuaObject.LESS_EQ, handler);

            private Chain Set(string event_name, NativeFunction handler)
            {
                // NativeFunctionObject rejects a null handler itself.
                table[event_name] = new NativeFunctionObject(event_name, handler);
                return this;
            }

            public static implicit operator TableObject(Chain chain) => chain.table;
        }
    }
}
