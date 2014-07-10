## Summary

The `branch` command permit to manage TFS branches. With this command, you can display, create, init, rename and delete branches.

## Synopsis
	Usage: git-tfs branch

		   * Display remote TFS branches:
		   git tfs branch -r
		   git tfs branch -r -all

		   * Create a TFS branch from current commit:
		   git tfs branch $/Repository/ProjectBranchToCreate <myWishedRemoteName> --comment="Creation of my branch"

		   * Rename a remote branch:
		   git tfs branch --move oldTfsRemoteName newTfsRemoteName

		   * Delete a remote branche:
		   git tfs branch --delete tfsRemoteName

		   * Initialise an existing remote TFS branch:
		   git tfs branch --init $/Repository/ProjectBranch
		   git tfs branch --init $/Repository/ProjectBranch myNewBranch
		   git tfs branch --init --all
		   git tfs branch --init --tfs-parent-branch=$/Repository/ProjectParentBranch $/Repository/ProjectBranch

	  -h, -H, --help
	  -V, --version
	  -d, --debug                Show debug output about everything git-tfs does
	  -i, --tfs-remote, --remote, --id=VALUE
								 The remote ID of the TFS to interact with
								   default: default
	  -r, --remotes              Display the TFS branches of the current TFS root
								   branch existing on the TFS server
		  --all                  Display (used with option --remotes) the TFS
								   branches of all the root branches existing on
								   the TFS server
									or Initialize (used with option --init) all
								   existing TFS branches (For TFS 2010 and later)
		  --comment=VALUE        Comment used for the creation of the TFS branch
	  -m, --move                 Rename a TFS remote
		  --delete               Delete a TFS remote
		  --init                 Initialize an existing TFS branch
          --ignore-regex=VALUE   a regex of files to ignore
          --except-regex=VALUE   a regex of exceptions to ignore-regex
		  --no-fetch              Don't fetch changeset for inited branch(es)
	  -b, --tfs-parent-branch=VALUE
								 TFS Parent branch of the TFS branch to clone
								   (TFS 2008 only! And required!!) ex:
								   $/Repository/ProjectParentBranch
	  -u, --username=VALUE       TFS username
	  -p, --password=VALUE       TFS password
	  -a, --authors=VALUE        Path to an Authors file to map TFS users to Git
								   users## Examples

## Display already inited branches

    git tfs branch

## Display existing TFS branches

### Display branches from the current repository

    git tfs branch -r

### Display branches of all the repositories
    git tfs branch -r -all

## Create a TFS branch

First, checkout with git the revision from where you want to create the TFS branch. Then use the command :

    git tfs branch $/Repository/ProjectBranchToCreate --comment="Creation of my branch"

You will now have a TFS branch (called $/Repository/ProjectBranchToCreate ) with a first commit with a the comment specified. The local git remote with the same name 'ProjectBranchToCreate' is created.

If you want to specify another name (but not recommended), use the command :

    git tfs branch $/Repository/ProjectBranchToCreate myWishedRemoteName --comment="Creation of my branch"

 The local git remote with the name 'myWishedRemoteName ' is created.

## Init an existing TFS branches

To use this command, you should have cloned only the trunk folder in TFS (and not the whole repository). See [clone](clone.md) command.
Suppose you have on TFS:

    A <- B <- C <- D <- E  $/Repository/ProjectTrunk
               \                              
                M <- N     $/Repository/ProjectBranch

You should have done (to clone only the trunk) :

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Repository/ProjectTrunk

Note : It is highly recommanded once having clone the root branch ( the branch that has no parents, here $/Repository/ProjectTrunk ) to init the other branches after.
If you have cloned the branch $/Repository/ProjectBranch, you will never be able to init the root branch $/Repository/ProjectTrunk after.

Then use `branch` like this :

### Init a TFS branch using auto-naming of your git branch

    git tfs branch --init $/Repository/ProjectBranch

### Init a TFS branch with customised git branch name

    git tfs branch --init $/Repository/ProjectBranch myNewBranch

### Merge changesets and branches

Since version 0.20, when initializing and fetching a TFS branch, if git-tfs encounter a merge changeset, it initialize and fetch automaticaly the other branch merged.

If you don't want to intialize the merged branches automatically ( or you can't because your use of TFS is not supported), you could use the option `--ignore-branches` to disable it!

Note: To successfully process the merge changeset (and come from an older version than TFS2010), you should have converted all the folders corresponding to a TFS branch to a branch in TFS (even the old deleted branches). To do that, open the 'Source Control Explorer', right clic on a folder and choose `Branching and Merging` -> `Convert to Branch`.

### Init all the TFS branches

    git tfs branch --init --all
	
This command init all the branches not already done and ignore existing ones.

### Init a branch with TFS2008

TFS2008 doesn't permit to know the parent of a branch. You should find it yourself with TFS and use the parameter `--tfs-parent-branch` to give it to the `init-branch` command:

    git tfs branch --init --tfs-parent-branch=$/Repository/ProjectParentBranch $/Repository/ProjectBranch

### Ignore files when fetching changesets

You could use the parameter `--ignore-regex`, to ignore some file when fetching the changesets of the branch.

    git tfs init-branch $/Repository/ProjectBranch --ignore-regex=*.bin

You could use the parameter `--except-regex`, to add an exception to the parameter  `--ignore-regex`.

    git tfs init-branch $/Repository/ProjectBranch --ignore-regex=*.bin --except-regex=important.bin

### Init a branch without fetching changesets

You could use the parameter `--no-fetch`, to init the branch by creating its remote but without fetching the changesets of the branch.

### Authentication

For the use of parameters `--username` and `--password`, see the [clone](clone.md) command.

### Map TFS users to git users

For the use of parameter `--authors`, see the [clone](clone.md) command.

## Rename a remote branch

Note : It will not rename the TFS branch, just the local git remote.

    git tfs branch --move oldTfsRemoteName newTfsRemoteName


## Delete a remote branche

Note : It will not delete the TFS branch, just the local git remote.

    git tfs branch --delete tfsRemoteName

## See also

* [clone](clone.md)
* [fetch](fetch.md)
* [checkin](checkin.md)
* [rcheckin](rcheckin.md)
