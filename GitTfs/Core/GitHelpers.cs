using System;

namespace Sep.Git.Tfs.Core
{
    public class GitHelpers : IGitHelpers
    {
        public string TryCommandOneline(params string[] command)
        {
            throw new System.NotImplementedException();
        }

        public string CommandOneline(params string[] command)
        {
            throw new System.NotImplementedException();
        }

        public void CommandNoisy(params string[] command)
        {
            throw new System.NotImplementedException();
        }

        public void Try(string exceptionMessage, Action action)
        {
            throw new System.NotImplementedException();
        }

        public IGitRepository MakeRepository(string dir)
        {
            throw new System.NotImplementedException();
        }
    }
}