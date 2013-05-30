## Summary

The `pull` command fetches TFS changesets (like the `fetch` command) and merges (or rebase) the current branch with the commits fetched (creation of a merge commit or rebase all the commits).

## Synopsis

    Usage: git-tfs pull [options]
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
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

### Fetch from a branch

To pull all the changeset of the `tfs/myBranch` branch:

    git tfs pull -i myBranch

### Authentication

For the use of parameters `--username` and `--password`, see the [[clone]] command.

### Map TFS users to git users

For the use of parameter `--authors`, see the [[clone]] command.

## See also

* [[fetch]]
