using System;
using System.Collections.Generic;
using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core.TfsInterop;

namespace Sep.Git.Tfs.Core
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
        void Shelve(string shelvesetName, bool evaluateCheckinPolicies, Func<string> generateCheckinComment);
        /// <summary>
        /// Evaluates check-in policies and checks in all pending changes.
        /// </summary>
        long Checkin(CheckinOptions checkinOptions);
        /// <summary>
        /// Populates the workspace with a snapshot, as of the given changeset.
        /// </summary>
        void Get(int changesetId);
        /// <summary>
        /// Gets the files changed in a given changeset.
        /// </summary>
        void Get(IChangeset changeset);
        /// <summary>
        /// Gets the files changed in the given changes.
        /// </summary>
        void Get(int changesetId, IEnumerable<IChange> change);

        long CheckinTool(Func<string> generateCheckinComment);
        void Merge(string sourceTfsPath, string tfsRepositoryPath);

        /// <summary>
        /// Gets the remote for which this workspace was created.
        /// </summary>
        IGitTfsRemote Remote { get; }
    }
}
