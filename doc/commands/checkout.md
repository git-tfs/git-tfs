## Summary

The `checkout` command permit to checkout a specific revision with only the tfs changeset id.

You could also create (and checkout) a branch in the same time (option `-b`)
or just get the sha of the commit (option `-n`)

## Synopsis

	Usage: git-tfs checkout changesetId [-b=branch_name]
	   ex: git-tfs checkout 2365
		   git-tfs checkout 2365 -b=bugfix_2365

	  -h, -H, --help
	  -d, --debug                Show debug output about everything git-tfs does
	  -b, --branch=VALUE         Name of the branch to create
	  -n, --dry-run              Don't checkout the commit, just return commit sha

## Examples

### Checkout a specific revision

If you want to checkout a specific revision based on the tfs changeset id, use the command:

    git-tfs checkout 2365

### Create and checkout a branch

If you want to create a branch and checkout it based on the tfs changeset id, use the command:

    git-tfs checkout 2365 -b=bugfix_2365

The branch `bugfix_2365` will be created and checkouted if the commit is found.

### Get the sha of the commit corresponding to a tfs changeset

If you want to get the sha of the commit corresponding to a tfs changeset id, use the command:

    git-tfs checkout -n 2365

The command will output the sha of the commit:

    58ef29b38cf714b6386523c1636ba1e37b63cf34