Git-tfs could be easily used to work with TFS.

# Cloning

## Find the tfs branch to clone (optional)

Note: This command is not supported in TFS2008

If you don't know (or remember) the path of the project you want to clone on a TFS server,
 you could use the `list-remote-branches` command :

    git tfs list-remote-branches http://tfs:8080/tfs/DefaultCollection

You will have an output like that (showing children branches linked to there parent branch) :

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

You could clone only the trunk of your project (and initialize the other branches later).
For that, use the command:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/project/trunk . --branches=none

See [clone](../commands/clone.md) command if you should use a password or an author file, ...

Wait quite some time, fetching changesets from TFS is a slow process :(

Pros & Cons: See [Manage Tfs branches](manage_tfs_branches.md).

# Working with the trunk

## Fetch Tfs changesets

To fetch the new changesets, just do:

    git tfs fetch

but be aware that this command DON'T include the new changesets in your git branch (like the git fetch).
To do so, you have to use the `merge` or `rebase` git command or prefer the git-tfs `pull` command.

## Merge or Rebase fetched Tfs changesets

### Rebase

If you want to rebase all your local commits onto the newly fetch changesets, use the command:

    git tfs pull -r

### Merge

You could also merge your commits into the ones fetch with the command:

    git tfs pull

but this solution is discouraged because merged commits couldn't be push to tfs as this and you will finish with strange git history!

## Check in TFS

Once you've rebased your commits onto the newly fetched changesets, you could check them in TFS:

    git tfs rcheckin
