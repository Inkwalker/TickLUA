namespace TickLUA.VM.Objects
{
    public interface IIndexable
    {
        LuaObject this[LuaObject index]
        {
            get;
            set;
        }

        bool Contains(LuaObject index);
    }
}
