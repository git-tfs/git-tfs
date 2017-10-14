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

        public TWrapper Wrap<TWrapper, TWrapped>(TWrapped wrapped) where TWrapper : class
        {
            return wrapped == null ? null : _container.With(this).With(wrapped).GetInstance<TWrapper>();
        }

        public TWrapper[] Wrap<TWrapper, TWrapped>(IEnumerable wrapped) where TWrapper : class
        {
            return wrapped == null ? null : wrapped.OfType<TWrapped>().Select(x => Wrap<TWrapper, TWrapped>(x)).ToArray();
        }

        public TTfs Unwrap<TTfs>(object wrapper) where TTfs : class
        {
            return wrapper == null ? null : ((WrapperFor<TTfs>)wrapper).Unwrap();
        }

        public TTfs[] Unwrap<TTfs>(IEnumerable wrappers) where TTfs : class
        {
            return wrappers == null ? null : wrappers.Cast<object>().Select(x => Unwrap<TTfs>(x)).ToArray();
        }

        public TEnum Convert<TEnum>(object originalEnum)
        {
            return (TEnum)originalEnum;
        }
    }
}
