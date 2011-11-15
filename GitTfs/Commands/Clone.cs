using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommandLine.OptParse;
using Sep.Git.Tfs.Core;
using StructureMap;

namespace Sep.Git.Tfs.Commands
{
    [Pluggable("clone")]
    [Description("clone [options] tfs-url-or-instance-name repository-path <git-repository-path>")]
    public class Clone : GitTfsCommand
    {
        private readonly Fetch fetch;
        private readonly Init init;
        private readonly Globals globals;

        public Clone(Globals globals, Fetch fetch, Init init)
        {
            this.fetch = fetch;
            this.init = init;
            this.globals = globals;
        }

        public IEnumerable<IOptionResults> ExtraOptions
        {
            get { return this.MakeNestedOptionResults(init, fetch); }
        }

        public int Run(string tfsUrl, string tfsRepositoryPath)
        {
            return Run(tfsUrl, tfsRepositoryPath, Path.GetFileName(tfsRepositoryPath));
        }

        public int Run(string tfsUrl, string tfsRepositoryPath, string gitRepositoryPath)
        {
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
                // if we appeared to be inside repository dir when exception was thrown - we won't be able to delete it
                Environment.CurrentDirectory = currentDir; 
                if (repositoryDirCreated)
                    Directory.Delete(gitRepositoryPath, recursive: true);
                else
                    CleanDirectory(gitRepositoryPath);

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