using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolBox
{
    public struct EditablePair<T>
    {
        private T _originalValue;
        private bool _hasValue;

        public T currentValue;

        public T originalValue
        {
            get { return this._originalValue; }
            set
            {
                this._originalValue = value;
                this._hasValue = true;
            }
        }

        public bool hasValue { get { return this._hasValue; } }

        public EditablePair(T currentValue, T originalValue)
        {
            this.currentValue = currentValue;
            this._originalValue = originalValue;
            this._hasValue = true;
        } 

        public void Reset()
        {
            this._hasValue = false;
        }

        //public static implicit operator T(EditablePair<T> v)
        //{
        //    return v.currentValue;
        //}
    }

    public struct EditablePair<T, TOriginal>
    {
        private TOriginal _originalValue;
        private bool _hasValue;

        public T currentValue;

        public TOriginal originalValue
        {
            get { return this._originalValue; }
            set
            {
                this._originalValue = value;
                this._hasValue = true;
            }
        }

        public bool hasValue { get { return this._hasValue; } }

        public EditablePair(T currentValue, TOriginal originalValue)
        {
            this.currentValue = currentValue;
            this._originalValue = originalValue;
            this._hasValue = true;
        } 

        public void Reset()
        {
            this._hasValue = false;
        }

        //public static implicit operator T(EditablePair<T, TOriginal> v)
        //{
        //    return v.currentValue;
        //}
    }
}
