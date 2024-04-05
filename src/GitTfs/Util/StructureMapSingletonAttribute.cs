using StructureMap;

namespace GitTfs.Util
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StructureMapSingletonAttribute : Attribute, ConfiguresStructureMap
    {
        public void Initialize(ConfigurationExpression initializer, Type t) => initializer.For(t).Singleton().Use(t);
    }
}