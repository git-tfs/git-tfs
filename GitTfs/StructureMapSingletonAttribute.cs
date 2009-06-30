using System;
using StructureMap;
using StructureMap.Attributes;

namespace Sep.Git.Tfs
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StructureMapSingletonAttribute : Attribute, ConfiguresStructureMap
    {
        public void Initialize(IInitializationExpression initializer, Type t)
        {
            initializer.ForRequestedType(t).CacheBy(InstanceScope.Singleton).TheDefaultIsConcreteType(t);
        }
    }
}
