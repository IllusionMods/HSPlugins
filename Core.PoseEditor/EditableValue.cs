namespace HSPE
{
    public struct EditableValue<T> where T : struct
    {
        private T _value;
        private bool _hasValue;

        public T value
        {
            get { return _value; }
            set
            {
                _value = value;
                _hasValue = true;
            }
        }

        public bool hasValue { get { return _hasValue; } }

        public EditableValue(T v)
        {
            _value = v;
            _hasValue = true;
        }

        public void Reset()
        {
            _hasValue = false;
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
