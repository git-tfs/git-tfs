Git-tfs could be easily used to work with TFS branches.

# Cloning

## Find the tfs branch to clone (optional)

Note: This command is not supported in TFS2008

If you don't know (or remember) the path of the project you want to clone on a TFS server,
 you could use the `list-remote-branches` command :

    git tfs list-remote-branches http://tfs:8080/tfs/DefaultCollection

You will have an output like that (showing branch linked to its parent branch) :

     $/project/trunk [*]
     |
     +- $/project/branch1
     |
     +- $/project/branch2
     |
     +- $/project/branch3
     |  |
     |  +- $/project/branche3-1
     |
     +- $/project/git_central_repo


     $/other_project/trunk [*]
     |
     +- $/other_project/b1
     |
     +- $/other_project/b2

    Cloning root branches (marked by [*]) is recommended!

    PS:if your branch is not listed here, perhaps you should convert the containing folder to a branch in TFS.

If you want to work with tfs branches, you should clone one of the root branches (marked by [*]) :
`$/project/trunk` or `$/other_project/trunk`

## Clone just the trunk

You could clone only the trunk of your project (and init the other branches later).
For that, use the command:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/project/trunk .

See [clone](../commands/clone.md) command if you should use a password or an author file, ...

Wait quite some time, fetching changesets from TFS is a slow process :(

Pros:
- quicker than cloning all the history
- get a smaller repository
- This command is supported in TFS2008

Cons:
- don't have all the whole history in the git repository (and that's the goal of a dvcs)
- ignore merges between branches! A branch merged in another one won't be materialized in the git repository and will never be.
__If you have merges, don't use this method!!__ If a merge is detected during the fetch, warning message will be displayed.
It is higly recommended to use the other method if you see one.

## Clone All History

First fetch all the source history (with all branches) in a local git repository:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/project/trunk . --branches=all

Wait quite some time, fetching all the changesets from TFS is even longer :(

Pros:
- you have all the whole history in the git repository
- manage merges between branches! A branch merged in another one will be materialized in the git repository.

Cons:
- slower than cloning just the main branch
- get a bigger repository
- This command is not supported in TFS2008

# Working with the trunk

Working with the trunk is like working without branches.
See [Working with no branches](working_with_no_branches.md) for more details.

# Working with branches

## Fetch, Pull and Check in
Working with branches, for the main commands (`fetch`, `pull` and `rcheckin`), is similar than for the trunk, git-tfs detecting which tfs remote to work with.

    //fetch the new changesets
    git tfs fetch
    //fetch and rebase on new  changesets
    git tfs pull -r
    //Check in TFS
    git tfs rcheckin

All the others actions are done through the `branch` command

## List branches

### Display already initialized Tfs remote

You will have the list of the already initialized Tfs branches and also the last changeset fetched.

    git tfs branch

### Display existing Tfs remote in current Tfs project

    git tfs branch -r

### Display existing Tfs remote in all the Tfs projects

    git tfs branch -r -all

# Initialize an existing remote TFS branch

## Initialize one tfs branch

    git tfs branch --init $/Repository/ProjectBranch

## Initialize one tfs branch, setting its name

    git tfs branch --init $/Repository/ProjectBranch myNewBranch

## Initialize all the tfs branches

    git tfs branch --init --all

## Create a TFS branch

### Create a TFS branch from scratch

You have first to checkout a commit corresponding to a tfs changeset already check in.
Once done, you just have to create a branch with the command:

    git tfs branch $/Repository/ProjectBranchToCreate --comment="Creation of my branch"

Git-tfs will create a branch on TFS with the path "$/Repository/ProjectBranchToCreate" where the first
 changeset comment will be "Creation of my branch". The name of the tfs remote will be extracted from the Tfs path
 and will be "ProjectBranchToCreate".

If you want to use a different name for your tfs remote, just specify it:

    git tfs branch $/Repository/ProjectBranchToCreate myNewProject --comment="Creation of my branch"

### Create a TFS branch from an existing git branch

Sometime, it's easier to create a local git branch, work in it and later decide to create a TFS branch.
When you are ready to check in your work in tfs, just checkout your local branch then use the command:

    git tfs branch $/Repository/ProjectBranchToCreate --comment="Creation of my branch"

The tfs branch will be created and all the git commits in the local branch will be checked in the Tfs branch \o/

## Rename a remote branch

This command will only rename the local remote and will not rename the branch in TFS.

    git tfs branch --move oldTfsRemoteName newTfsRemoteName

## Delete a remote branch

This command will only delete the local remote and will not delete the branch in TFS.

    git tfs branch --delete tfsRemoteName

## Manage merges with git-tfs

Git-tfs can handle merges (ie merge changesets) but there is some restrictions and you must follow some rules to do it well

### Fetch an existing merge changeset

If git-tfs encounter a merge changeset when fetching changesets, there is 2 possibilities:

* Either, the 2 parent changesets have already been fetched and a merge commit will be created locally (the merge changeset has been well managed).
* Either, the parent of the merged branch has not already been fetch. Then the merge changeset will be ignored and a normal commit will be created
 (the merge changeset has not been well managed). In this case, a warning will be displayed.

You should know that if you don't manage well merge changesets and that, in the future, you want to merge again the 2 branches, you will issue a lot of merge conflicts!

You could prevent that by doing 2 things:

* cloning using the `-branches=all` option which will manage well all the merge changesets
* always fetch the merge branch before fetching a merge changeset

Note: if you see a warning, you could correct that by resetting the tfs remote to a previous commit. Then fetch the merged branch and retry to fetch the branch.

### Merge 2 branches and checkin this merge in Tfs

Because merging 2 branches with git is a lot more easy than with Tfs, you could use git-tfs to do it.

If you want, for example, to merge the branch `b1` in the trunk `trunk`, you need that `b1` and `trunk` to be entirely checked in Tfs.
Once done, you could do the merge with git as a normal merge with 2 local git branches.
Then you have to check this commit into Tfs with the command `rcheckin` and a merge changeset will be created into Tfs.
