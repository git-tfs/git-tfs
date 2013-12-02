The [rcheckin](commands/rcheckin.md) command reads checkin information from git commit messages
with certain git commit messages.

* `git-tfs-work-item: <id>` will link the new changeset with the given work item and the default action type.
* `#<id>` will also link the new changeset with the work item ( but the text is kept and can be put anywhere in the commit message) .
* `git-tfs-work-item: <id> <action>` will link the new changeset with the given work item and the given action type.
* `git-tfs-code-reviewer: <name>` sets the Code Reviewer field.
* `git-tfs-security-reviewer: <name>` sets the Security Reviewer field.
* `git-tfs-performance-reviewer: <name>` sets the Performance Reviewer field.
* `git-tfs-force: <reason>` will force the checkin, overriding TFS checkin policies with the given reason.


For example, the following commit message
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
