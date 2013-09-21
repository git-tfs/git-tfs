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

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/project/trunk . --with-branches

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

Working with branches, for the main commands (`fetch`, `pull` and `rcheckin`), is similar than for the trunk
 but with specifying the tfs remote with the option `-i`.
 
    //fetch the new changesets
    git tfs fetch -i branch1
    //fetch and rebase on new  changesets
    git tfs pull -r -i branch1
    //Check in TFS
    git tfs rcheckin -i branch1

All the others actions are done throught the `branch` command

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


