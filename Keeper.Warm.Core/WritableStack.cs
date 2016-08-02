using System;
using System.Collections;
using System.Collections.Generic;

namespace Keeper.Warm
{
    public class WritableStack<T>
        : IEnumerable<T>, ICollection
    {
        private List<T> values;

        public int Count
        {
            get
            {
                return ((ICollection)this.values).Count;
            }
        }

        public object SyncRoot
        {
            get
            {
                return ((ICollection)this.values).SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return ((ICollection)this.values).IsSynchronized;
            }
        }

        public T this[int index]
        {
            get
            {
                return this.values[index];
            }
            set
            {
                this.values[index] = value;
            }
        }

        public WritableStack(IEnumerable<T> initial)
        {
            this.values = new List<T>(initial);
        }

        public void Push(T value)
        {
            this.values.Add(value);
        }

        public T Pop()
        {
            int lastIndex = this.values.Count - 1;
            T result = this.values[lastIndex];

            this.values.RemoveAt(lastIndex);

            return result;
        }

        public T Peek()
        {
            int lastIndex = this.values.Count - 1;

            return this.values[lastIndex];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)this.values).CopyTo(array, index);
        }
    }
}
