namespace TickLUA.VM.Objects
{
    /// <summary>
    /// Opt-in metatable support for a Lua value. The VM consults this
    /// interface (never concrete types) for metamethod dispatch and
    /// getmetatable; values that don't implement it simply have none.
    /// Read-only at the interface level: script-side setmetatable assigns
    /// through <see cref="TableObject"/> directly, and treats every other
    /// implementation as protected — so a host type can expose one shared
    /// static metatable without scripts being able to swap it.
    /// </summary>
    public interface IMetatable
    {
        /// <summary>The value's metatable, or null when unset.</summary>
        TableObject Metatable { get; }
    }
}
