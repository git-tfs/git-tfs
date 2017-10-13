
namespace Sep.Git.Tfs.Core.TfsInterop
{
    public class WrapperFor<TFS_TYPE>
    {
        private readonly TFS_TYPE _wrapped;

        public WrapperFor(TFS_TYPE wrapped)
        {
            _wrapped = wrapped;
        }

        public TFS_TYPE Unwrap()
        {
            return _wrapped;
        }

        public static TFS_TYPE Unwrap(object wrapper)
        {
            return ((WrapperFor<TFS_TYPE>)wrapper).Unwrap();
        }
    }
}
