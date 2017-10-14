namespace GitTfs.Commands
{
    public class GitTfs // TODO: extract IGitTfs
    {
        public void FetchAll(string remoteId)
        {
            var remote = FindRemote(remoteId);
            //...
        }
    }
}
