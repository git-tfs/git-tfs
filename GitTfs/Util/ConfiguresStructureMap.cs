using System;
using StructureMap;

namespace Sep.Git.Tfs.Util
{
    public interface ConfiguresStructureMap
    {
        void Initialize(IInitializationExpression initializer, Type t);
    }
}