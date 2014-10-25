## Summary

The `autotag` option put in the configuration of a Tfs remote indicate that git-tfs should create a git tag (displaying the changeset id) on each changeset fetched.

## Enable this option for a remote

To enable this feature, with one of your remote (with `remoteId` is the name of your tfs remote):

    git config --local tfs-remote.remoteId.autotag "true"

Example for the `default` remote:

    git config --local tfs-remote.default.autotag "true"
	
Note: You could also edit the `.git\config` file with an editor to enable the option.