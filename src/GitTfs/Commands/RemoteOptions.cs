using GitTfs.Util;
using NDesk.Options;

namespace GitTfs.Commands
{
    [StructureMapSingleton]
    public class RemoteOptions
    {
        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "ignore-regex=", "A regex of files to ignore",
                        v => IgnoreRegex = v },
                    { "except-regex=", "A regex of exceptions to '--ignore-regex'",
                        v => ExceptRegex = v},
                    { "gitignore:", "Use .gitignore to ignore files on download from TFS. Alternatively, provide path toward the .gitignore file which will be used to ignore files",
                        v => { GitIgnorePath = v; UseGitIgnore = true; } },
                    { "no-gitignore", "Do not use .gitignore to ignore files on download from TFS",
                        v => NoGitIgnore = v != null },
                    { "u|username=", "TFS username",
                        v => Username = v },
                    { "p|password=", "TFS password",
                        v => Password = v },
                    { "no-parallel", "Do not do parallel requests to TFS",
                        v => NoParallel = (v != null) },
                    { "MultipleWorkingFoldersConfigFilePath=", "Pass the path to a JSON file containing the workplace's folder mapping. Try the '--workspace' workaround first, if that's not enough. pass a json file containing a list of Working Folders mappings.  See online docs for details and for json schema.",
                        v => MultipleWorkingFoldersConfigFilePath = v},
                };
            }
        }

        public string IgnoreRegex { get; set; }
        public string ExceptRegex { get; set; }
        public string GitIgnorePath { get; set; }
        public bool UseGitIgnore { get; set; }
        public bool NoGitIgnore { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool NoParallel { get; set; }

        /// <summary>
        /// Workaround/Hack for the TF400889/TF205022.  Path to a json file containing mappings between [Source Control Folder] and [Local Folder]
        /// </summary>
        /// <remarks>
        /// <para>When you encounter a Path too long error TF400889/TF205022, the first step is try specifying a short <see cref="WorkspacePath"/>.  
        /// If even that fails, then the only workaround is to map the failing source control folder ($/x/y/z) to a different, short, local folder (c:\x\1)
        /// <seealso cref="https://learn.microsoft.com/en-us/azure/devops/repos/tfvc/create-work-workspaces?view=azure-devops&redirectedfrom=MSDN&f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(vs.tfc.sourcecontrol.DialogEditWorkspace)%26rd%3Dtrue#q-why-would-i-need-to-change-the-working-folders-how-should-i-do-it"/>
        /// </para>
        /// </remarks>
        public string MultipleWorkingFoldersConfigFilePath { get; set; }
    }
}
