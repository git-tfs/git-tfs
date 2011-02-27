using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sep.Git.Tfs.Core.TfsInterop;
using Sep.Git.Tfs.VsCommon;
using StructureMap.AutoMocking;

namespace GitTfsTest.Vs2010
{
    [TestClass]
    public class TfsApiBridgeTest
    {
        private RhinoAutoMocker<TfsApiBridge> _mocks;

        [TestInitialize]
        public void Setup()
        {
            _mocks = new RhinoAutoMocker<TfsApiBridge>();
            _mocks.MockObjectFactory();
        }

        [TestMethod]
        public void ConvertsEnumToTfsEnum()
        {
            Assert.AreEqual(ItemType.Folder, _mocks.ClassUnderTest.Convert<ItemType>(TfsItemType.Folder));
        }

        [TestMethod]
        public void ConvertsTfsEnumToEnum()
        {
            Assert.AreEqual(TfsItemType.Folder, _mocks.ClassUnderTest.Convert<TfsItemType>(ItemType.Folder));
        }

        [TestMethod]
        public void WrapsAndUnwrapsObject()
        {
            var originalObject = new OriginalType();
            var wrappedObject = _mocks.ClassUnderTest.Wrap<WrapperForOriginalType, OriginalType>(originalObject);
            Assert.AreEqual(originalObject, _mocks.ClassUnderTest.Unwrap<OriginalType>(wrappedObject), "unwrap(wrap(object))");
        }

        [TestMethod]
        public void WrapsObjectWithBridge()
        {
            var originalObject = new OriginalType();
            var wrappedObject = _mocks.ClassUnderTest.Wrap<WrapperForOriginalTypeWithBridge, OriginalType>(originalObject);
            Assert.IsNotNull(wrappedObject.Bridge, "wrappedObject.Bridge");
        }

        [TestMethod]
        public void WrapsAndUnwrapsArray()
        {
            var originalObjects = new [] { new OriginalType() };
            var wrappedObjects = _mocks.ClassUnderTest.Wrap<WrapperForOriginalType, OriginalType>(originalObjects);
            Assert.AreEqual(1, wrappedObjects.Length, "wrappedObject.Length");
            Assert.AreEqual(originalObjects[0], _mocks.ClassUnderTest.Unwrap<OriginalType>(wrappedObjects)[0], "unwrap(wrap(objects))[0]");
        }

        [TestMethod]
        public void WrapsNullAsNull()
        {
            OriginalType obj = null;
            Assert.IsNull(_mocks.ClassUnderTest.Wrap<WrapperForOriginalType, OriginalType>(obj), "wrap(null)");
        }

        [TestMethod]
        public void WrapsNullArrayAsNull()
        {
            OriginalType [] obj = null;
            Assert.IsNull(_mocks.ClassUnderTest.Wrap<WrapperForOriginalType, OriginalType>(obj), "wrap(null[])");
        }

        [TestMethod]
        public void UnwrapsNullAsNull()
        {
            WrapperForOriginalType obj = null;
            Assert.IsNull(_mocks.ClassUnderTest.Unwrap<OriginalType>(obj), "unwrap(null)");
        }

        [TestMethod]
        public void UnwrapsNullArrayAsNull()
        {
            WrapperForOriginalType[] obj = null;
            Assert.IsNull(_mocks.ClassUnderTest.Unwrap<OriginalType>(obj), "unwrap(null[])");
        }

        public class OriginalType
        {
            public static int counter;
            public static object lockObject = new object();
            private readonly int _id;
            public OriginalType()
            {
                lock (lockObject)
                {
                    _id = ++counter;
                }
            }
            public override bool Equals(object obj)
            {
                return obj is OriginalType && ((OriginalType) obj)._id == _id;
            }
            public override int GetHashCode()
            {
                return _id;
            }
            public override string ToString()
            {
                return "OriginalObject:" + _id;
            }
        }
        interface IOriginalType {}
        public class WrapperForOriginalType : WrapperFor<OriginalType>, IOriginalType
        {
            public WrapperForOriginalType(OriginalType o) : base(o) {}
        }
        public class WrapperForOriginalTypeWithBridge : WrapperFor<OriginalType>, IOriginalType
        {
            public WrapperForOriginalTypeWithBridge(OriginalType o, TfsApiBridge b) : base(o)
            {
                Bridge = b;
            }
            public TfsApiBridge Bridge { get; private set; }
        }
    }
}
