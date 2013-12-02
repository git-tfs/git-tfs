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

To init `$/Project1` from your TFS 2010 server `tfs`
into a new directory `Project1`, do this:

    git tfs reset-remote 3dcce821d7a20e6b2499cdd6f2f52ffbe8507be7

Due to an optimisation of git-tfs, you then have to fetch the changesets with the `--force` option:

    git tfs fetch --force
	
## Use cases

### "Rewrite" your TFS history

When you use [checkintool], you will end with an history looking like that :

    A[C1] <- B[C2] <- C[C3] <- D[C4] <- E[C5,tfs/default, master, HEAD]
     \      /  \     /  \     /  \     /
      --M--     --N--    --O--    --P--

Perhaps, you would like to have a better historic, whithout all these merges, and, especially, following the TFS history.
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

## See also

* [fetch](fetch.md)
