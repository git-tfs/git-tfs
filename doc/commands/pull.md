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
      -A, --authors=VALUE        Path to an Authors file to map TFS users to Git
                                  users (will be kept in cache and used for all
                                  the following commands)
          --all, --fetch-all     Fetch TFS changesets of all the initialized tfs
                                  remotes
          --parents              Fetch TFS changesets of the parent(s)
                                  initialized tfs remotes
      -l, --with-labels, --fetch-labels
                                  Fetch the labels also when fetching TFS
                                  changesets
      -b, --bare-branch=VALUE    The name of the branch on which the fetch will
                                  be done for a bare repository
          --force                Force fetch of tfs changesets when there is
                                  ahead commits (ahead commits will be lost!)
      -x, --export               Export metadata
          --export-work-item-mapping=VALUE
                                  Path to Work-items mapping export file
          --branches=VALUE       Strategy to manage branches:
                                  * auto:(default) Manage the encountered merged
                                  changesets and initialize only the merged
                                  branches
                                  * none: Ignore branches and merge changesets,
                                  fetching only the cloned tfs path
                                  * all: Manage merged changesets and initialize
                                  all the branches during the clone
          --batch-size=VALUE     Size of the batch of tfs changesets fetched (-1
                                  for all in one batch)
      -c, --changeset, --from=VALUE
                                  The changeset to clone from (must be a number)
      -t, --up-to, --to=VALUE    up-to changeset # (optional, -1 for up to
                                  maximum, must be a number, not prefixed with 'C')
          --ignore-branches-regex=VALUE
                                  Don't initialize branches that match given regex
          --ignore-not-init-branches
                                  Ignore not-initialized branches
          --ignore-restricted-changesets
                                  Ignore restricted changesets
          --ignore-regex=VALUE   A regex of files to ignore
          --except-regex=VALUE   A regex of exceptions to '--ignore-regex'
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
