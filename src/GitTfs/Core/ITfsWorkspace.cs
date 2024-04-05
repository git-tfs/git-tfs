using GitTfs.Commands;
using GitTfs.Core.TfsInterop;

namespace GitTfs.Core
{
    /// <summary>
    /// Things needed by code that pends changes to a workspace.
    /// </summary>
    public interface ITfsWorkspaceModifier
    {
        string GetLocalPath(string path);
        void Add(string path);
        void Edit(string path);
        void Delete(string path);
        void Rename(string pathFrom, string pathTo, string score);
    }

    /// <summary>
    /// All the other tricks workspaces know.
    /// </summary>
    public interface ITfsWorkspace : ITfsWorkspaceModifier
    {
        /// <summary>
        /// Shelves all pending changes, with the given shelveset name.
        /// </summary>
        void Shelve(string shelvesetName, bool evaluateCheckinPolicies, CheckinOptions checkinOptions, Func<string> generateCheckinComment);
        /// <summary>
        /// Evaluates check-in policies and checks in all pending changes.
        /// </summary>
        int Checkin(CheckinOptions checkinOptions, Func<string> generateCheckinComment = null);
        /// <summary>
        /// Populates the workspace with a snapshot, as of the given changeset.
        /// </summary>
        void Get(int changesetId);

		/// <summary>
        /// Populates the workspace with specified items, as of the given changeset.
        /// </summary>
        void Get(int changesetId, IEnumerable<IItem> items);

        /// <summary>
        /// Gets the files changed in a given changeset.
        /// </summary>
        void Get(IChangeset changeset);
        /// <summary>
        /// Gets the files changed in the given changes.
        /// </summary>
        void Get(int changesetId, IEnumerable<IChange> change);
        /// <summary>
        /// Find path where the server item is mapped to in the
        /// local workspace.
        /// </summary>
        string GetLocalItemForServerItem(string serverItem);

        int CheckinTool(Func<string> generateCheckinComment);
        void Merge(string sourceTfsPath, string tfsRepositoryPath);

        void DeleteShelveset(string shelvesetName);

        /// <summary>
        /// Gets the remote for which this workspace was created.
        /// </summary>
        IGitTfsRemote Remote { get; }
    }
}
