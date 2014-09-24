## Summary

The `clone` command creates a new git repository, initialized from
a TFS source tree and fetch all the changesets

## Synopsis

	Usage: git-tfs clone [options] tfs-url-or-instance-name repository-path <git-repository-path>
	  ex : git tfs clone http://myTfsServer:8080/tfs/TfsRepository $/ProjectName/ProjectBranch

	  -h, -H, --help
	  -V, --version
	  -d, --debug                Show debug output about everything git-tfs does
	  -i, --tfs-remote, --remote, --id=VALUE
								 The remote ID of the TFS to interact with
								   default: default
		  --template=VALUE       Passed to git-init
		  --shared[=VALUE]       Passed to git-init
		  --autocrlf=VALUE       Normalize line endings (default: false)
		  --ignorecase=VALUE     Ignore case in file paths (default: system default)
		  --bare                 clone the TFS repository in a bare git repository
		  --workspace=VALUE      set tfs workspace to a specific folder (a shorter path is better!)
		  --ignore-regex=VALUE   a regex of files to ignore
		  --except-regex=VALUE   a regex of exceptions to ignore-regex
	  -u, --username=VALUE       TFS username
	  -p, --password=VALUE       TFS password
		  --all, --fetch-all
		  --parents
		  --authors=VALUE        Path to an Authors file to map TFS users to Git users
	  -l, --with-labels, --fetch-labels
								 Fetch the labels also when fetching TFS changesets
	  -x, --export               Export metadatas
		  --export-work-item-mapping=VALUE
								 Path to Work-items mapping export file
		  --ignore-branches      Ignore fetching merged branches when encounter merge changesets
		  --batch-size=VALUE          Size of a the batch of tfs changesets fetched (-1 for all in one batch)
		  --with-branches        init all the TFS branches during the clone
		  --resumable            if an error occurred, try to continue when you restart clone
								 with same parameters

## Examples
### Simple
To clone all of `$/Project1` from your TFS 2010 server `tfs`
into a new directory `Project1`, do this:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1

### Clone only the trunk (or a branch)
Sometimes, it could be interesting to clone only a branch of a TFS repository (for exemple to extract only the trunk of your project and manage branches with `[branch](branch.md)`. 

Suppose you have on TFS:

    A <- B <- C <- D <- E  $/Project1/Trunk
               \                              
                M <- N     $/Project1/Branch

Then, do this (the clone will be done in the `MyProject1Directory` directory):

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1/Trunk MyProject1Directory

Note : It is highly recommanded to clone the root branch ( the branch that has no parents, here $/Project1/Trunk ) to be able to init the other branches after.
If you clone the branch $/Project1/Branch, you will never able to init the root branch $/Project1/Trunk after.

### Merge changesets and branches

Since version 0.20, when cloning the trunk from TFS, if git-tfs encounter a merge changeset, it initialize and fetch automaticaly the other branch merged.

Suppose you have on TFS:

    A <- B <- C <- D <- E <- X <- Y <- Z $/Project1/Trunk
               \           /                   
                M <- N <- O <- P <- Q    $/Project1/Branch

When cloning the tfs branch `$/Project1/Trunk`, after having fetch changesets A to E, git-tfs encounter merge changeset X. When it did, git-tfs initialize also the tfs branch `$/Project1/Branch` and fetch changeset M to O to be able to create the merge commit X and then continue to fetch changesets Y and Z.

If you don't want to intialize the merged branches automatically ( or you can't because your use of TFS is not supported), you could use the option `--ignore-branches` to disable it!

Note: To successfully process the merge changeset (and come from an older version than TFS2010), you should have converted all the folders corresponding to a TFS branch to a branch in TFS (even the old deleted branches). To do that, open the 'Source Control Explorer', right clic on a folder and choose `Branching and Merging` -> `Convert to Branch`.

### What repository path to clone?

If you don't know exactly what repository path to clone, see [list-remote-branches](list-remote-branches.md) command to get a list of the existing repositories.

### Clone all the branches (and merge changesets)

Prerequisite: To use this feature, all your source code folders corresponding to branches
should be converted into branches (a notion introduced by TFS2010).
To change that, you should open 'Source Control Explorer' and then, for each folder corresponding to a branch, right click on your source folder and select 'Branching and Merging' > 'Convert to branch'.

If you want to clone your entire repository with all the branches or that the tfs branches are merged througth merge changeset, perhaps you should use the option `--with-branches`:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1/Trunk --with-branches

All the tfs history (and all the branches) and the merge changesets will consequently be fetched from TFS and created in the git repository!

### Excludes

Let's say you want to clone `$/Project`, but you don't want to
clone exes.

    git tfs clone --ignore-regex=exe$ http://tfs:8080/tfs/DefaultCollection $/Project1

### Authentication

If the TFS server need an authentication, you could use the _--username_ and _--password_ parameters. If you don't specify theses informations, you will be prompted to enter them. If you use these parameters, the informations, git-tfs will store these informations (in the .git/config file --in clear--) and never prompt you again. If you don't want your password to be saved, don't use these options.

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1 -u=DISSRVTFS03\peter.pan -p=wendy


### Map TFS users to git users

With the parameter _--authors_, you could specify a file containing all the mapping of the TFS users to the git users. Each line describing a mapping following the syntax:

    DISSRVTFS03\peter.pan = Peter Pan <peter.pan@disney.com>

The clone command will be :

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1 --authors="c:\project_file\authors.txt"

Once the clone is done, the file is store in the `.git` folder (with the name `git-tfs_authors`) and used with later `fetch`. You could overwrite it by specifting another file (or go delete it).


### Set a custom Tfs Workspace directory

By default, git-tfs use as a Tfs workspace an internal directory and you shouldn't care about ;)
But, due to [file system limitations](../Set-custom-workspace.md), it could be usefull to set a custom directory (with a path as short as possible) as a tfs wordspace.
You could do it with the _--workspace_ parameter:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1 --workspace="c:\ws"

### Export metadatas

The option `--export` permit, when fetching changesets and creating commits, to add all the tfs metadatas to the commit message (work items ids, reviewers, ...).

This option could be used with other option `--export-work-item-mapping` that specify a mapping file to convert old work items ids to new work items ids.

It could be used to migrate sources away from TFSVC. See [Migrate from tfs to git](../usecases/migrate_tfs_to_git.md#migrate-toward-tfs2013-git-repository-keeping-workitems) for more details.

### Batch size of fetched changesets

The option `--batch-size` permit to specify the number of changesets fetched from tfs at the same time (default:100).
You could use this option to specify smaller batch size if git-tfs use too much memory because changesest are huge.
This option is saved in the git config file (key `git-tfs.batch-size`). See [config file doc](../config.md). 
Note: this option could also be specified during the `fetch`.

## After cloning a repository

It is recommended, especially if the TFS repository is a big one, to run, after a clone :
* a git garbage collect : `git gc`
* a [cleanup](cleanup.md) : `git tfs cleanup`

## See also

* [list-remote-branches](list-remote-branches.md)
* [init](init.md)
* [fetch](fetch.md)
* [quick-clone](quick-clone.md)
