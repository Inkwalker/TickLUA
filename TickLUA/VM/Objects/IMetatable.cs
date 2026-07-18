namespace TickLUA.VM.Objects
{
    /// <summary>
    /// Opt-in metatable support for a Lua value. The VM consults this
    /// interface (never concrete types) for metamethod dispatch and
    /// setmetatable/getmetatable, so any type that implements it — tables
    /// today, extendable host types later — participates in metatable
    /// semantics; values that don't implement it simply have none.
    /// </summary>
    public interface IMetatable
    {
        /// <summary>The value's metatable, or null when unset.</summary>
        TableObject Metatable { get; set; }
    }
}
