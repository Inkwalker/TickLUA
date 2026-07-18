using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TickLUA.VM.Objects;

namespace TickLUA.VM
{
    /// <summary>
    /// Approximate memory accounting for one VM, enforcing
    /// <see cref="TickVMOptions.MaxMemoryBytes"/>. The ledger is kept
    /// incrementally: every value movement flows through a few choke points
    /// (register/upvalue cell writes, table slot writes, frame push/pop) and
    /// each adjusts <see cref="Total"/> by the value's shallow cost — strings
    /// by their length, containers by a flat header (their contents charge
    /// themselves through their own writes). O(1) per instruction, no
    /// traversal, cycles are a non-issue.
    ///
    /// The ledger never undercounts unboundedly but drifts UP over time:
    /// shared values are charged once per referencing slot, and dropping the
    /// last reference to a populated structure only credits that one slot
    /// (the .NET GC gives no free signal). <see cref="EnforceLimit"/> corrects
    /// the drift lazily: only when Total crosses the limit does a full
    /// reachability scan recompute the truth — scripts that stay under their
    /// limit never pay for a walk.
    /// </summary>
    internal class MemoryLedger
    {
        /// <summary>
        /// The ledger of the VM currently executing an instruction, set around
        /// Execute by <see cref="TickVM.Tick"/>. The write choke points
        /// (RegisterCell, TableObject) carry no VM reference, and a VM executes
        /// single-threaded, so a thread-static hands them the right ledger.
        /// Null when the executing VM has no memory limit — and outside Tick,
        /// so host writes are uncharged until the next correction scan counts
        /// them as reachable.
        /// </summary>
        [ThreadStatic]
        internal static MemoryLedger Current;

        // Rough x64 costs of the VM's own structures. Precision is not the
        // goal: the ledger only has to scale with real memory use so the
        // limit bounds it within a small constant factor. LuaObject costs
        // live on the types themselves (see LuaObject.ShallowMemoryCost).
        internal const long RegisterCost = 32;   // cell object + array slot
        internal const long FrameCost = 96;
        internal const long VarargSlotCost = 8;

        private readonly TickVM vm;
        internal readonly long MaxBytes;
        internal long Total;

        internal MemoryLedger(TickVM vm, long maxBytes)
        {
            this.vm = vm;
            MaxBytes = maxBytes;
        }

        /// <summary>
        /// Null-safe access to <see cref="LuaObject.ShallowMemoryCost"/>: the
        /// cost a value contributes to each slot referencing it. Each type
        /// reports its own cost (from the constants above), so new data types
        /// join the accounting by overriding the method.
        /// </summary>
        internal static long ShallowCost(LuaObject value)
        {
            return value == null ? 0 : value.ShallowMemoryCost();
        }

        /// <summary>
        /// Charge for one slot's reference changing from oldValue to newValue.
        /// Serves register/upvalue cells and the IMetatable Metatable edge.
        /// </summary>
        internal static void OnSlotWrite(LuaObject oldValue, LuaObject newValue)
        {
            var ledger = Current;
            if (ledger != null)
                ledger.Total += ShallowCost(newValue) - ShallowCost(oldValue);
        }

        /// <summary>Register array grown or shrunk by <paramref name="delta"/> cells.</summary>
        internal static void OnRegistersResized(int delta)
        {
            var ledger = Current;
            if (ledger != null)
                ledger.Total += RegisterCost * delta;
        }

        /// <summary>
        /// Structural cost of a frame: charged on push (sign +1), credited on
        /// pop (sign -1). Register cell VALUES are intentionally not credited
        /// on pop — cells can outlive the frame through closure captures, and
        /// crediting a still-referenced value would break the never-undercount
        /// invariant. The resulting drift is corrected by the scan. Varargs
        /// arrays are exclusively owned by the frame, so those are symmetric.
        /// </summary>
        internal void ChargeFrame(StackFrame frame, int sign)
        {
            long cost = FrameCost + RegisterCost * frame.Registers.Length;
            var varargs = frame.Varargs;
            for (int i = 0; i < varargs.Length; i++)
                cost += VarargSlotCost + ShallowCost(varargs[i]);
            Total += sign * cost;
        }

        /// <summary>
        /// Guard for the one instruction that can allocate a payload far larger
        /// than anything already charged: CONCAT. Called with the result length
        /// before the string is built, so a runaway s = s .. s dies at the
        /// limit instead of exhausting the host first. Scans before giving up,
        /// like <see cref="EnforceLimit"/>.
        /// </summary>
        internal static void PrecheckStringAllocation(long chars)
        {
            var ledger = Current;
            if (ledger == null)
                return;

            long bytes = StringObject.BaseMemoryCost + 2 * chars;
            if (ledger.Total + bytes <= ledger.MaxBytes)
                return;

            ledger.Total = ledger.MeasureReachable();
            if (ledger.Total + bytes > ledger.MaxBytes)
                throw new RuntimeException("not enough memory");
        }

        /// <summary>
        /// Called once per tick after the instruction ran. Cheap while under
        /// the limit; crossing it triggers the correction scan, and only a
        /// confirmed excess throws. The error is an ordinary Lua runtime
        /// error: pcall catches it, resume reports it.
        /// </summary>
        internal void EnforceLimit()
        {
            if (Total <= MaxBytes)
                return;

            Total = MeasureReachable();
            if (Total > MaxBytes)
                throw new RuntimeException("not enough memory");
        }

        /// <summary>
        /// The correction scan: recomputes what the ledger approximates by
        /// walking everything reachable from the VM's roots, using the same
        /// per-reference shallow costs (strings are billed once per slot,
        /// matching the ledger; containers and cells are visited once).
        /// Bytecode (instructions, unloaded constants) is host-bounded via
        /// the source it compiles and stays out of the budget.
        /// </summary>
        internal long MeasureReachable()
        {
            long total = 0;
            var visited = new HashSet<object>(ReferenceComparer.Instance);
            var pending = new Stack<object>();

            // A slot referencing this value: bill the shallow cost, queue
            // containers for a single visit each.
            void Visit(LuaObject value)
            {
                if (value == null)
                    return;
                total += ShallowCost(value);
                if ((value is TableObject || value is ClosureObject || value is CoroutineObject)
                    && visited.Add(value))
                    pending.Push(value);
            }

            void VisitCells(RegisterCell[] cells)
            {
                foreach (var cell in cells)
                    if (visited.Add(cell))
                        Visit(cell.Value);
            }

            void VisitFrame(StackFrame frame)
            {
                total += FrameCost + RegisterCost * frame.Registers.Length;
                VisitCells(frame.Registers);
                VisitCells(frame.Upvalues);
                foreach (var v in frame.Varargs)
                {
                    total += VarargSlotCost;
                    Visit(v);
                }
            }

            void AddRoot(LuaObject root)
            {
                if (root != null && visited.Add(root))
                    pending.Push(root);
            }

            AddRoot(vm.Globals);
            AddRoot(vm.LoadedModules);
            AddRoot(vm.MainCoroutine);
            AddRoot(vm.CurrentCoroutine);
            if (vm.ExecutionResult != null)
                foreach (var v in vm.ExecutionResult)
                    Visit(v);

            while (pending.Count > 0)
            {
                var node = pending.Pop();

                // The Metatable edge of any metatable-capable value is a
                // charged slot (see IMetatable), so it is billed like one here.
                if (node is IMetatable owner && owner.Metatable != null)
                    Visit(owner.Metatable);

                if (node is TableObject table)
                {
                    foreach (var pair in table.Elements)
                    {
                        total += TableObject.EntryMemoryCost;
                        Visit(pair.Key);
                        Visit(pair.Value);
                    }
                }
                else if (node is ClosureObject closure)
                {
                    // The closure's own header + upvalue array were billed as
                    // its shallow cost at the referencing slot.
                    VisitCells(closure.Upvalues);
                }
                else if (node is CoroutineObject co)
                {
                    foreach (var f in co.Stack)
                        VisitFrame(f);
                    Visit(co.Body);
                    if (co.Resumer != null && visited.Add(co.Resumer))
                        pending.Push(co.Resumer);
                }
            }

            return total;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();

            bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);
            int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
