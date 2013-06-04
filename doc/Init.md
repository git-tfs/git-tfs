## Summary

The `init` command creates a new git repository, initialized from a TFS source tree (without fetching the changesets). Fetching changeset should be done with [[fetch]] command.

Prefer the [[clone]] command to init and fetch changesets from a TFS repository!

## Synopsis

    Usage: git-tfs init [options] tfs-url-or-instance-name repository-path [git-repository]
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
          --template=VALUE       Passed to git-init
          --shared[=VALUE]       Passed to git-init
          --autocrlf=VALUE       Normalize line endings (default: false)
          --ignore-regex=VALUE   a regex of files to ignore
          --no-metadata          leave out the 'git-tfs-id:' tag in commit
                                   messages
                                   Use this when you're exporting from TFS and
                                   don't need to put data back into TFS.
      -u, --username=VALUE       TFS username
      -p, --password=VALUE       TFS password

## Examples

### Simple

To init `$/Project1` from your TFS 2010 server `tfs`
into a new directory `Project1`, do this:

    git tfs init http://tfs:8080/tfs/DefaultCollection $/Project1


## See also

* [[clone]]
* [[quick-clone]]
* [[fetch]]
