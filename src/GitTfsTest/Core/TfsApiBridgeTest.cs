using GitTfs.Core.TfsInterop;
using GitTfs.Test;
using GitTfs.VsCommon;
using StructureMap.AutoMocking;
using Xunit;

namespace GitTfsTest.Core
{
    public class TfsApiBridgeTest : BaseTest
    {
        private readonly MoqAutoMocker<TfsApiBridge> _mocks;

        public TfsApiBridgeTest()
        {
            _mocks = new MoqAutoMocker<TfsApiBridge>();
            _mocks.MockObjectFactory();
        }

        [Fact]
        public void ConvertsEnum() => Assert.Equal(OriginalEnum.Value2, _mocks.ClassUnderTest.Convert<OriginalEnum>(WrappedEnum.Value2));

        [Fact]
        public void WrapsAndUnwrapsObject()
        {
            var originalObject = new OriginalType();
            var wrappedObject = _mocks.ClassUnderTest.Wrap<WrapperForOriginalType, OriginalType>(originalObject);
            Assert.Equal(originalObject, _mocks.ClassUnderTest.Unwrap<OriginalType>(wrappedObject));
        }

        [Fact]
        public void WrapsObjectWithBridge()
        {
            var originalObject = new OriginalType();
            var wrappedObject = _mocks.ClassUnderTest.Wrap<WrapperForOriginalTypeWithBridge, OriginalType>(originalObject);
            Assert.NotNull(wrappedObject.Bridge);
        }

        [Fact]
        public void WrapsAndUnwrapsArray()
        {
            var originalObjects = new[] { new OriginalType() };
            var wrappedObjects = _mocks.ClassUnderTest.Wrap<WrapperForOriginalType, OriginalType>(originalObjects);
            Assert.Single(wrappedObjects);
            Assert.Equal(originalObjects[0], _mocks.ClassUnderTest.Unwrap<OriginalType>(wrappedObjects)[0]);
        }

        [Fact]
        public void WrapsNullAsNull()
        {
            OriginalType obj = null;
            Assert.Null(_mocks.ClassUnderTest.Wrap<WrapperForOriginalType, OriginalType>(obj));
        }

        [Fact]
        public void WrapsNullArrayAsNull()
        {
            OriginalType[] obj = null;
            Assert.Null(_mocks.ClassUnderTest.Wrap<WrapperForOriginalType, OriginalType>(obj));
        }

        [Fact]
        public void UnwrapsNullAsNull()
        {
            WrapperForOriginalType obj = null;
            Assert.Null(_mocks.ClassUnderTest.Unwrap<OriginalType>(obj));
        }

        [Fact]
        public void UnwrapsNullArrayAsNull()
        {
            WrapperForOriginalType[] obj = null;
            Assert.Null(_mocks.ClassUnderTest.Unwrap<OriginalType>(obj));
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
            public override bool Equals(object obj) => obj is OriginalType && ((OriginalType)obj)._id == _id;
            public override int GetHashCode() => _id;
            public override string ToString() => "OriginalObject:" + _id;
        }
        private interface IOriginalType { }
        public class WrapperForOriginalType : WrapperFor<OriginalType>, IOriginalType
        {
            public WrapperForOriginalType(OriginalType o) : base(o) { }
        }
        public class WrapperForOriginalTypeWithBridge : WrapperFor<OriginalType>, IOriginalType
        {
            public WrapperForOriginalTypeWithBridge(OriginalType o, TfsApiBridge b) : base(o)
            {
                Bridge = b;
            }
            public TfsApiBridge Bridge { get; private set; }
        }

        public enum OriginalEnum
        {
            Value1,
            Value2,
        };

        public enum WrappedEnum
        {
            Value1,
            Value2,
        };
    }
}
