using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Core
{
    public class RemoteConfigConverter
    {
        public IEnumerable<RemoteInfo> Load(IEnumerable<ConfigurationEntry> config)
        {
            var remotes = new Dictionary<string, RemoteInfo>();
            foreach (var entry in config)
            {
                var keyParts = entry.Key.Split('.');
                if (keyParts.Length == 3 && keyParts[0] == "tfs-remote")
                {
                    var id = keyParts[1];
                    var key = keyParts[2];
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
                    else if (key == "no-meta-data")
                        remote.NoMetaData = entry.Value.ToLower() != "false";
                    else if (key == "legacy-urls")
                        remote.Aliases = entry.Value.Split(',');
                    else if (key == "autotag")
                        remote.Autotag = bool.Parse(entry.Value);
                }
            }
            return remotes.Values;
        }

        public IEnumerable<ConfigurationEntry> Dump(RemoteInfo remote)
        {
            if (!string.IsNullOrWhiteSpace(remote.Id))
            {
                var prefix = "tfs-remote." + remote.Id + ".";
                yield return new ConfigurationEntry(prefix + "url", remote.Url);
                yield return new ConfigurationEntry(prefix + "repository", remote.Repository);
                yield return new ConfigurationEntry(prefix + "username", remote.Username);
                yield return new ConfigurationEntry(prefix + "password", remote.Password);
                yield return new ConfigurationEntry(prefix + "ignore-paths", remote.IgnoreRegex);
                yield return new ConfigurationEntry(prefix + "no-meta-data", remote.NoMetaData ? "true" : null);
                yield return new ConfigurationEntry(prefix + "legacy-urls", remote.Aliases == null ? null : string.Join(",", remote.Aliases));
                yield return new ConfigurationEntry(prefix + "autotag", remote.Autotag ? "true" : null);
            }
        }
    }
}
