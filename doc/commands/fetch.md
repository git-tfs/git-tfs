## Summary

The fetch command fetch all the new changesets from a TFS remote

## Synopsis

    Usage: git-tfs fetch [options] [tfs-remote-id]...
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
      -I, --auto-tfs-remote, --auto-remote
                                 Autodetect (from git history) the remote ID of
                                   the TFS to interact with
          --all, --fetch-all     Fetch TFS changesets of all the initialized tfs
                                   remotes
          --parents              Fetch TFS changesets of the parent(s)
                                   initialized tfs remotes
          --authors=VALUE        Path to an Authors file to map TFS users to Git
                                   users
          --ignore-regex=VALUE   a regex of files to ignore
          --no-metadata          leave out the 'git-tfs-id:' tag in commit
                                   messages
                                   Use this when you're exporting from TFS and
                                   don't need to put data back into TFS.
      -u, --username=VALUE       TFS username
      -p, --password=VALUE       TFS password
## Examples

### Simple

To fetch all the changeset of the `default` remote:

    git tfs fetch

### Fetch from a branch

To fetch all the changesets of the `tfs/myBranch` remote:

    git tfs fetch -i myBranch

### Fetch from the current branch

To fetch all the changesets of the current remote:

    git tfs fetch -I

The current remote depend of the git commit that is currently checkouted. Git-tfs will look in the history
to find the appropriate remote to fetch.

### Fetch from all the remotes

To fetch all the changesets of all the initialized remotes

    git tfs fetch --all

Note: you could see all the tfs remotes already initialized using command `git tfs branch` and
all that exists on the tfs server using command `git tfs branch -r`

### Authentication

For the use of parameters `--username` and `--password`, see the [clone](clone.md) command.

### Map TFS users to git users

For the use of parameter `--authors`, see the [clone](clone.md) command.

### Fetching merge changesets

Merge changesets will be automatically fetched and created as a merge commit if the tfs branch merged has already inited in the git repository.
If the tfs branch merged has not be inited, the merge changeset will be created as a normal commit (not a merged one) and this warning message will be created :

    warning: this changeset 34 is a merge changeset. But it can't have been managed accordingly because one of the parent changeset 33 is not present in the repository! If you want to do it, fetch the branch containing this changeset before retrying...

### Batch size of fetched changesets

The option `--batch-size` permit to specify the number of changesets fetched from tfs at the same time (default:100).
You could use this option to specify smaller batch size if git-tfs use too much memory because changesets are huge.
This option is not saved but you could add it to the git config file (key `git-tfs.batch-size`). See [config file doc](../config.md). 
Note: this option could also be specified during the `clone`.

## See also

* [clone](clone.md)
* [branch](branch.md)
