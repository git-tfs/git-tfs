using GitTfs.Core;

namespace GitTfs.Util
{
    /// <summary>
    /// An object that can help initialize a repository or a remote for exporting
    /// TFS metadata
    /// </summary>
    public class ExportMetadatasInitializer
    {
        private readonly string exportMetadatasFilePath;

        public ExportMetadatasInitializer(Globals globals)
        {
            exportMetadatasFilePath = Path.Combine(globals.GitDir, "git-tfs_workitem_mapping.txt");
        }

        /// <summary>
        /// Initializes a repository to always export meta data
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="mappingFile"></param>
        public void InitializeConfig(IGitRepository repository, string mappingFile = null)
        {
            repository.SetConfig(GitTfsConstants.ExportMetadatasConfigKey, true);
            if (!string.IsNullOrEmpty(mappingFile))
            {
                if (File.Exists(mappingFile))
                {
                    File.Copy(mappingFile, exportMetadatasFilePath);
                }
                else
                    throw new GitTfsException("error: the work items mapping file doesn't exist!");
            }
        }

        /// <summary>
        /// Configures a IGitTfsRemote to export metadata
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="shouldExportMetadata"></param>
        public void InitializeRemote(IGitTfsRemote remote, bool shouldExportMetadata)
        {
            remote.ExportMetadatas = shouldExportMetadata;
            remote.ExportWorkitemsMapping = new Dictionary<string, IExportWorkItem>();

            if (shouldExportMetadata && File.Exists(exportMetadatasFilePath))
            {
                try
                {
                    foreach (var lineRead in File.ReadAllLines(exportMetadatasFilePath))
                    {
                        if (string.IsNullOrWhiteSpace(lineRead))
                            continue;
                        var values = lineRead.Split('|');
                        var oldWorkitem = values[0].Trim();
                        if (!remote.ExportWorkitemsMapping.ContainsKey(oldWorkitem))
                            remote.ExportWorkitemsMapping.Add(oldWorkitem,
                                new ExportWorkItem(values[1].Trim(), String.Empty /* TODO: Read workitem title from TFS here if necessary */));
                    }
                }
                catch (Exception)
                {
                    throw new GitTfsException("error: bad format of workitems mapping file! One line format should be: OldWorkItemId|NewWorkItemId");
                }
            }
        }
    }
}
