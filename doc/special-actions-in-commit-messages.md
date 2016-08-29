The [rcheckin](commands/rcheckin.md) command examines the commit message for additional TFS specific
notifications. The following may be appended as separate lines to the end of the commit message:

* `git-tfs-work-item: <id>` will link the new changeset with the given work item and the default action type.
* `git-tfs-work-item: <id> <action>` will link the new changeset with the given work item and the given action type.
* `git-tfs-code-reviewer: <name>` sets the Code Reviewer field.
* `git-tfs-security-reviewer: <name>` sets the Security Reviewer field.
* `git-tfs-performance-reviewer: <name>` sets the Performance Reviewer field.
* `git-tfs-force: <reason>` will force the checkin, overriding TFS checkin policies with the given reason.

The [shelve](commands/shelve.md) command also examines the commit message for the following TFS specific notifications, in the same matter as the rcheckin command:

* `git-tfs-work-item: <id>` will link the new changeset with the given work item and the default action type.

### Workitem IDs from message

Additionally the text of the message is searched for work item IDs. If a string matching a # followed
by a valid work-item ID number is found (e.g. `Introduced lazy evaluation (#1234)`), then the commit will be associated with the specified TFS work-item.

For cases where this may be undesirable, the default match may be overridden by setting the
`git-tfs.work-item-regex` config variable to a suitable alternate regular expression.
The default is `"#(?<item_id>\d+)"` but if more specificity is required it could be redefined as
`"workitem #(?<item_id>\d+)"` to require "workitem #" as a prefix, avoiding collision with
alternative bug trackers.

## Example

The following commit message 
produces the checkin comment "Make this change (#456)",
links to work item 123,
links to work item 456,
resolves work item 234,
sets George Washington as the Code Reviewer,
sets John Adams as the Security Reviewer,
sets Thomas Jefferson as the Performance Reviewer,
and overrides any checkin policy failures with the reason "Because".

```
commit 7440baf7ef01f9bb78d7ad02c3f1341758676ad2
Author: Matt Burke <spraints@gmail.com>
Date:   Thu May 16 15:31:11 2013 -0400

    Make this change (#456)

    git-tfs-work-item: 123
    git-tfs-work-item: 234 resolve
    git-tfs-code-reviewer: George Washington
    git-tfs-security-reviewer: John Adams
    git-tfs-performance-reviewer: Thomas Jefferson
    git-tfs-force: Because
```
