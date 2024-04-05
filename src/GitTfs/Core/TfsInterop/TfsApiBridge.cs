using System.Collections;

using GitTfs.Core.TfsInterop;

using StructureMap;

namespace GitTfs.VsCommon
{
    public class TfsApiBridge
    {
        private readonly IContainer _container;

        public TfsApiBridge(IContainer container)
        {
            _container = container;
        }

        public TWrapper Wrap<TWrapper, TWrapped>(TWrapped wrapped) where TWrapper : class => wrapped == null ? null : _container.With(this).With(wrapped).GetInstance<TWrapper>();

        public TWrapper[] Wrap<TWrapper, TWrapped>(IEnumerable wrapped) where TWrapper : class => wrapped == null ? null : wrapped.OfType<TWrapped>().Select(x => Wrap<TWrapper, TWrapped>(x)).ToArray();

        public TTfs Unwrap<TTfs>(object wrapper) where TTfs : class => wrapper == null ? null : (wrapper is TTfs ? (TTfs)wrapper : ((WrapperFor<TTfs>)wrapper).Unwrap());

        public TTfs[] Unwrap<TTfs>(IEnumerable wrappers) where TTfs : class => wrappers == null ? null : wrappers.Cast<object>().Select(x => Unwrap<TTfs>(x)).ToArray();

        public TEnum Convert<TEnum>(object originalEnum) => (TEnum)originalEnum;
    }
}
