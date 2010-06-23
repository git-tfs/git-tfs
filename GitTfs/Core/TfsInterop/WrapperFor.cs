namespace Sep.Git.Tfs.Core.TfsInterop
{
    public class WrapperFor<T>
    {
        private readonly T _wrapped;

        public WrapperFor(T wrapped)
        {
            _wrapped = wrapped;
        }

        public T Unwrap()
        {
            return _wrapped;
        }

        public static T Unwrap(object wrapper)
        {
            return ((WrapperFor<T>)wrapper).Unwrap();
        }
    }
}
