## Summary
Unshelves a TFS shelveset into a new Git branch.

    Usage: git-tfs unshelve -u <shelve-owner-name> <shelve-name> <git-branch-name>
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
      -u, --user=VALUE           Shelveset owner (default: current user)
                                   Use 'all' to search all shelvesets.
      -b, --branch=VALUE         Git branch to branch from (default: TFS default branch)
                                   Used to work with TFS Branches in the Git
                                   repository.  Shelfset becomes a branch of
                                   the virtual TFS branch in GIT.

## Examples

Note: These commands will fail if the branch specified by `git-branch-name` already exists locally.

### Create a branch from a shelveset under the current TFS user

`git tfs unshelve MyShelvesetName MyBranch`

### Create a branch from a shelveset under the defined TFS user

`git tfs unshelve -u UserName MyShelvesetName MyBranch`

## See also

* [shelve-list](shelve-list.md)
* [shelve](shelve.md)
