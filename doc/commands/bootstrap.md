## Summary

The bootstrap command allows you to quickly configure a cloned git repository for use with the original TFS repository.

## Synopsis

    Usage: git-tfs bootstrap [parent-commit]
    where options are:
    
        -i, --id, --tfs-remote, --remote
            (Type: Value required, Value Type:[String])
            An optional remote ID, useful if this repository will track multiple TFS repositories.
    
        -d, --debug
            (Type: Flag, Value Type:[Boolean])
            Show lots of output.
    
        -H, -h, --help
            (Type: Flag, Value Type:[Boolean])
            ShowHelp
    
        -V, --version
            (Type: Flag, Value Type:[Boolean])
            ShowVersion
    

## How to use this

`bootstrap` is useful if you create a TFS clone and share it with a colleague who then needs to interact with TFS. While two identical invocations of git tfs clone will produce identical repositories, git clone is always going to be faster than git tfs clone. So, I would guess that most people who want to collaborate on a TFS project using git will benefit from this command.

    [user 1] git tfs clone http://blah/blah/blah $/blah
    [user 1] cd blah
    [user 1] git remote add shared git@someplace:shared/repo.git
    [user 1] git push shared master

    [user 2] git clone git@someplace:shared/repo.git
    [user 2] cd repo
    [user 2] git tfs bootstrap

At this point, `user2` will be able to use all the normal git-tfs commands.

## Using bootstrap in a repository with TFS branches

If you cloned a git tfs repository where TFS branches has already been initialized, and that you planned to use `bootstrap`, you _MUST_ `bootstrap` FIRST the trunk also named 'master' (that will be bootstrapped as `tfs/default`):

    git clone git@someplace:shared/repo.git
    cd repo
    //checkout the 'master' branch if that's not already the case
    git checkout master
    //bootstrap the trunk
    git tfs bootstrap

Then, you could `bootstrap`, when you want or when you need, all the missing tfs remotes:

    //bootstrap a tfs branch 'myBranch' for example
    git checkout myBranch
    //bootstrap the tfs branch
    git tfs bootstrap

Note: When you `bootstrap` one branch, the `bootstrap` command will automatically bootstrap also all the TFS remotes that will be found when following the commit history of the branch you are bootstrapping. So, perhaps, you won't have to `bootstrap` all the TFS remotes one by one ;)
