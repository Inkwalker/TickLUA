using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    internal class RegisterCell
    {
        public RegisterCell()
        {
            Value = NilObject.Nil;
        }

        public LuaObject Value { get; set; }
    }
}
