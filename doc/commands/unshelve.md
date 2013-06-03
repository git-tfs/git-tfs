## Summary
Unshelves a TFS shelveset into a Git branch.

    Usage: git-tfs unshelve -u <shelve-owner-name> <shelve-name> <git-branch-name>
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
      -u, --user=VALUE           Shelveset owner (default: current user)
                                   Use 'all' to search all shelvesets.

## Examples

### Create a branch from a shelveset under the current TFS user

`git tfs unshelve MyShelvesetName MyBranch`

### Create a branch from a shelveset under the defined TFS user

`git tfs unshelve -u UserName MyShelvesetName MyBranch`

## See also

* [shelve-list](shelve-list.md)
* [shelve](shelve.md)
