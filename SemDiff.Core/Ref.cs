using System;

namespace SemDiff.Core
{
    public static class Ref
    {
        public static Ref<T> Create<T>(T initialValue)
        {
            return new Ref<T>
            {
                Value = initialValue,
            };
        }
    }

    public class Ref<T>
    {
        internal Ref()
        {
        }

        public T Value { get; set; }
    }
}