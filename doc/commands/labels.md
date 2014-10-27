## Summary

The `labels` command fetch all the tfs labels.

Note : Due to how TFS manage labels, this command fetch a lot of data and could be quite long :(

## Synopsis

	Usage: git-tfs labels [options] [tfsRemoteId]
	 ex : git tfs labels
		  git tfs labels -i myRemoteBranche
		  git tfs labels --all
	  -h, -H, --help
	  -V, --version
	  -d, --debug                Show debug output about everything git-tfs does
	  -i, --tfs-remote, --remote, --id=VALUE
								 The remote ID of the TFS to interact with
								   default: default
		  --all, --fetch-all     Fetch all the labels on all the TFS remotes (For
								   TFS 2010 and later)
	  -n, --label-name=VALUE     Fetch all the labels respecting this name filter
	  -e, --exclude-label-name=VALUE
								 Exclude all the labels respecting this regex
								   name filter
	  -u, --username=VALUE       TFS username
	  -p, --password=VALUE       TFS password
	  -a, --authors=VALUE        Path to an Authors file to map TFS users to Git
								   users
## Examples

TODO...