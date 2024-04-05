using LibGit2Sharp;

namespace GitTfs.Core
{
    public class RemoteConfigConverter
    {
        public IEnumerable<RemoteInfo> Load(IEnumerable<ConfigurationEntry<string>> config)
        {
            var remotes = new Dictionary<string, RemoteInfo>();
            foreach (var entry in config)
            {
                var keyParts = entry.Key.Split('.');
                if (keyParts.Length >= 3 && keyParts[0] == "tfs-remote")
                {
                    // The branch name may contain dots ("maint-1.0.0") which must be considered since split on "."
                    var id = string.Join(".", keyParts, 1, keyParts.Length - 2);
                    var key = keyParts.Last();
                    var remote = remotes.GetOrAdd(id);
                    remote.Id = id;
                    if (key == "url")
                        remote.Url = entry.Value;
                    else if (key == "repository")
                        remote.Repository = entry.Value;
                    else if (key == "username")
                        remote.Username = entry.Value;
                    else if (key == "password")
                        remote.Password = entry.Value;
                    else if (key == "ignore-paths")
                        remote.IgnoreRegex = entry.Value;
                    else if (key == "ignore-except")
                        remote.IgnoreExceptRegex = entry.Value;
                    else if (key == "gitignore-path")
                        remote.GitIgnorePath = entry.Value;
                    else if (key == "legacy-urls")
                        remote.Aliases = entry.Value.Split(',');
                    else if (key == "autotag")
                        remote.Autotag = bool.Parse(entry.Value);
                    else if (key == "noparallel")
                        remote.NoParallel = bool.Parse(entry.Value);
                }
            }
            return remotes.Values.Where(r => !string.IsNullOrWhiteSpace(r.Url));
        }

        public IEnumerable<KeyValuePair<string, string>> Dump(RemoteInfo remote)
        {
            if (!string.IsNullOrWhiteSpace(remote.Id))
            {
                var prefix = "tfs-remote." + remote.Id + ".";
                yield return c(prefix + "url", remote.Url);
                yield return c(prefix + "repository", remote.Repository);
                yield return c(prefix + "username", remote.Username);
                yield return c(prefix + "password", remote.Password);
                yield return c(prefix + "ignore-paths", remote.IgnoreRegex);
                yield return c(prefix + "ignore-except", remote.IgnoreExceptRegex);
                yield return c(prefix + "gitignore-path", remote.GitIgnorePath);
                yield return c(prefix + "legacy-urls", remote.Aliases == null ? null : string.Join(",", remote.Aliases));
                yield return c(prefix + "autotag", remote.Autotag ? "true" : null);
                yield return c(prefix + "noparallel", remote.NoParallel ? "true" : null);
            }
        }

        private KeyValuePair<string, string> c(string key, string value) => new KeyValuePair<string, string>(key, value);

        public IEnumerable<KeyValuePair<string, string>> Delete(string remoteId)
        {
            if (string.IsNullOrWhiteSpace(remoteId))
                return new List<KeyValuePair<string, string>>();

            return Dump(new RemoteInfo { Id = remoteId });
        }
    }
}
