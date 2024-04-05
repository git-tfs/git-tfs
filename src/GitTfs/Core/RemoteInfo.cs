using GitTfs.Commands;

namespace GitTfs.Core
{
    public class RemoteInfo
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Repository { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string IgnoreRegex { get; set; }
        public string IgnoreExceptRegex { get; set; }
        public string GitIgnorePath { get; set; }
        public IEnumerable<string> Aliases { get; set; }
        public bool Autotag { get; set; }
        public bool NoParallel { get; set; }

        public RemoteOptions RemoteOptions
        {
            get => new RemoteOptions { IgnoreRegex = IgnoreRegex, ExceptRegex = IgnoreExceptRegex, GitIgnorePath = GitIgnorePath, Username = Username, Password = Password, NoParallel = NoParallel };
            set { IgnoreRegex = value.IgnoreRegex; IgnoreExceptRegex = value.ExceptRegex; GitIgnorePath = value.GitIgnorePath; Username = value.Username; Password = value.Password; NoParallel = value.NoParallel; }
        }
    }
}
