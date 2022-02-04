## Summary

The quick-clone command creates a new git repository, initialized from the last changeset (or a specific changeset in history) in a TFS source tree, ignoring the full history. 
Useful for making code changes or additions where past history isn't relevant.

## Synopsis

	Usage: git-tfs quick-clone [options] tfs-url-or-instance-name repository-path <git-repository-path>
	where options are:

		-i, --id, --tfs-remote, --remote
			(Type: Value required, Value Type:[String])
			An optional remote ID, useful if this repository will track multiple TFS
				repositories.

		-d, --debug
			(Type: Flag, Value Type:[Boolean])
			Show lots of output.

		-H, -h, --help
			(Type: Flag, Value Type:[Boolean])
			ShowHelp

		-V, --version
			(Type: Flag, Value Type:[Boolean])
			ShowVersion

		--template
			(Type: Value required, Value Type:[String])
			The --template option to pass to git-init.

		--shared
			(Type: Value required, Value Type:[Object])
			The --shared option to pass to git-init.

		--initial-branch
			(Type: Value required, Value Type:[String])
			The --initial-branch option to pass to git-init (requires Git >= 2.28.0).

		--no-metadata
			(Type: Flag, Value Type:[Boolean])
			If specified, git-tfs will leave out the git-tfs-id: lines at the end of every
				commit.

		--ignore-regex
			(Type: Value required, Value Type:[String])
			If specified, git-tfs will not sync any paths that match this regular expression.

		-p, --Password
			(Type: Value required, Value Type:[String])
			Password for TFS connection

		-u, --Username
			(Type: Value required, Value Type:[String])
			Username for TFS connection

		--ignore-regex
			(Type: Value required, Value Type:[String])
			If specified, git-tfs will not sync any paths that match this regular expression.

		-c, --changeset
			(Type: Value optional, Value Type:[Int32])
			Specify a changeset to clone from

		--fetch-all, --all
			(Type: Flag, Value Type:[Boolean])
			all

		-p, --parents
			(Type: Flag, Value Type:[Boolean])
			parents

## Remark

Make sure that you use a local drive (and not a network share) where the clone is stored.
`git-tfs` didn't receive any explicit testing for cloning on a network share and there are known reports
like [Issue 1373](https://github.com/git-tfs/git-tfs/issues/1373) where cloning/fetching a shelveset
didn't work when the clone was done on a network share.

## Examples

### Simple

To clone the latest changeset in `$/Project1` from your TFS server `tfs`
into a new directory `Project1`, do this:

    git tfs quick-clone http://tfs:8080/tfs/DefaultCollection $/Project1

### Clone a specific changeset

To clone a specific changeset in the history of `$/Project1` from your TFS server `tfs`
into a new directory `Project1`, do this:

    git tfs quick-clone http://tfs:8080/tfs/DefaultCollection $/Project1 -c=126

where `126` is the id of the changeset to clone.

If you want to get all the history from this specific changeset, then just do:

    git tfs fetch

### Excludes

Let's say you want to clone `$/Project`, but you don't want to
clone exes.

    git tfs quick-clone --ignore-regex=exe$ http://tfs:8080/tfs/DefaultCollection $/Project1

## Cloning the whole TFS Project Collection

You can clone all projects by specifying ``$/`` as the tfs-repository path. If you do not specify a git repository name, it will clone into ``tfs-collection``.

## See also

* [init](init.md)
* [fetch](fetch.md)
* [clone](clone.md)
