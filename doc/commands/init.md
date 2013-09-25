## Summary

The `init` command creates a new git repository, initialized from a TFS source tree (without fetching the changesets). Fetching changeset should be done with [fetch](fetch.md) command.

Prefer the [clone](clone.md) command to init and fetch changesets from a TFS repository!

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

then, to retrieve tfs changesets do this :

    git tfs pull

Note: [pull] is here prefered to [fetch], otherwise the git branch `master` won't be created :(	

## See also

* [clone](clone.md)
* [quick-clone](quick-clone.md)
* [pull](pull.md)
* [fetch](fetch.md)
* [Set custom workspace to bypass errors due to NTFS length path limits of 259 characters](../Set-custom-workspace.md)
