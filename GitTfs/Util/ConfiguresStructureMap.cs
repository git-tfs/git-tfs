using System;
using StructureMap;

namespace Sep.Git.Tfs.Util
{
    public interface ConfiguresStructureMap
    {
        void Initialize(ConfigurationExpression initializer, Type t);
    }
}