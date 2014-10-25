## Summary

This function launches the standard TFS checkin window to commit your changes into TFS. It squashes differences between the last TFS commit and HEAD into one TFS-checkin and marks it as descendant of both previous TFS-checkin and HEAD.

It works similarly to [checkin](checkin.md), except checkin doesn't launch the standard TFS checkin window.

## Synopsis

    Usage: git-tfs checkintool [options] [ref-to-checkin]
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
      -m, --comment=VALUE        A comment for the changeset
          --no-build-default-comment
                                 Do not concatenate commit comments for the
                                   changeset comment.
          --no-merge             Omits setting commit being checked in as parent,
                                   thus allowing to rebase remaining onto TFS
                                   changeset without exceeding merge commits.
      -f, --force=VALUE          The policy override reason.
      -w, --work-item=VALUE1:VALUE2
                                 Associated work items
                                   e.g. -w12345 to associate with 12345
                                   or -w12345:resolve to resolve 12345
      -c, --code-reviewer=VALUE  Set code reviewer
                                   e.g. -c "John Smith"
      -s, --security-reviewer=VALUE
                                 Set security reviewer
                                   e.g. -s "John Smith"
      -p, --performance-reviewer=VALUE
                                 Set performance reviewer
                                   e.g. -p "John Smith"
          --no-gate              Disables gated checkin.

## See also

* [checkin](checkin.md)
* [rcheckin](rcheckin.md)
