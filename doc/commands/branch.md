## Summary

The `branch` command allows you to manage TFS branches. With this command, you can display, create, initialize, rename and delete Tfs branches/remotes.

## Synopsis
	Usage: git-tfs branch

		   * Display remote TFS branches:
		   git tfs branch -r
		   git tfs branch -r -all

		   * Create a TFS branch from current commit:
		   git tfs branch $/Repository/ProjectBranchToCreate <myWishedRemoteName> --comment="Creation of my branch"

		   * Rename a remote branch:
		   git tfs branch --move oldTfsRemoteName newTfsRemoteName

		   * Delete a remote branch:
		   git tfs branch --delete tfsRemoteName
		   git tfs branch --delete --all

		   * Initialise an existing remote TFS branch:
		   git tfs branch --init $/Repository/ProjectBranch
		   git tfs branch --init $/Repository/ProjectBranch myNewBranch
		   git tfs branch --init --all

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
								    or Delete (used with option --delete) all tfs 
									remotes (for example after lfs migration).
		  --comment=VALUE        Comment used for the creation of the TFS branch
	  -m, --move                 Rename a TFS remote
		  --delete               Delete a TFS remote
		  --init                 Initialize an existing TFS branch
          --ignore-regex=VALUE   a regex of files to ignore
          --except-regex=VALUE   a regex of exceptions to ignore-regex
		  --no-fetch              Don't fetch changeset for inited branch(es)
	  -u, --username=VALUE       TFS username
	  -p, --password=VALUE       TFS password
	  -a, --authors=VALUE        Path to an Authors file to map TFS users to Git
								   users## Examples

## Display already initialized branches

    git tfs branch

## Display existing TFS branches

### Display branches from the current repository

    git tfs branch -r

### Display branches of all the repositories
    git tfs branch -r -all

## Create a TFS branch

First, use git to checkout the revision (branch or hash) from where you want to create the TFS branch. Then use the command :

    git tfs branch $/Repository/ProjectBranchToCreate --comment="Creation of my branch"

You will now have a TFS branch (called $/Repository/ProjectBranchToCreate ) whose first checkin will have the comment specified. A local git remote with the same name ('ProjectBranchToCreate') is created.

While not recommended, if you want to specify another name for the local branch, use the command :

    git tfs branch $/Repository/ProjectBranchToCreate myWishedRemoteName --comment="Creation of my branch"

 The local git remote with the name 'myWishedRemoteName ' is created.

## Initialize an existing TFS branch

To use this command, you should have cloned only the trunk folder in TFS (and not the whole repository). See [clone](clone.md) command.
Suppose you have on TFS:

    A <- B <- C <- D <- E  $/Repository/ProjectTrunk
               \                              
                M <- N     $/Repository/ProjectBranch

You should have done (to clone only the trunk) :

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Repository/ProjectTrunk

Note : It is highly recommended once having cloned the root branch (the branch that has no parents, here $/Repository/ProjectTrunk) to initialize the other branches after.
If you have cloned the branch $/Repository/ProjectBranch, you will not be able to init the root branch $/Repository/ProjectTrunk later (git can't create new commits that are parents to your existing local branch).

Then use `branch` like this :

### Initialize a TFS branch using auto-naming of your git branch

    git tfs branch --init $/Repository/ProjectBranch

### Initialize a TFS branch with customised git branch name

    git tfs branch --init $/Repository/ProjectBranch myNewBranch

### Merge changesets and branches

Since version 0.20, if git-tfs encounters a merge changeset while initializing and fetching a TFS branch, it will automatically initialize and fetch the merged branch as well.

If you don't want to initialize the merged branches automatically (or you can't because your version of TFS does not support this feature), you can use the option `--branches=none` to disable it!

Note: To successfully process the merge changeset (and come from an older version than TFS2010), you must first convert all the folders corresponding to a TFS branch to a branch in TFS (even the old deleted branches). To do that, open the 'Source Control Explorer', right click on a folder and choose `Branching and Merging` -> `Convert to Branch`.

### Initialize all the TFS branches

    git tfs branch --init --all

This command will initialize all the branches that haven't yet been initialized.

### Ignore files when fetching changesets

You can use the parameter `--ignore-regex`, to ignore some files when fetching the changesets of the branch.

    git tfs init-branch $/Repository/ProjectBranch --ignore-regex=*.bin

You can use the parameter `--except-regex`, to add an exception to the parameter `--ignore-regex`.

    git tfs init-branch $/Repository/ProjectBranch --ignore-regex=*.bin --except-regex=important.bin

### Initialize a branch without fetching changesets

You can use the parameter `--no-fetch`, to initialize the branch by creating its remote but without fetching the changesets of the branch.

### Authentication

For the use of parameters `--username` and `--password`, see the [clone](clone.md) command.

### Map TFS users to git users

For the use of parameter `--authors`, see the [clone](clone.md) command.

## Rename a remote branch

Note : This will not rename the TFS branch, just the local git remote.

    git tfs branch --move oldTfsRemoteName newTfsRemoteName


## Delete a remote branch

Note : This will not delete the TFS branch, just the local git remote.

    git tfs branch --delete tfsRemoteName

## See also

* [clone](clone.md)
* [fetch](fetch.md)
* [checkin](checkin.md)
* [rcheckin](rcheckin.md)
