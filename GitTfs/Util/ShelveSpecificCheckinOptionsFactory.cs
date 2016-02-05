using System.IO;
using Sep.Git.Tfs.Commands;

namespace Sep.Git.Tfs.Util
{
    public class ShelveSpecificCheckinOptionsFactory
    {
        private readonly TextWriter writer;
        private readonly Globals globals;

        public ShelveSpecificCheckinOptionsFactory(TextWriter writer, Globals globals)
        {
            this.writer = writer;
            this.globals = globals;
        }

        public CheckinOptions BuildShelveSetSpecificCheckinOptions(CheckinOptions sourceCheckinOptions,
            string commitMessage)
        {
            var customCheckinOptions = sourceCheckinOptions.Clone(this.globals);

            customCheckinOptions.CheckinComment = commitMessage;

            customCheckinOptions.ProcessWorkItemCommands(writer, false);

            return customCheckinOptions;
        }
    }
}
