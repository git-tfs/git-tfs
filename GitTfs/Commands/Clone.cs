using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NDesk.Options;
using Sep.Git.Tfs.Core;
using StructureMap;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("clone")]
    [Description("clone [options] tfs-url-or-instance-name repository-path <git-repository-path>")]
    public class Clone : GitTfsCommand
    {
        private readonly Fetch fetch;
        private readonly Init init;
        private readonly Globals globals;

        string InitialChangesetNumber { get; set; }

        public Clone(Globals globals, Fetch fetch, Init init)
        {
            this.fetch = fetch;
            this.init = init;
            this.globals = globals;
        }

        public OptionSet OptionSet
        {
            get
            {
                return
                new OptionSet()
                {
                    { "initchangeset=", "the initial changeset number to start with when pulling down a clone for the first time",
                        v => InitialChangesetNumber = v}, 
                }.Merge(init.OptionSet, fetch.OptionSet);
            }
        }

        public int Run(string tfsUrl, string tfsRepositoryPath)
        {
            return Run(tfsUrl, tfsRepositoryPath, Path.GetFileName(tfsRepositoryPath));
        }

        public int Run(string tfsUrl, string tfsRepositoryPath, string gitRepositoryPath)
        {
            int startingChangesetNumber; 
            if (!String.IsNullOrWhiteSpace(InitialChangesetNumber))
            {
                if (!int.TryParse(this.InitialChangesetNumber, out startingChangesetNumber))
                {
                    throw new GitTfsException("The initial changeset number was not a valid number!.  Please enter a valid number for the initial changeset value.");
                }
                else if (startingChangesetNumber > 0)
                {
                    fetch.StartWithChangesetId = startingChangesetNumber;
                }
            }

            var currentDir = Environment.CurrentDirectory;
            var repositoryDirCreated = InitGitDir(gitRepositoryPath);

            try
            {
                var retVal = 0;
                retVal = init.Run(tfsUrl, tfsRepositoryPath, gitRepositoryPath);
                if (retVal == 0) retVal = fetch.Run();
                if (retVal == 0) globals.Repository.CommandNoisy("merge", globals.Repository.ReadAllTfsRemotes().First().RemoteRef);
                return retVal;
            }
            catch
            {
                try
                {
                    // if we appeared to be inside repository dir when exception was thrown - we won't be able to delete it
                    Environment.CurrentDirectory = currentDir;
                    if (repositoryDirCreated)
                        Directory.Delete(gitRepositoryPath, recursive: true);
                    else
                        CleanDirectory(gitRepositoryPath);
                }
                catch (IOException e)
                {
                    // swallow IOException. Smth went wrong before this and we're much more interested in that error
                    string msg = String.Format("Something went wrong, can't cleanup files because of IOException:\n{0}\n", e.IndentExceptionMessage());
                    Trace.WriteLine(msg);
                }
                catch (UnauthorizedAccessException e)
                {
                    // swallow it also
                    string msg = String.Format("Something went wrong, can't cleanup files because of UnauthorizedAccessException:\n{0}\n", e.IndentExceptionMessage());
                    Trace.WriteLine(msg);
                }

                throw;
            }
        }

        private static bool InitGitDir(string gitRepositoryPath)
        {
            bool repositoryDirCreated = false;
            var di = new DirectoryInfo(gitRepositoryPath);
            if (di.Exists)
            {
                if (di.EnumerateFileSystemInfos().Any())
                    throw new GitTfsException("error: Specified git repository directory is not empty");
            }
            else
            {
                repositoryDirCreated = true;
                di.Create();
            }
            return repositoryDirCreated;
        }

        private static void CleanDirectory(string gitRepositoryPath)
        {
            var di = new DirectoryInfo(gitRepositoryPath);
            foreach (var fileSystemInfo in di.EnumerateDirectories())
                fileSystemInfo.Delete(true);
            foreach (var fileSystemInfo in di.EnumerateFiles())
                fileSystemInfo.Delete();
        }
    }
}
