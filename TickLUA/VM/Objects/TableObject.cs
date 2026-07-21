using System.Collections.Generic;

namespace TickLUA.VM.Objects
{
    public sealed class TableObject : LuaObject, IHasLen, IIndexable, IMetatable
    {
        // Rough x64 memory-accounting costs (see LuaObject.ShallowMemoryCost):
        // the table object itself, and one dictionary entry (slot + hash
        // bookkeeping) — the key and value bill their own shallow costs.
        internal const long HeaderMemoryCost = 64;
        internal const long EntryMemoryCost = 32;

        public Dictionary<LuaObject, LuaObject> Elements { get; }

        private TableObject metatable;

        /// <summary>
        /// The table's metatable, or null when unset. Consulted by the VM for
        /// metamethod dispatch; the indexers above stay raw. A charged slot
        /// like any other, so a chain of metatables stays inside the budget.
        /// </summary>
        public TableObject Metatable
        {
            get => metatable;
            set
            {
                MemoryLedger.OnSlotWrite(metatable, value);
                metatable = value;
            }
        }

        public LuaObject this[LuaObject i]
        {
            get
            {
                if (Elements.TryGetValue(i, out var obj))
                    return obj;
                else
                    return NilObject.Nil;
            }
            set
            {
                // Memory-ledger choke point: entry adds/removes and value
                // overwrites adjust the executing VM's budget (no-op when the
                // ledger is inactive). Keys and values bill their shallow cost
                // per slot; entry overhead covers the dictionary slot itself.
                var ledger = MemoryLedger.Current;
                if (ledger != null)
                {
                    Elements.TryGetValue(i, out var old);
                    if (value == NilObject.Nil)
                    {
                        if (old != null)
                            ledger.Total -= EntryMemoryCost
                                + MemoryLedger.ShallowCost(i) + MemoryLedger.ShallowCost(old);
                    }
                    else if (old != null)
                        ledger.Total += MemoryLedger.ShallowCost(value) - MemoryLedger.ShallowCost(old);
                    else
                        ledger.Total += EntryMemoryCost
                            + MemoryLedger.ShallowCost(i) + MemoryLedger.ShallowCost(value);
                }

                if (value == NilObject.Nil)
                    Elements.Remove(i);
                else
                    Elements[i] = value;
            }
        }

        public LuaObject this[int i]
        {
            get => this[new NumberObject(i)];
            set => this[new NumberObject(i)] = value;
        }

        public LuaObject this[string i]
        {
            get => this[new StringObject(i)];
            set => this[new StringObject(i)] = value;
        }


        public TableObject()
        {
            Elements = new Dictionary<LuaObject, LuaObject>();
        }

        ///<summary> Array constructor </summary>
        public TableObject(params LuaObject[] elements)
        {
            Elements = new Dictionary<LuaObject, LuaObject>();

            for (int i = 0; i < elements.Length; i++)
            {
                var key = new NumberObject(i + 1);
                var value = elements[i];

                Elements[key] = value;
            }

            ChargeInitialContents();
        }

        ///<summary> Key-value pair constructor </summary>
        public TableObject(IDictionary<LuaObject, LuaObject> elements)
        {
            Elements = new Dictionary<LuaObject, LuaObject>(elements);

            ChargeInitialContents();
        }

        ///<summary> Copy constructor </summary>
        public TableObject(TableObject other)
        {
            Elements = new Dictionary<LuaObject, LuaObject>(other.Elements);

            ChargeInitialContents();
        }

        /// <summary>
        /// Bulk constructors fill Elements without going through the indexer,
        /// so the entries are billed here instead when a ledger is active.
        /// </summary>
        private void ChargeInitialContents()
        {
            var ledger = MemoryLedger.Current;
            if (ledger == null)
                return;

            foreach (var pair in Elements)
                ledger.Total += EntryMemoryCost
                    + MemoryLedger.ShallowCost(pair.Key) + MemoryLedger.ShallowCost(pair.Value);
        }

        public T Get<T>(LuaObject key, T default_value) where T : LuaObject
        {
            if (Elements.TryGetValue(key, out LuaObject val))
            {
                if (val is T r) return r;
            }
            return default_value;
        }

        public T Get<T>(int key, T default_value) where T : LuaObject
        {
            if (Elements.TryGetValue(new NumberObject(key), out LuaObject val))
            {
                if (val is T r) return r;
            }
            return default_value;
        }

        public bool Contains(LuaObject index)
        {
            return Elements.ContainsKey(index);
        }

        /// <summary>
        /// Stateless iteration primitive backing the 'next' stdlib function.
        /// A null or nil <paramref name="key"/> yields the first pair; otherwise the
        /// pair following <paramref name="key"/> in enumeration order. Order is
        /// unspecified but stable as long as the table is not mutated.
        /// </summary>
        /// <returns>False when iteration is finished (both outputs are nil)</returns>
        /// <exception cref="RuntimeException"><paramref name="key"/> is not present in the table</exception>
        public bool TryNext(LuaObject key, out LuaObject next_key, out LuaObject next_value)
        {
            bool found = key == null || key is NilObject;

            foreach (var pair in Elements)
            {
                if (found)
                {
                    next_key = pair.Key;
                    next_value = pair.Value;
                    return true;
                }

                if (pair.Key.Equals(key)) found = true;
            }

            if (!found)
                throw new RuntimeException("invalid key to 'next'");

            next_key = NilObject.Nil;
            next_value = NilObject.Nil;
            return false;
        }

        /// <summary>
        /// Insert array element at index and shift all elements
        /// </summary>
        /// <exception cref="RuntimeException">Index out of bounds</exception>
        public void Insert(LuaObject index, LuaObject value)
        {
            if (index is NumberObject num)
            {
                int i = (int)num;
                int len = (int)Len();

                if (i >= 1 && i <= len + 1)
                {
                    if (i <= len) //shift elements when inserting not at end.
                    {
                        for (int j = len + 1; j >= i; j--)
                        {
                            this[j] = this[j - 1];
                        }
                    }

                    this[i] = value;
                    return;
                }
            }

            throw new RuntimeException("Position out of bounds");
        }

        /// <summary>
        /// Delete array index and shift all elements
        /// </summary>
        /// <param name="index"></param>
        /// <returns>Deleted value at index</returns>
        /// <exception cref="System.Exception">Index out of bounds</exception>
        public LuaObject DeleteIndex(LuaObject index)
        {
            if (index is NumberObject num)
            {
                int i = (int)num;
                int len = (int)Len();

                if (i >= 1 && i <= len)
                {
                    var value = this[index];

                    //shift elements
                    for (int j = i; j < len; j++)
                    {
                        this[j] = this[j + 1];
                    }

                    this[len] = NilObject.Nil;

                    return value;
                }
            }

            throw new System.Exception("Position out of bounds");
        }

        /// <summary>
        /// Delete the first curring value in an array and shift all elements
        /// </summary>
        public LuaObject DeleteValue(LuaObject value)
        {
            int len = (int)Len();

            for (int i = 1; i <= len; i++)
            {
                var v = this[i];
                if (v == value)
                {
                    DeleteIndex(new NumberObject(i));
                    return v;
                }
            }

            return NilObject.Nil;
        }

        public override string ToString()
        {
            var str = new System.Text.StringBuilder();

            str.Append('{');

            foreach (var e in Elements)
            {
                str.Append(e.Key);
                str.Append(":");
                str.Append(e.Value);
                str.Append(", ");
            }

            str.Append('}');

            return str.ToString();
        }

        public override string TypeName => "table";

        public override StringObject ToStringObject() => new StringObject("[table]");

        // Header only: the entries bill themselves through the indexer.
        public override long ShallowMemoryCost() => HeaderMemoryCost;

        public NumberObject Len()
        {
            for (int i = 1; i <= Elements.Count; i++)
            {
                var num = new NumberObject(i);
                if (!Elements.ContainsKey(num))
                    return new NumberObject(i - 1);
            }

            return new NumberObject(Elements.Count);
        }
    }
}
