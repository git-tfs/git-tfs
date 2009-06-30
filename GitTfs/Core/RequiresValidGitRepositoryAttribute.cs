using System;

namespace Sep.Git.Tfs.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RequiresValidGitRepositoryAttribute : Attribute
    {
    }
}
