using System;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    [Obsolete]
    public class TestClassAttribute : Attribute
    { }

    [Obsolete]
    public class TestInitializeAttribute : Attribute
    { }

    [Obsolete]
    public class TestCleanup : Attribute
    { }

    [Obsolete]
    public class TestMethodAttribute : Attribute
    { }

    [Obsolete]
    public class IgnoreAttribute : Attribute
    { }

    [Obsolete]
    public class Assert
    {
        [Obsolete]
        public static void AreEqual(params object[] args) { }

        [Obsolete]
        public static void AreNotEqual(params object[] args) { }

        [Obsolete]
        public static void IsFalse(params object[] args) { }

        [Obsolete]
        public static void IsTrue(params object[] args) { }

        [Obsolete]
        public static void IsInstanceOfType(params object[] args) { }

        [Obsolete]
        public static void AreSame(params object[] args) { }
    }

    [Obsolete]
    public class CollectionAssert : Assert
    { }
}