using CommandLine.OptParse;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [StructureMapSingleton]
    public class CleanupOptions
    {
        private readonly Globals _globals;

        public CleanupOptions(Globals globals)
        {
            _globals = globals;
        }

        [OptDef(OptValType.Flag)]
        [ShortOptionName('v')]
        [LongOptionName("verbose")]
        [UseNameAsLongOption(false)]
        public bool IsVerbose { get; set; }

        public void Init()
        {
            if (IsVerbose)
                _globals.DebugOutput = true;
        }
    }
}