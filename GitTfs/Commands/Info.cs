﻿using System;
using System.ComponentModel;
using System.IO;
using NDesk.Options;
using StructureMap;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("info")]
    [Description("info")]
    [RequiresValidGitRepository]
    public class Info : GitTfsCommand
    {
        Globals globals;
        TextWriter stdout;
        IGitTfsVersionProvider versionProvider;

        public Info(Globals globals, TextWriter stdout, IGitTfsVersionProvider versionProvider)
        {
            this.globals = globals;
            this.stdout = stdout;
            this.versionProvider = versionProvider;
        }

        public OptionSet OptionSet { get { return globals.OptionSet; } }

        public int Run()
        {
            DescribeGit();

            DescribeGitTfs();

            var tfsRemotes = globals.Repository.ReadAllTfsRemotes();
            foreach (var remote in tfsRemotes)
            {
                DescribeTfsRemotes(remote);
            }

            return GitTfsExitCodes.OK;
        }

        private void DescribeGit()
        {
            // add a line of whitespace to improve readability
            stdout.WriteLine();

            // show git version
            globals.Repository.CommandOutputPipe(reader => stdout.Write(reader.ReadToEnd()), "--version");
        }

        private void DescribeGitTfs()
        {
            // add a line of whitespace to improve readability
            stdout.WriteLine();
            stdout.WriteLine(versionProvider.GetVersionString());
            stdout.WriteLine(" " + versionProvider.GetPathToGitTfsExecutable());
        }

        private void DescribeTfsRemotes(IGitTfsRemote remote)
        {
            // add a line of whitespace to improve readability
            stdout.WriteLine();
            stdout.WriteLine("remote tfs id: '{0}' {1} {2}", remote.Id, remote.TfsUrl, remote.TfsRepositoryPath);
            stdout.WriteLine("               {0} - {1} @ {2}", remote.RemoteRef, remote.MaxCommitHash, remote.MaxChangesetId);
        }
    }
}
