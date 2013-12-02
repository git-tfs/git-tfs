## Summary

The `pull` command fetches TFS changesets (like the `fetch` command) and merges 
(or rebase using `r` option) the current branch with the commits fetched 
(creation of a merge commit or rebase all the commits).

## Synopsis

    Usage: git-tfs pull [options]
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
      -I, --auto-tfs-remote, --auto-remote
                                 Autodetect (from git history) the remote ID of
                                   the TFS to interact with
          --all, --fetch-all
          --parents
          --authors=VALUE        Path to an Authors file to map TFS users to Git
                                   users
          --ignore-regex=VALUE   a regex of files to ignore
          --no-metadata          leave out the 'git-tfs-id:' tag in commit
                                   messages
                                   Use this when you're exporting from TFS and
                                   don't need to put data back into TFS.
      -u, --username=VALUE       TFS username
      -p, --password=VALUE       TFS password
      -r, --rebase               rebase your modifications on tfs changes

## Examples

### Simple

To pull all the changesets of the `default` branch (and create a merge commit):

    git tfs pull

To pull all the changesets of the `default` branch and rebase your modifications onto:

    git tfs pull --rebase

or 

    git tfs pull -r

### Pull from a branch

To pull all the changeset of the `tfs/myBranch` branch:

    git tfs pull -i myBranch

### Pull from the current branch

To pull all the changeset of the current branch:

    git tfs pull -I

The current branch depend of the git commit that is currently checkouted. Git-tfs will look in the history
to find the appropriate branch to pull.

### Authentication

For the use of parameters `--username` and `--password`, see the [clone](clone.md) command.

### Map TFS users to git users

For the use of parameter `--authors`, see the [clone](clone.md) command.

## See also

* [fetch](fetch.md)
