
namespace GitTfs.Core.TfsInterop
{
    public class NullIdentity : IIdentity
    {
        public string MailAddress => null;

        public string DisplayName => null;
    }

    public class FakeIdentity : IIdentity
    {
        public string MailAddress { get; set; }

        public string DisplayName { get; set; }
    }
}