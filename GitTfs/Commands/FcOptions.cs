using System.ComponentModel;
using CommandLine.OptParse;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [StructureMapSingleton]
    public class FcOptions
    {
        [OptDef(OptValType.Flag)]
        [LongOptionName("follow-parent")]
        [LongOptionName("follow")]
        [UseNameAsLongOption(false)]
        [Description("Tracks the cloned element through renames outside of the specified repository path (enabled by default).")]
        public bool FollowParent
        {
            get { return !NoFollowParent; }
            set { NoFollowParent = !value; }
        }

        [OptDef(OptValType.Flag)]
        [LongOptionName("no-follow-parent")]
        [LongOptionName("no-follow")]
        [UseNameAsLongOption(false)]
        [Description("Does not track the cloned element through renames outside of the specified repository path.")]
        public bool NoFollowParent { get; set; }

// probably not used
//        [OptDef(OptValType.ValueReq)]
//        [LongOptionName("authors-file")]
//        [ShortOptionsName('A')]
//        [UseNameAsLongOption(false)]
//        [Description("Looks up TFS committers in the specified file, and halts if the author is not found.")]
//        public string AuthorsFile { get; set; }

// I think these are SVN-specific.
//        public bool NoMetadata { get; set; }
//        public bool UseSvmProps { get; set; }
//        public bool useSnvsyncProps { get; set; }
// I don't know what this is
//        public int LogWindowSize { get; set; }
//        public bool NoCheckout { get; set; }
        
        [OptDef(OptValType.IncrementalFlag)]
        [ShortOptionName('q')]
        [LongOptionName("quiet")]
        [UseNameAsLongOption(false)]
        [Description("Reduce the amount of logged information.")]
        public int OutputLevel { get; set; }

        // I think I'm going to make these the default. I may allow their disablement
        // later.
        //use-log-author
        //public bool UseLogAuthor { get; set; }
        //add-author-from
        //public bool AddAuthorFrom { get; set; }

    }
}
