using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using StructureMap;

namespace Sep.Git.Tfs.Core
{
    public class GitRepository : GitHelpers, IGitRepository
    {
        public string GitDir { get; set; }
        public string WorkingCopyPath { get; set; }
        public string WorkingCopySubdir { get; set; }

        protected override Process Start(string [] command, Action<ProcessStartInfo> initialize)
        {
            return base.Start(command, initialize.And(SetUpPaths));
        }

        private void SetUpPaths(ProcessStartInfo gitCommand)
        {
            if(GitDir != null)
                gitCommand.EnvironmentVariables["GIT_DIR"] = GitDir;
            if(WorkingCopyPath != null)
                gitCommand.WorkingDirectory = WorkingCopyPath;
            if(WorkingCopySubdir != null)
                gitCommand.WorkingDirectory = Path.Combine(gitCommand.WorkingDirectory, WorkingCopySubdir);
        }

        public IEnumerable<GitTfsRemote> ReadAllTfsRemotes()
        {
            return ReadTfsRemotes().Values;
        }

        public GitTfsRemote ReadTfsRemote(string remoteId)
        {
            try
            {
                return ReadTfsRemotes()[remoteId];
            }
            catch(Exception e)
            {
                throw new Exception("Unable to locate git-tfs remote with id = " + remoteId, e);
            }
        }

        private IDictionary<string, GitTfsRemote> ReadTfsRemotes()
        {
            var remotes = new Dictionary<string, GitTfsRemote>();
            CommandOutputPipe(stdout => ParseRemoteConfig(stdout, remotes), "config", "-l");
            return remotes;
        }

        private void ParseRemoteConfig(TextReader stdout, IDictionary<string, GitTfsRemote> remotes)
        {
            var configLineRegex = new Regex("^tfs-remote\\.(?<id>[^.]+)\\.(?<key>[^.=]+)=(?<value>.*)$");
            string line;
            while ((line = stdout.ReadLine()) != null)
            {
                var match = configLineRegex.Match(line);
                if (match.Success)
                {
                    var remoteId = match.Groups["id"].Value;
                    var key = match.Groups["key"].Value;
                    var value = match.Groups["value"].Value;
                    var remote = remotes.ContainsKey(remoteId)
                                     ? remotes[remoteId]
                                     : (remotes[remoteId] = CreateRemote(remoteId));
                    switch (key)
                    {
                        case "url":
                            remote.Tfs.Url = value;
                            break;
                        case "username":
                            remote.Tfs.Username = value;
                            break;
                        case "repository":
                            remote.TfsRepositoryPath = value;
                            break;
                        //case "fetch":
                        //    remote.??? = value;
                        //    break;
                    }
                }
            }
        }

        private GitTfsRemote CreateRemote(string id)
        {
            var remote = ObjectFactory.GetInstance<GitTfsRemote>();
            remote.Repository = this;
            remote.Id = id;
            return remote;
        }

        // This is attractive, but I'm wary of encoding/buffering issues due
        // to pulling BaseStream out of stdin. It also breaks my abstraction of
        // using TextWriter for stdin.
        // An alternative is to write the stream to a temp file, and call the
        // string-based HAIO.
        public string HashAndInsertObject(Stream file)
        {
            string newHash = null;
            CommandInputOutputPipe((stdin, stdout) => newHash = HashAndInsertObject(stdin, stdout, file),
                "has-object", "-w", "--stdin");
            return newHash;
        }
        private string HashAndInsertObject(TextWriter stdin, TextReader stdout, Stream file)
        {
            file.CopyTo(((StreamWriter) stdin).BaseStream);
            return stdout.ReadLine().Trim();
        }

        public string HashAndInsertObject(string filename)
        {
            string newHash = null;
            CommandInputOutputPipe((stdin, stdout) => newHash = HashAndInsertObject(stdin, stdout, filename),
                "has-object", "-w", "--stdin-paths");
            return newHash;
        }

        private string HashAndInsertObject(TextWriter stdin, TextReader stdout, string filename)
        {
            stdin.WriteLine(filename);
            return stdout.ReadLine().Trim();
        }
    }
}
