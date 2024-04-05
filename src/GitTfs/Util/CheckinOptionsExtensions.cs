using System.Text.RegularExpressions;
using GitTfs.Commands;
using GitTfs.Core;
using System.Diagnostics;

namespace GitTfs.Util
{
    public static class CheckinOptionsExtensions
    {
        public static CheckinOptions Clone(this CheckinOptions source, Globals globals)
        {
            CheckinOptions clone = new CheckinOptions();

            clone.CheckinComment = source.CheckinComment;
            clone.NoGenerateCheckinComment = source.NoGenerateCheckinComment;
            clone.NoMerge = source.NoMerge;
            clone.OverrideReason = source.OverrideReason;
            clone.Force = source.Force;
            clone.OverrideGatedCheckIn = source.OverrideGatedCheckIn;
            clone.WorkItemsToAssociate.AddRange(source.WorkItemsToAssociate);
            clone.WorkItemsToResolve.AddRange(source.WorkItemsToResolve);
            clone.AuthorTfsUserId = source.AuthorTfsUserId;
            try
            {
                string re = globals.Repository.GetConfig(GitTfsConstants.WorkItemAssociateRegexConfigKey);
                if (string.IsNullOrEmpty(re))
                    clone.WorkItemAssociateRegex = GitTfsConstants.TfsWorkItemAssociateRegex;
                else
                    clone.WorkItemAssociateRegex = new Regex(re);
            }
            catch (Exception)
            {
                clone.WorkItemAssociateRegex = null;
            }
            foreach (var note in source.CheckinNotes)
            {
                clone.CheckinNotes[note.Key] = note.Value;
            }

            return clone;
        }

        public static void ProcessWorkItemCommands(this CheckinOptions checkinOptions, bool isResolvable = true)
        {
            MatchCollection workitemMatches = GitTfsConstants.TfsWorkItemRegex.Matches(checkinOptions.CheckinComment);
            if (workitemMatches.Count > 0)
            {
                foreach (Match match in workitemMatches)
                {
                    if (isResolvable && match.Groups["action"].Value == "resolve")
                    {
                        Trace.TraceInformation("Resolving work item {0}", match.Groups["item_id"]);
                        checkinOptions.WorkItemsToResolve.Add(match.Groups["item_id"].Value);
                    }
                    else
                    {
                        Trace.TraceInformation("Associating with work item {0}", match.Groups["item_id"]);
                        checkinOptions.WorkItemsToAssociate.Add(match.Groups["item_id"].Value);
                    }
                }
                checkinOptions.CheckinComment = GitTfsConstants.TfsWorkItemRegex.Replace(checkinOptions.CheckinComment, "").Trim(' ', '\r', '\n');
            }

            if (checkinOptions.WorkItemAssociateRegex != null)
            {
                var workitemAssociatedMatches = checkinOptions.WorkItemAssociateRegex.Matches(checkinOptions.CheckinComment);
                if (workitemAssociatedMatches.Count != 0)
                {
                    foreach (Match match in workitemAssociatedMatches)
                    {
                        var workitem = match.Groups["item_id"].Value;
                        if (!checkinOptions.WorkItemsToAssociate.Contains(workitem))
                        {
                            Trace.TraceInformation("Associating with work item {0}", workitem);
                            checkinOptions.WorkItemsToAssociate.Add(workitem);
                        }
                    }
                }
            }
        }

        public static void ProcessCheckinNoteCommands(this CheckinOptions checkinOptions)
        {
            MatchCollection matches = GitTfsConstants.TfsReviewerRegex.Matches(checkinOptions.CheckinComment);
            if (matches.Count == 0)
                return;

            foreach (Match match in matches)
            {
                string reviewer = match.Groups["reviewer"].Value;
                if (!string.IsNullOrWhiteSpace(reviewer))
                {
                    switch (match.Groups["type"].Value)
                    {
                        case "code":
                            Trace.TraceInformation("Code reviewer: {0}", reviewer);
                            checkinOptions.CheckinNotes.Add("Code Reviewer", reviewer);
                            break;
                        case "security":
                            Trace.TraceInformation("Security reviewer: {0}", reviewer);
                            checkinOptions.CheckinNotes.Add("Security Reviewer", reviewer);
                            break;
                        case "performance":
                            Trace.TraceInformation("Performance reviewer: {0}", reviewer);
                            checkinOptions.CheckinNotes.Add("Performance Reviewer", reviewer);
                            break;
                    }
                }
            }
            checkinOptions.CheckinComment = GitTfsConstants.TfsReviewerRegex.Replace(checkinOptions.CheckinComment, "").Trim(' ', '\r', '\n');
        }

        public static void ProcessForceCommand(this CheckinOptions checkinOptions)
        {
            MatchCollection workitemMatches = GitTfsConstants.TfsForceRegex.Matches(checkinOptions.CheckinComment);
            if (workitemMatches.Count != 1)
                return;

            string overrideReason = workitemMatches[0].Groups["reason"].Value;
            if (!string.IsNullOrWhiteSpace(overrideReason))
            {
                Trace.TraceInformation("Forcing the checkin: {0}", overrideReason);
                checkinOptions.Force = true;
                checkinOptions.OverrideReason = overrideReason;
            }
            checkinOptions.CheckinComment = GitTfsConstants.TfsForceRegex.Replace(checkinOptions.CheckinComment, "").Trim(' ', '\r', '\n');
        }

        public static void ProcessAuthor(this CheckinOptions checkinOptions, GitCommit commit, AuthorsFile authors)
        {
            if (!authors.IsParseSuccessfull)
                return;

            Author a = authors.FindAuthor(commit.AuthorAndEmail);
            if (a == null)
            {
                checkinOptions.AuthorTfsUserId = null;
                return;
            }

            checkinOptions.AuthorTfsUserId = a.TfsUserId;
            Trace.TraceInformation("Commit was authored by git user {0} {1} ({2})", a.Name, a.Email, a.TfsUserId);
        }
    }
}
