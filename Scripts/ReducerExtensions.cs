using System;

namespace SpacetimeDB
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ReducerClassAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DeserializeEventAttribute : Attribute
    {
        public string? FunctionName { get; set; }
    }
}
