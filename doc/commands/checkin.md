## Summary

This function squashes differences between the last TFS commit and HEAD into one TFS-checkin and mark it as descendant of both previous TFS-checkin and HEAD.

It works similarly to [checkintool](checkintool.md), except checkintool launches the standard TFS checkin window.

The `checkin` command differs from [rcheckin](rcheckin.md) in that the latter mirrors a series of commits into TFS instead of squashing them into one.

## Synopsis

    Usage: git-tfs checkin [options] [ref-to-checkin]
    where options are:
        -d, --debug
            Show debug output about everything git-tfs does

        -i, --tfs-remote, --remote, --id=VALUE
            The remote ID of the TFS to interact with

        -m, --comment=VALUE
            A comment for the changeset

        --no-build-default-comment
            Do not concatenate commit comments for the changeset comment.

        --no-merge
            Omits setting commit being checked in as parent, thus allowing to rebase remaining onto TFS changeset without exceeding merge commits.

        -f, --force=VALUE
            The policy override reason.

        -w, --work-item=VALUE1:VALUE2
            Associated work items:
                e.g. -w12345 to associate with 12345
                or -w12345:resolve to resolve 12345

        --no-gate
            Disables gated checkin.
