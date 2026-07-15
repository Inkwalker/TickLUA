using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    internal class RegisterCell
    {
        private LuaObject value = NilObject.Nil;

        // The universal choke point for value movement: every register and
        // upvalue write lands here, so this is where the memory ledger sees
        // reference changes (no-op when the executing VM has no limit).
        public LuaObject Value
        {
            get => value;
            set
            {
                MemoryLedger.OnSlotWrite(this.value, value);
                this.value = value;
            }
        }
    }
}
