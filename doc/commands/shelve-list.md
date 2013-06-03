## Summary

Lists the TFS shelvesets for the given or current user.

    Usage: git-tfs shelve-list -u <shelve-owner-name> [options]
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
      -s, --sort=VALUE           How to sort shelvesets
                                   date, owner, name, comment
      -f, --full                 Detailed output
      -u, --user=VALUE           Shelveset owner (default: current user)
                                   Use 'all' to get all shelvesets.

## Examples

### List the TFS shelvesets for the current TFS user.

`git tfs shelve-list`

### List the TFS shelvesets for the all the TFS users.

`git tfs shelve-list -u=all`

### List the full defails of TFS shelvesets for the current TFS user.

`git tfs shelve-list --full`

### List the sorted (by date) TFS shelvesets for the current TFS user.

`git tfs shelve-list --sort=date`

## See also

* [unshelve](unshelve.md)
* [shelve](unshelve.md)
