using StructureMap;

namespace GitTfs.Util
{
    public interface ConfiguresStructureMap
    {
        void Initialize(ConfigurationExpression initializer, Type t);
    }
}