## Summary

The `init-branch` command creates a git branch for an existing TFS branch (or all) and fetch all the changeset of the TFS branch.

To use this command, you should have cloned only the trunk folder in TFS (and not the whole repository). See [clone](clone.md) command.

## Synopsis
    Usage: git-tfs init-branch [$/Repository/path <git-branch-name-wished>|--all]
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
          --all                  Clone all the TFS branches (For TFS 2010 and
                                   later)
      -b, --tfs-parent-branch=VALUE
                                 TFS Parent branch of the TFS branch to clone
                                 (TFS 2008 only! And required!!) ex: $/Repository/ProjectParentBranch
      -u, --username=VALUE       TFS username
      -p, --password=VALUE       TFS password
## Examples

Suppose you have on TFS:

    A <- B <- C <- D <- E  $/Repository/ProjectTrunk
               \                              
                M <- N     $/Repository/ProjectBranch

You should have done (to clone only the trunk) :

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Repository/ProjectTrunk

Then use `init-branch` like this :
### Init a TFS branches using auto-naming of your git branch
    git tfs init-branch $/Repository/ProjectBranch

### Init a TFS branches naming yourself the git branch
    git tfs init-branch $/Repository/ProjectBranch myNewBranch

### Init all the TFS branches
    git tfs init-branch --all
This command init all the branches not already done and ignore existing ones.

### Init a branch with TFS2008

TFS2008 doesn't permit to know the parent of a branch. You should find it yourself with TFS and use the parameter `--tfs-parent-branch` to give it to the `init-branch` command:

    git tfs init-branch --tfs-parent-branch=$/Repository/ProjectParentBranch $/Repository/ProjectBranch

### Authentication

For the use of parameters `--username` and `--password`, see the [clone](clone.md) command.

### Map TFS users to git users

For the use of parameter `--authors`, see the [clone](clone.md) command.

## And Now...

After that your branch is created, you should use the commands [fetch](fetch.md) and [checkin](checkin.md) or [rcheckin](rcheckin.md) with the parameter `-i` to work with the TFS branch.

## See also

* [clone](clone.md)
* [fetch](fetch.md)
* [checkin](checkin.md)
* [rcheckin](rcheckin.md)
