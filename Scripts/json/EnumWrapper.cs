using System.Collections;
using System.Collections.Generic;

namespace SpacetimeDB 
{
    public class EnumWrapper<T>
    {
        public T Value { get; set; }

        public EnumWrapper(T value)
        {
            Value = value;
        }
    }
}
