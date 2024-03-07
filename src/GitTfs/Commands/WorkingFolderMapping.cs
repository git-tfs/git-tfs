namespace GitTfs.Commands
{
    /// <summary>
    /// Source Control Folder to Local folder mapping
    /// </summary>
    /// <remarks>
    /// <para>This class is used by the deserialization of the JSON in the git tfs clone --MultipleWorkingFoldersConfigFilePath</para>
    /// </remarks>
    /// <see cref="https://learn.microsoft.com/en-us/azure/devops/repos/tfvc/create-work-workspaces?view=azure-devops"/>
    public class WorkingFolderMapping
    {
        /// <summary>
        /// The TFVC/Source Control Path.  Starting with a $/
        /// </summary>
        /// <example>
        /// $/Foo/Bar
        /// </example>
        public string SourceControlFolder { get; set; }

        /// <summary>
        /// The Local folder where any item under the the specified <see cref="SourceControlFolder"/> will be downloaded to.  Shorter the better.
        /// </summary>
        /// <example>
        /// C:\x\1
        /// </example>
        public string LocalFolder { get; set; }
    }
}
