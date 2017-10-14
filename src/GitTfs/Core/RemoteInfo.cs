using System.Collections.Generic;
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
        public IEnumerable<string> Aliases { get; set; }
        public bool Autotag { get; set; }

        public RemoteOptions RemoteOptions
        {
            get { return new RemoteOptions { IgnoreRegex = IgnoreRegex, ExceptRegex = IgnoreExceptRegex, Username = Username, Password = Password }; }
            set { IgnoreRegex = value.IgnoreRegex; IgnoreExceptRegex = value.ExceptRegex; Username = value.Username; Password = value.Password; }
        }
    }
}
