﻿using System;
using StructureMap;
using StructureMap.Attributes;

namespace Sep.Git.Tfs.Util
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StructureMapSingletonAttribute : Attribute, ConfiguresStructureMap
    {
        public void Initialize(ConfigurationExpression initializer, Type t)
        {
            initializer.For(t).Singleton().Use(t);
        }
    }
}