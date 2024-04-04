
namespace GitTfs.Core.TfsInterop
{
    public class WrapperFor<TFS_TYPE>
    {
        private readonly TFS_TYPE _wrapped;

        public WrapperFor(TFS_TYPE wrapped)
        {
            _wrapped = wrapped;
        }

        public TFS_TYPE Unwrap() => _wrapped;

        public static TFS_TYPE Unwrap(object wrapper) => ((WrapperFor<TFS_TYPE>)wrapper).Unwrap();
    }
}
