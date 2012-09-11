using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace Sep.Git.Tfs.Core
{
    public class RemoteConfigReader
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
    }
}
