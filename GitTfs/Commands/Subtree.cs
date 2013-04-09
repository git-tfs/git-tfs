using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("subtree")]
    [Description("subtree [add|pull|split] [options] [remote | ( [tfs-url] [repository-path] )]")]
    [RequiresValidGitRepository]
    public class Subtree : GitTfsCommand
    {
        
        private readonly TextWriter _stdout;
        private readonly Fetch _fetch;
        private readonly QuickFetch _quickFetch;
        private readonly Globals _globals;
        private readonly RemoteOptions _remoteOptions;

        private string Prefix;
        private bool Squash = false;

        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "p|prefix=",
                        v => Prefix = v},
                    { "squash",
                        v => Squash = v != null}
                    //                    { "r|revision=",
                    //                        v => RevisionToFetch = Convert.ToInt32(v) },
                }
                .Merge(_fetch.OptionSet);
            }
        }

        public Subtree(TextWriter stdout, Fetch fetch, QuickFetch quickFetch, Globals globals, RemoteOptions remoteOptions)
        {
            this._stdout = stdout;
            this._fetch = fetch;
            this._quickFetch = quickFetch;
            this._globals = globals;
            this._remoteOptions = remoteOptions;
        }

        public int Run(IList<string> args)
        {
            string command = args.FirstOrDefault() ?? "";
            _stdout.WriteLine("executing subtree " + command);

            if (string.IsNullOrEmpty(Prefix))
            {
                _stdout.WriteLine("Prefix must be specified, use -p or -prefix");
                return GitTfsExitCodes.InvalidArguments;
            }

            switch (command.ToLower())
            {
                case "add":
                    return DoAdd(args.ElementAtOrDefault(1) ?? "", args.ElementAtOrDefault(2) ?? "");
                    break;

                case "pull":
                    return DoPull(args.ElementAtOrDefault(1));


                default:
                    _stdout.WriteLine("Expected one of [add, pull, merge, split]");
                    return GitTfsExitCodes.InvalidArguments;
                    break;
            }
        }

        public int DoAdd(string tfsUrl, string tfsRepositoryPath)
        {
            if (File.Exists(Prefix) || Directory.Exists(Prefix))
            {
                _stdout.WriteLine(string.Format("Directory {0} already exists", Prefix));
                return GitTfsExitCodes.InvalidArguments;
            }


            var fetch = Squash ? this._quickFetch : this._fetch;

            bool didCreate = false;
            var tfsUri = new Uri(tfsUrl);
            IGitTfsRemote owner = _globals.Repository.ReadAllTfsRemotes().FirstOrDefault(x => string.IsNullOrEmpty(x.TfsRepositoryPath) && !x.Id.StartsWith("subtree/") && tfsUri.Equals(x.TfsUrl));
            if (owner == null)
            {
                owner = _globals.Repository.CreateTfsRemote(new RemoteInfo
                {
                    Id = "origin",
                    Url = tfsUrl,
                    Repository = null,
                    RemoteOptions = _remoteOptions
                });
                _stdout.WriteLine("-> new owning remote " + owner.Id);
                didCreate = true;
            }
            else
            {
                _stdout.WriteLine("Attaching subtree to owning remote " + owner.Id);
            }
            
            
            //create a remote for the new subtree
            string remoteId = "subtree/" + Prefix;
            IGitTfsRemote remote = _globals.Repository.HasRemote(remoteId) ? 
                _globals.Repository.ReadTfsRemote(remoteId) :
                _globals.Repository.CreateTfsRemote(new RemoteInfo
                 {
                     Id = remoteId,
                     Url = tfsUrl,
                     Repository = tfsRepositoryPath,
                     RemoteOptions = _remoteOptions,
                 });
            
            _stdout.WriteLine("-> new remote " + remote.Id);
            
            int result = fetch.Run(remote.Id);
            
            if (result == GitTfsExitCodes.OK)
            {

                var p = Prefix.Replace(" ", "\\ ");

                List<string> args = new List<string>(){"subtree", "add", 
                    "--prefix=" + p,
                    remote.RemoteRef
                    };
                command(args);

                //update the owner remote to point at the commit where the newly created subtree was merged.
                var commit = _globals.Repository.GetCurrentCommit();
                owner.UpdateRef(commit, remote.MaxChangesetId);

                result = GitTfsExitCodes.OK;
            }

            
            

            return result;
        }
    
        public int DoPull(string remoteId)
        {
            if (!Directory.Exists(Prefix))
            {
                _stdout.WriteLine("You must first add the subtree using 'git tfs subtree add -p=<prefix> [tfs-server] [tfs-repository]'");
                return GitTfsExitCodes.InvalidArguments;
            }

            remoteId = remoteId ?? "subtree/" + Prefix;
            IGitTfsRemote remote = _globals.Repository.ReadTfsRemote(remoteId);

            int result = this._fetch.Run(remote.Id);
            if (result == GitTfsExitCodes.OK)
            {
                var p = Prefix.Replace(" ", "\\ ");
                List<string> args = new List<string>(){ "subtree", "merge", "--prefix=" + p, remote.RemoteRef };
                command(args);
                result = GitTfsExitCodes.OK;
            }

            return result;
        }

        private void command(List<string> args)
        {
            _stdout.WriteLine("git " + string.Join(" ", args));
            _globals.Repository.CommandNoisy(args.ToArray());
        }
    }
}
