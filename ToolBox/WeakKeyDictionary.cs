using System;
using System.Collections;
using System.Collections.Generic;

namespace ToolBox
{
	public class WeakReference<T> : WeakReference
	{
        public WeakReference(T reference) : base(reference) { }

        public new T Target { get { return (T)base.Target; } }
	}

    public class HashedWeakReference<T> : WeakReference<T>
    {
        private readonly int _hashCode;

        public HashedWeakReference(T reference) : base(reference)
        {
            this._hashCode = reference.GetHashCode();
        }

        public override int GetHashCode()
        {
            return this._hashCode;
        }

        public override bool Equals(object obj)
        {
            return obj != null && this._hashCode == obj.GetHashCode();
        }
    }

    public class WeakKeyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<HashedWeakReference<TKey>, TValue> _dictionary = new Dictionary<HashedWeakReference<TKey>, TValue>();

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        public void Remove(object key)
        {
            this._dictionary.Remove(new HashedWeakReference<TKey>((TKey)key));
        }

        public object this[object key] { get { return this._dictionary[new HashedWeakReference<TKey>((TKey)key)]; } set { this._dictionary[new HashedWeakReference<TKey>((TKey)key)] = (TValue)value; } }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this._dictionary.Add(new HashedWeakReference<TKey>(item.Key), item.Value);
        }

        public bool Contains(object key)
        {
            return false;
        }

        public void Add(object key, object value)
        {
            this._dictionary.Add(new HashedWeakReference<TKey>((TKey)key), (TValue)value);
        }

        public void Clear()
        {
            this._dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return false;
        }

        public int Count { get { return this._dictionary.Count; } }
        public bool IsReadOnly { get { return false; } }

        public bool ContainsKey(TKey key)
        {
            return this._dictionary.ContainsKey(new HashedWeakReference<TKey>(key));
        }

        public void Add(TKey key, TValue value)
        {
            this._dictionary.Add(new HashedWeakReference<TKey>(key), value);
        }

        public bool Remove(TKey key)
        {
            return this._dictionary.Remove(new HashedWeakReference<TKey>(key));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return this._dictionary.TryGetValue(new HashedWeakReference<TKey>(key), out value);
        }

        public TValue this[TKey key] { get { return this._dictionary[new HashedWeakReference<TKey>(key)]; } set { this._dictionary[new HashedWeakReference<TKey>(key)] = value; } }
        public ICollection<TKey> Keys { get { return null; } }
        public ICollection<TValue> Values { get { return this._dictionary.Values; } }

        public void Purge()
        {
            Dictionary<HashedWeakReference<TKey>, TValue> newDic = new Dictionary<HashedWeakReference<TKey>, TValue>();
            foreach (KeyValuePair<HashedWeakReference<TKey>, TValue> pair in this._dictionary)
            {
                if (pair.Key.IsAlive)
                    newDic.Add(pair.Key, pair.Value);
            }
            this._dictionary = newDic;
        }

        public class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly IEnumerator<KeyValuePair<HashedWeakReference<TKey>, TValue>> _enumerator;

            public Enumerator(WeakKeyDictionary<TKey, TValue> dictionary)
            {
                this._enumerator = dictionary._dictionary.GetEnumerator();
            }

            public void Dispose()
            {
                this._enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return this._enumerator.MoveNext();
            }

            public void Reset()
            {
                this._enumerator.Reset();
            }

            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    KeyValuePair<HashedWeakReference<TKey>, TValue> p = this._enumerator.Current;
                    return new KeyValuePair<TKey, TValue>(p.Key.Target, p.Value);
                }
            }
            object IEnumerator.Current { get { return this.Current; } }
        }
    }
}
