namespace HSPE
{
    public struct EditableValue<T> where T : struct
    {
        private T _value;
        private bool _hasValue;

        public T value
        {
            get { return this._value; }
            set
            {
                this._value = value;
                this._hasValue = true;
            }
        }

        public bool hasValue { get { return this._hasValue; } }

        public EditableValue(T v)
        {
            this._value = v;
            this._hasValue = true;
        } 

        public void Reset()
        {
            this._hasValue = false;
        }

        public static implicit operator T(EditableValue<T> v)
        {
            return v._value;
        }

        public static implicit operator EditableValue<T>(T v)
        {
            return new EditableValue<T>(v);
        }
    }
}
