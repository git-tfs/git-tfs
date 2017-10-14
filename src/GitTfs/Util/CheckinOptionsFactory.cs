using Sep.Git.Tfs.Commands;
using Sep.Git.Tfs.Core;

namespace Sep.Git.Tfs.Util
{
    /// <summary>
    /// Creates a new <see cref="CheckinOptions"/> that is customized based 
    /// on extracting special git-tfs commands from a git commit message.
    /// </summary>
    /// <remarks>
    /// This class handles the pre-checkin commit message parsing that
    /// enables special git-tfs commands: 
    /// https://github.com/git-tfs/git-tfs/blob/master/doc/Special-actions-in-commit-messages.md
    /// </remarks>
    public class CheckinOptionsFactory
    {
        private readonly Globals _globals;

        public CheckinOptionsFactory(Globals globals)
        {
            _globals = globals;
        }

        public CheckinOptions BuildCommitSpecificCheckinOptions(CheckinOptions sourceCheckinOptions, string commitMessage)
        {
            var customCheckinOptions = sourceCheckinOptions.Clone(_globals);

            customCheckinOptions.CheckinComment = commitMessage;

            customCheckinOptions.ProcessWorkItemCommands();

            customCheckinOptions.ProcessCheckinNoteCommands();

            customCheckinOptions.ProcessForceCommand();

            return customCheckinOptions;
        }

        public CheckinOptions BuildCommitSpecificCheckinOptions(CheckinOptions sourceCheckinOptions,
            string commitMessage, GitCommit commit, AuthorsFile authors)
        {
            var customCheckinOptions = BuildCommitSpecificCheckinOptions(sourceCheckinOptions, commitMessage);

            customCheckinOptions.ProcessAuthor(commit, authors);

            return customCheckinOptions;
        }

        public CheckinOptions BuildShelveSetSpecificCheckinOptions(CheckinOptions sourceCheckinOptions,
            string commitMessage)
        {
            var customCheckinOptions = sourceCheckinOptions.Clone(_globals);

            customCheckinOptions.CheckinComment = commitMessage;

            customCheckinOptions.ProcessWorkItemCommands(false);

            return customCheckinOptions;
        }
    }
}
