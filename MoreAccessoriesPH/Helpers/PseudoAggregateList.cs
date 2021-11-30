using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoreAccessoriesPH.Helpers
{
    public class PseudoAggregateList<T> : IList<T>, IList
    {
        private readonly Func<int, T> _getFirst;
        private readonly Func<int, T> _getSecond;
        private readonly Action<int, T> _setFirst;
        private readonly Action<int, T> _setSecond;
        private readonly Func<int> _countFirst;
        private readonly Func<int> _countSecond;

        public PseudoAggregateList(Func<int, T> getFirst, Func<int, T> getSecond, 
                                   Action<int, T> setFirst, Action<int, T> setSecond,
                                   Func<int> countFirst, Func<int> countSecond)
        {
            this._getFirst = getFirst;
            this._getSecond = getSecond;
            this._setFirst = setFirst;
            this._setSecond = setSecond;
            this._countFirst = countFirst;
            this._countSecond = countSecond;
            this.Count = this._countFirst() + this._countSecond();
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
            throw new InvalidOperationException();
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
            throw new InvalidOperationException();
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
        public object SyncRoot { get { return this; } }
        public bool IsSynchronized { get { return false; } }
        public bool IsReadOnly { get { return false; } }
        public int IndexOf(T item)
        {
            throw new InvalidOperationException();
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
                if (index < this._countFirst())
                    return this._getFirst(index);
                index -= this._countFirst();
                return this._getSecond(index);
            }
            set
            {
                if (index < this._countFirst())
                    this._setFirst(index, value);
                else
                    this._setSecond(index - this._countFirst(), value);
            }
        }

        public struct Enumerator<T1> : IEnumerator<T1>, IEnumerable
        {
            private int _index;
            private readonly PseudoAggregateList<T1> _list;

            internal Enumerator(PseudoAggregateList<T1> list)
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
