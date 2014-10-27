## Summary

The `reset-remote` command permit to reset the current TFS remote to a previous changeset of the branch.
This way, you are able to fetch again the next changesets.

## Synopsis

```
Usage: git-tfs reset-remote commit-sha1-ref
  ex : git tfs reset-remote 3dcce821d7a20e6b2499cdd6f2f52ffbe8507be7
  -h, -H, --help
  -V, --version
  -d, --debug                Show debug output about everything git-tfs does
```

## Example

To reset the remote containing the commit '3dcce821d7a' to this commit, do this:

    git tfs reset-remote 3dcce821d7a

Due to an optimisation of git-tfs, you then have to reset also the local git branch (the wrong commits should not be in your local branch) and then fetch the changesets :

    git tfs fetch

## Use cases

### "Rewrite" your TFS history

When you use [checkintool], you will end with an history looking like that :

    A[C1] <- B[C2] <- C[C3] <- D[C4] <- E[C5,tfs/default, master, HEAD]
     \      /  \     /  \     /  \     /
      --M--     --N--    --O--    --P--

Perhaps, you would like to have a better historic, without all these merges, and, especially, following the TFS history.
You have to reset the `tfs/default` remote to the `A` commit and then fetch to have an history like that:

    A[C1] <- B[C2] <- C[C3] <- D[C4] <- E[C5,tfs/default, master, HEAD]
  

### Repair your repository

I don't know how you did it ;) but it could happens that you mess your repository and end with an history like that :

    A[C1] <- B <- C <- D[C2,tfs/default, master, HEAD]

with the 2 commits B and C that aren't changesets in TFS.

You have to reset the `tfs/default` remote to the `A` commit and then fetch to have an history like that:

    A[C1] <-D'[C2,tfs/default, master, HEAD]

### Fetch a merged changeset that you missed

Now, git-tfs could manage merge changesets. But at the time of the fetch, perhaps the merge changeset
have not been managed because the merged branch was not initialized in the git repository.
In this case, you should have seen a 'warning' message...

You have an history looking like that (without the merge):

    X[C34] <- ... <- A[C101] <- B[C102] <- C[C112] <- D[C114] <- E[C115,tfs/default, master, HEAD]
       \                                              
        ----- ... <- M[C109] <- N[C110] <- O[C111] <- P[C113,tfs/branch]

instead of (with the merge):

    X[C34] <- ... <- A[C101] <- B[C102] <- C[C112] <- D[C114] <- E[C115,tfs/default, master, HEAD]
       \                                                        /
        ----- ... <- M[C109] <- N[C110] <- O[C111] <- P[C113,tfs/branch]

Now that you have initialized this branch `tfs/branch`, you could be interested by managing this merge changeset to have
 the same history than the TFS one!
Then, you have to reset the `tfs/default` (or another) remote to the last changeset before the merge changeset (here D[C114]) :

    git tfs reset-remote shaOfCommitD

 and fetch again.

### Reset a remote from a commit not belonging to the tfs remote to reset

If you try to reset a remote to a pure git commit (which does not correspond to a fetched tfs changeset),
git-tfs will warn you:

    error : the current commit does not belong to a tfs remote!

If you try to reset to a commit from a different TFS remote, you'll see a similar message:

    error : the commit where you want to reset the tfs remote does not belong to the current tfs remote "currentRemoteName"!

When you see this message, ensure that you're updating the remote that you expect to. You can do this with the `-i <remote>` or `-I` option.
To reset the tfs remote anyway, use the `--force` flag!
 
## See also

* [fetch](fetch.md)
