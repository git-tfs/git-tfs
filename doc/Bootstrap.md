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

    [user1] git tfs clone http://blah/blah/blah $/blah
    [user1] cd blah
    [user1] git remote add shared git@someplace:shared/repo.git
    [user1] git push shared master
    [user2] git clone git@someplace:shared/repo.git
    [user2] cd repo
    [user2] git tfs bootstrap

At this point, `user2` will be able to use all the normal git-tfs commands.