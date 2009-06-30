using System;
using StructureMap;

namespace Sep.Git.Tfs
{
    public interface ConfiguresStructureMap
    {
        void Initialize(IInitializationExpression initializer, Type t);
    }
}