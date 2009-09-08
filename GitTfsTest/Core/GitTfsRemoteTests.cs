using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Core;
using StructureMap.AutoMocking;

namespace Sep.Git.Tfs.Test.Core
{
    [TestClass]
    public class GitTfsRemoteTests
    {
        private RhinoAutoMocker<GitTfsRemote> mocks;

        [TestInitialize]
        public void Setup()
        {
            mocks = new RhinoAutoMocker<GitTfsRemote>();
        }
    }
}
