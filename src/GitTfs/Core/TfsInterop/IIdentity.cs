
namespace GitTfs.Core.TfsInterop
{
    public interface IIdentity
    {
        string MailAddress { get; }
        string DisplayName { get; }
    }
}