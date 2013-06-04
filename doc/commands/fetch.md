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
## Examples

### Simple

To fetch all the changeset of the `default` branch:

    git tfs fetch

### Fetch from a branch

To fetch all the changeset of the `tfs/myBranch` branch:

    git tfs fetch -i myBranch

### Authentication

For the use of parameters `--username` and `--password`, see the [clone](clone.md) command.

### Map TFS users to git users

For the use of parameter `--authors`, see the [clone](clone.md) command.

## See also

* [clone](clone.md)
