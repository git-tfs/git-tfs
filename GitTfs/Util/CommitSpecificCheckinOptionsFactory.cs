using System.IO;
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
    public class CommitSpecificCheckinOptionsFactory
    {
        private readonly TextWriter writer;
        private readonly AuthorsFile authors;
        private readonly Globals globals;

        public CommitSpecificCheckinOptionsFactory(TextWriter writer, Globals globals, AuthorsFile authors)
        {
            this.writer = writer;
            this.authors = authors;
            this.globals = globals;
        }

        public CheckinOptions BuildCommitSpecificCheckinOptions(CheckinOptions sourceCheckinOptions, string commitMessage)
        {
            var customCheckinOptions = sourceCheckinOptions.Clone(this.globals);

            customCheckinOptions.CheckinComment = commitMessage;

            customCheckinOptions.ProcessWorkItemCommands(writer);

            customCheckinOptions.ProcessCheckinNoteCommands(writer);

            customCheckinOptions.ProcessForceCommand(writer);

            return customCheckinOptions;
        }

        public CheckinOptions BuildCommitSpecificCheckinOptions(CheckinOptions sourceCheckinOptions, string commitMessage, GitCommit commit)
        {
            var customCheckinOptions = BuildCommitSpecificCheckinOptions(sourceCheckinOptions, commitMessage);

            customCheckinOptions.ProcessAuthor(writer, commit, authors);

            return customCheckinOptions;
        }
    }
}
