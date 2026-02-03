using System.Collections.Generic;

namespace TickLUA.VM.Objects
{
    public class TableObject : LuaObject, IHasLen, IIndexable
    {
        public Dictionary<LuaObject, LuaObject> Elements { get; }

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
        }

        ///<summary> Key-value pair constructor </summary>
        public TableObject(IDictionary<LuaObject, LuaObject> elements)
        {
            Elements = new Dictionary<LuaObject, LuaObject>(elements);
        }

        ///<summary> Copy constructor </summary>
        public TableObject(TableObject other)
        {
            Elements = new Dictionary<LuaObject, LuaObject>(other.Elements);
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

        public override StringObject ToStringObject() => new StringObject("[table]");

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
