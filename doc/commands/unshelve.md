## Summary
Unshelves a TFS shelveset into a new Git branch.

    Usage: git-tfs unshelve [options] shelve-name destination-branch
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
      -A, --authors=VALUE        Path to an Authors file to map TFS users to Git
                                   users (will be kept in cache and used for all
                                   the following commands)
      -u, --user=VALUE           Shelveset owner (default: current user)
                                   Use 'all' to search all shelvesets.
      -b, --branch=VALUE         Git Branch to apply Shelveset to? (default: TFS
                                   current remote)
          --force                Get as much of the Shelveset as possible, and
                                   log any other errors

## Examples

Note: These commands will fail if the branch specified by `git-branch-name` already exists locally.

### Create a branch from a shelveset under the current TFS user

`git tfs unshelve MyShelvesetName MyBranch`

### Create a branch from a shelveset under the defined TFS user

`git tfs unshelve -u UserName MyShelvesetName MyBranch`

## See also

* [shelve-list](shelve-list.md)
* [shelve](shelve.md)
