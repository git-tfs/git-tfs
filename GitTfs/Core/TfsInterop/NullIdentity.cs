namespace Sep.Git.Tfs.Core.TfsInterop
{
    public class NullIdentity : IIdentity
    {
        public string MailAddress
        {
            get { return null; }
        }

        public string DisplayName
        {
            get { return null; }
        }
    }

    public class FakeIdentity : IIdentity
    {
        public string MailAddress { get; set; }

        public string DisplayName { get; set; }
    }

}