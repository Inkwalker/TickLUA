using System;
using System.Collections.Generic;

namespace TickLUA.Compilers.LUA
{
    /// <summary>
    /// Indexable stack implementation. It allows to access elements by index, where 0 is the top of the stack.
    /// </summary>
    internal class IndexableStack<T>
    {
        private List<T> values;

        public T this[int i]
        {
            get => values[values.Count - 1 - i];
            set => values[values.Count - 1 - i] = value;
        }

        public int Count => values.Count;

        public IndexableStack()
        {
            values = new List<T>();
        }

        public void Push(T value) 
        { 
            values.Add(value);
        }

        public T Pop()
        {
            if (values.Count == 0)
                throw new IndexOutOfRangeException();

            var val = values[values.Count - 1];
            values.RemoveAt(values.Count - 1);
            return val;
        }

        public T Peek()
        {
            if (values.Count == 0)
                throw new IndexOutOfRangeException();

            return values[values.Count - 1];
        }

        public void Clear()
        {
            values.Clear();
        }
    }
}
