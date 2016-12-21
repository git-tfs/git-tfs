## Summary

Deletes a TFS shelveset for the current user.

    Usage: git-tfs shelve-delete <shelveset-name>
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default

## Examples

### Delete a shelveset.

`git tfs shelve-delete "feature-reset-password"`

## See also

* [unshelve](unshelve.md)
* [shelve](unshelve.md)