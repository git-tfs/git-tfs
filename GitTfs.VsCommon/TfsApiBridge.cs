using System.Collections;
using System.Linq;
using Sep.Git.Tfs.Core.TfsInterop;
using StructureMap;

namespace Sep.Git.Tfs.VsCommon
{
    public class TfsApiBridge
    {
        private readonly IContainer _container;

        public TfsApiBridge(IContainer container)
        {
            _container = container;
        }

        public TWrapper Wrap<TWrapper, TWrapped>(TWrapped wrapped)
        {
            return _container.With(this).With(wrapped).GetInstance<TWrapper>();
        }

        public TWrapper[] Wrap<TWrapper, TWrapped>(IEnumerable wrapped)
        {
            return wrapped.OfType<TWrapped>().Select(x => Wrap<TWrapper, TWrapped>(x)).ToArray();
        }

        public TTfs Unwrap<TTfs>(object wrapper)
        {
            return ((WrapperFor<TTfs>) wrapper).Unwrap();
        }

        public TTfs[] Unwrap<TTfs>(IEnumerable wrappers)
        {
            return wrappers.Cast<object>().Select(x => Unwrap<TTfs>(x)).ToArray();
        }

        public TEnum Convert<TEnum>(object originalEnum)
        {
            return (TEnum) originalEnum;
        }
    }
}
