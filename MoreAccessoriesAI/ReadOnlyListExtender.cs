using System;
using System.Collections;
using System.Collections.Generic;

namespace MoreAccessoriesAI
{
    public class ReadOnlyListExtender<T> : IList<T>, IList
    {
        private readonly IList<T> _list1;
        private readonly IList<T> _list2;

        public ReadOnlyListExtender(IList<T> list1, IList<T> list2)
        {
            this._list1 = list1;
            this._list2 = list2;
            this.Count = this._list1.Count + this._list2.Count;
        } 
        
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(T item)
        {
            throw new InvalidOperationException();
        }

        public int Add(object value)
        {
            throw new InvalidOperationException();
        }

        public bool Contains(object value)
        {
            return this.Contains((T)value);
        }

        void IList.Clear()
        {
            throw new InvalidOperationException();
        }

        public int IndexOf(object value)
        {
            return this.IndexOf((T)value);
        }

        public void Insert(int index, object value)
        {
            throw new InvalidOperationException();
        }

        public void Remove(object value)
        {
            throw new InvalidOperationException();
        }

        void IList.RemoveAt(int index)
        {
            throw new InvalidOperationException();
        }

        object IList.this[int index] { get { return this[index]; } set { this[index] = (T)value; } }
        bool IList.IsReadOnly { get { return true; } }
        public bool IsFixedSize { get { return true; } }
        void ICollection<T>.Clear()
        {
            throw new InvalidOperationException();
        }

        public bool Contains(T item)
        {
            return this._list1.Contains(item) || this._list2.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new InvalidOperationException();
        }

        public bool Remove(T item)
        {
            throw new InvalidOperationException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new InvalidOperationException();
        }

        public int Count { get; }
        public object SyncRoot { get { return false; } }
        public bool IsSynchronized { get { return false; } }
        public bool IsReadOnly { get { return true; } }
        public int IndexOf(T item)
        {
            int index = this._list1.IndexOf(item);
            if (index == -1)
                index = this._list2.IndexOf(item);
            if (index != -1)
                index += this._list1.Count;
            return index;
        }

        public void Insert(int index, T item)
        {
            throw new InvalidOperationException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new InvalidOperationException();
        }

        public T this[int index]
        {
            get
            {
                if (index < this._list1.Count)
                    return this._list1[index];
                index -= this._list1.Count;
                return this._list2[index];
            }
            set
            {
                if (index < this._list1.Count)
                    this._list1[index] = value;
                else
                    this._list2[index - this._list1.Count] = value;
            }
        }

        public struct Enumerator<T1> : IEnumerator<T1>, IEnumerable
        {
            private int _index;
            private ReadOnlyListExtender<T1> _list;

            internal Enumerator(ReadOnlyListExtender<T1> list)
            {
                this._index = -1;
                this._list = list;
                this.Current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                this._index++;
                if (this._index < this._list.Count)
                {
                    this.Current = this._list[this._index];
                    return true;
                }
                this.Current = default;
                return false;
            }

            public void Reset()
            {
                this._index = -1;
                this.Current = default;
            }

            public T1 Current { get; private set; }
            object IEnumerator.Current
            {
                get { return this.Current; }
            }
            public IEnumerator GetEnumerator()
            {
                return this;
            }
        }

    }
}
