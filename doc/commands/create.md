## Summary

The `create` command creates the project folder on a TFS server and initialize the git repository with this new TFS folder.
You could also in the same time create the Tfs repository...

Prefer the [clone](clone.md) or [init](init.md) command if the project folder already exists in TFS!

## Synopsis

	Usage: git-tfs create [options] tfs-url-or-instance-name project-name -t=trunk-name <git-repository-path>
	ex : git tfs create http://myTfsServer:8080/tfs/TfsRepository myProjectName
		 git tfs create http://myTfsServer:8080/tfs/TfsRepository myProjectName -t=myTrunkName

	  -h, -H, --help
	  -V, --version
	  -d, --debug                Show debug output about everything git-tfs does
	  -i, --tfs-remote, --remote, --id=VALUE
								 The remote ID of the TFS to interact with
								   default: default
	  -c, --create-project-folder
								 Create also the team project folder if it
								   doesn't exist!
	  -t, --trunk-name=VALUE     name of the main branch that will be created on
								   TFS (default: "trunk")
		  --template=VALUE       Passed to git-init
		  --shared[=VALUE]       Passed to git-init
		  --initial-branch=VALUE Passed to git-init (requires Git >= 2.28.0)
		  --autocrlf=VALUE       Normalize line endings (default: false)
		  --bare                 clone the TFS repository in a bare git repository
		  --ignore-regex=VALUE   a regex of files to ignore
		  --except-regex=VALUE   a regex of exceptions to ingore-regex
	  -u, --username=VALUE       TFS username
	  -p, --password=VALUE       TFS password
		  --no-parallel          Do not do parallel requests to TFS
		  --all, --fetch-all
		  --parents
		  --authors=VALUE        Path to an Authors file to map TFS users to Git
								   users
	  -l, --with-labels, --fetch-labels
								 Fetch the labels also when fetching TFS
								   changesets
	  -b, --bare-branch=VALUE    The name of the branch on which the fetch will
								   be done for a bare repository
		  --force                Force fetch of tfs changesets when there is
								   ahead commits (ahead commits will be lost!)

## Examples

### Create root branch with the default name 'trunk'

To create `$/myProjectName/trunk` in your TFS 2010/2012 server when `$/myProjectName` already exists, do this:

    git tfs create http://myTfsServer:8080/tfs/TfsRepository myProjectName

### Create root branch with a custom name

To create `$/myProjectName/master` in your TFS 2010/2012 server when `$/myProjectName` already exists, do this:

    git tfs create http://myTfsServer:8080/tfs/TfsRepository myProjectName -t=master

### Create the project folder and the root branch

To create `$/myProjectName/trunk` in your TFS 2010/2012 server and the project folder `$/myProjectName` in the same time, do this:

    git tfs create --create-project-folder http://myTfsServer:8080/tfs/TfsRepository myProjectName

PS: it is preferred to create the project folder from TFS GUI because this command create only the source controller part of the project.
TFS will create in the same the other components for the project (ALM, issue management,...)

## See also

* [clone](clone.md)
* [init](init.md)
* [quick-clone](quick-clone.md)
* [fetch](fetch.md)
