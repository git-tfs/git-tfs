using System.Collections.Generic;
using System.Text.RegularExpressions;
using NDesk.Options;
using Sep.Git.Tfs.Util;

namespace Sep.Git.Tfs.Commands
{
    [StructureMapSingleton]
    public class CheckinOptions
    {
        public OptionSet OptionSet
        {
            get
            {
                return new OptionSet
                {
                    { "m|comment=", "A comment for the changeset",
                        v => CheckinComment = v },
                    { "no-build-default-comment", "Do not concatenate commit comments for the changeset comment.",
                        v => NoGenerateCheckinComment = v != null },
                    { "no-merge", "Omits setting commit being checked in as parent, thus allowing to rebase remaining onto TFS changeset without exceeding merge commits.",
                        v => NoMerge = v != null },
                    { "f|force=", "The policy override reason.",
                        v => { Force = true; OverrideReason = v; } },
                    { "w|work-item:", "Associated work items\ne.g. -w12345 to associate with 12345\nor -w12345:resolve to resolve 12345",
                        (n, opt) => { if(n == null) throw new OptionException("Missing work item number for option -w.", "-w");
                            (opt == "resolve" ? WorkItemsToResolve : WorkItemsToAssociate).Add(n); } },
                    { "c|code-reviewer=", "Set code reviewer\ne.g. -c \"John Smith\"",
                        (reviewer) => { CheckinNotes["Code Reviewer"] = reviewer; } },
                    { "s|security-reviewer=", "Set security reviewer\ne.g. -s \"John Smith\"",
                        (reviewer) => { CheckinNotes["Security Reviewer"] = reviewer; } },
                    { "p|performance-reviewer=", "Set performance reviewer\ne.g. -p \"John Smith\"",
                        (reviewer) => { CheckinNotes["Performance Reviewer"] = reviewer; } },
                    { "n|checkin-note=:", "Add checkin note\ne.g. -n \"Unit Test Reviewer\" \"John Smith\"",
                        (key, value) => { CheckinNotes[key] = value; } },
                    { "no-gate", "Disables gated checkin.",
                        v => { OverrideGatedCheckIn = true; } },
                    { "author=", "TFS User ID to check in on behalf of", var => AuthorTfsUserId = var },
                    { "ignore-missing-files", "When applying an edit operation, ignores items that were expected to already be in TFS but were not found. Missing files indicates bad synchronization at some point. Use this option with care.",
                        v => IgnoreMissingItems = v != null },
                    { "add-missing-files", "When applying an edit operation, add items that were expected to already be in TFS but were not found. Missing files indicates bad synchronization at some point. Use this option with care. This option is ignored if ignore-missing-files option is used.",
                        v => AddMissingItems = v != null },
                };
            }
        }

        private List<string> _workItemsToAssociate = new List<string>();
        private List<string> _workItemsToResolve = new List<string>();
        private Dictionary<string, string> _checkinNotes = new Dictionary<string, string>();

        public string CheckinComment { get; set; }
        // This can be extended to checkin when the $EDITOR is invoked.
        public bool NoGenerateCheckinComment { get; set; }
        public bool NoMerge { get; set; }
        public string OverrideReason { get; set; }
        public bool Force { get; set; }
        public bool OverrideGatedCheckIn { get; set; }
        public List<string> WorkItemsToAssociate { get { return _workItemsToAssociate; } }
        public List<string> WorkItemsToResolve { get { return _workItemsToResolve; } }
        public Dictionary<string, string> CheckinNotes { get { return _checkinNotes; } }

        public string AuthorTfsUserId { get; set; }
        public Regex WorkItemAssociateRegex { get; set; }
        public bool IgnoreMissingItems { get; set; }
        public bool AddMissingItems { get; set; }

    }
}
