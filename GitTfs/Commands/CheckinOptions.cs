using System.Collections.Generic;
using System.ComponentModel;
using CommandLine.OptParse;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [StructureMapSingleton]
    public class CheckinOptions
    {
        private List<string> _workItemsToAssociate = new List<string>();
        private List<string> _workItemsToResolve = new List<string>();

        [OptDef(OptValType.ValueReq)]
        [ShortOptionName('m')]
        [LongOptionName("comment")]
        [UseNameAsLongOption(false)]
        [Description("A comment for the changeset.")]
        public string CheckinComment { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("no-build-default-comment")]
        [UseNameAsLongOption(false)]
        [Description("Use the comments from the commits on the current branch to create a default checkin message (checkintool only)")]
                      // This can be extended to checkin when the $EDITOR is invoked.
        public bool GenerateCheckinComment { get; set; }

        [OptDef(OptValType.Flag)]
        [LongOptionName("no-merge")]
        [UseNameAsLongOption(false)]
        [Description("Omits setting commit being checked in as parent, thus allowing to rebase remaining onto TFS changeset without exceeding merge commits.")]
        public bool NoMerge { get; set; }

        private string _overrideReason;

        [OptDef(OptValType.ValueOpt)]
        [ShortOptionName('f')]
        [LongOptionName("force")]
        [UseNameAsLongOption(false)]
        [Description("To force a checkin, supply the policy override reason as an argument to this flag.")]
        public string OverrideReason
        {
            get { return _overrideReason; }
            set { Force = true; _overrideReason = value; }
        }

        public bool Force { get; set; }

        [OptDef(OptValType.MultValue, ValueType = typeof(string))]
        [ShortOptionName('w')]
        [LongOptionName("associated-work-item")]
        [UseNameAsLongOption(false)]
        public List<string> WorkItemsToAssociate { get { return _workItemsToAssociate; } }

        [OptDef(OptValType.MultValue, ValueType = typeof(string))]
        [LongOptionName("resolved-work-item")]
        [UseNameAsLongOption(false)]
        public List<string> WorkItemsToResolve { get { return _workItemsToResolve; } }
    }
}
