namespace SemDiff.Core
{
    internal static class Ref
    {
        public static Ref<T> Create<T>(T initialValue)
        {
            return new Ref<T>
            {
                Value = initialValue,
            };
        }
    }

    internal class Ref<T>
    {
        internal Ref()
        {
        }

        public T Value { get; set; }
    }
}