## Summary

Checkins path of commits from last found TFS-commit to HEAD with comments provided for corresponding git commits. Preserves merge commits.

rcheckin differs from [checkin](checkin.md) in that the latter squashes the commits into a single TFS checkin, while rcheckin uses a series of commits.


## Features
[Special actions in commit messages](../special-actions-in-commit-messages.md) can be inserted, to associate or resolve TFS work items or override checkin policies.

## Synopsis

    Usage: git-tfs rcheckin [options]
    where options are:
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
      -A, --authors=VALUE        Path to an Authors file to map TFS users to Git
                                   users (will be kept in cache and used for all
                                   the following commands)
      -a, --autorebase           Continue and rebase if new TFS changesets found
          --ignore-merge         Force check in ignoring parent tfs branches in
                                   merge commits
      -m, --comment=VALUE        A comment for the changeset
          --no-build-default-comment
                                 Do not concatenate commit comments for the
                                   changeset comment.
          --no-merge             Omits setting commit being checked in as parent,
                                   thus allowing to rebase remaining onto TFS
                                   changeset without exceeding merge commits.
      -f, --force=VALUE          The policy override reason.
      -w, --work-item[=VALUE1:VALUE2]
                                 Associated work items
                                   e.g. -w12345 to associate with 12345
                                   or -w12345:resolve to resolve 12345
      -c, --code-reviewer=VALUE  Set code reviewer
                                   e.g. -c "John Smith"
      -s, --security-reviewer=VALUE
                                 Set security reviewer
                                   e.g. -s "John Smith"
      -p, --performance-reviewer=VALUE
                                 Set performance reviewer
                                   e.g. -p "John Smith"
      -n, --checkin-note=VALUE1:VALUE2
                                 Add checkin note
                                   e.g. -n "Unit Test Reviewer" "John Smith"
          --no-gate              Disables gated checkin.
          --author=VALUE         TFS User ID to check in on behalf of
          --ignore-missing-files When applying an edit operation, ignores items
                                   that were expected to already be in TFS but were
                                   not found. Missing files indicates bad
                                   synchronization at some point. Use this option
                                   with care.
          --add-missing-files    When applying an edit operation, add items that
                                   were expected to already be in TFS but were not
                                   found. Missing files indicates bad
                                   synchronization at some point. Use this option
                                   with care. This option is ignored if ignore-
                                   missing-files option is used.


## Examples

### Simple

Suppose you have 

    A [tfs/default, C1] <- B <- C [master, HEAD]

After executing `git tfs rcheckin` you would have

    A [C1] <- B [C2] <- C [master, HEAD, tfs/default, C3]

Comments to B and C in TFS are preserved (same as in git excluding `git-tfs-id` markings).

### Merge preserving

Suppose you have

    A [tfs/default, C1] <- B <- C <- D <- E [master, HEAD]
     \                              /
       M <------------------------ N

So that M and N were commits on some branch and C is first parent of D which is merge-commit. After executing `git tfs rcheckin` you would have

    A [C1] <- B [C2] <- C [C3] <- D [C4] <- E [tfs/default, master, HEAD, C5]
     \                           /
       M <--------------------- N

Comments on B, C and E are preserved. Comment on D will have following structure:

    Comment from D
      Comment from M
      Comment from N

TFS can't see M and N, so in order to preserve commit messages in its history rcheckin formats messages in this way.

### Checkin a merge changeset

To checkin a merge commit (done with git) as a merge changeset, the merge commit should be done on top of the 2 tfs remotes like that :

    A [C1] <- B [C2] <- C [C4] <- D [tfs/default, C6] <- E [master, HEAD]
     \                                                  /
       M [C3] <--------------- N [tfs/branch, branch, C5]

`E` is the merge commit that should be checked in as a merge changeset.

After the `rcheckin` of `tfs/default`, you should have an history that looks like that :

    A [C1] <- B [C2] <- C [C4] <- D [C6] <-------------- E [tfs/default, master, HEAD, C7]
     \                                                  /
       M [C3] <--------------- N [tfs/branch, branch, C5]

### Rcheckin on a branch

To checkins commits on the `tfs/myBranch` branch:

    git tfs rcheckin -i myBranch

### Rcheckin on current branch

To pull all the changeset of the current branch:

    git tfs rcheckin -I

The current branch depend of the git commit that is currently checkouted. Git-tfs will look in the history
to find the appropriate branch to rcheckin.

### Rcheckin commits of other users

You could check in commits of other users in TFS. To be able to do that, you must specify the path toward an author file with the option `--authors` which permit to match TFS users to git users. See [clone](clone.md) command for more informations about the format.

    git tfs rcheckin --authors="c:\path\to\authors.txt"

Note : To be able to check in commits of other users, you should have a special right defined in TFS. To activate, Right click on the TFS project, then "Security..." and select "Allow" to the "Check in other user's changes" permission (CheckinOther). 

## Internals

Internally rcheckin takes from `rev-list --ancestry-path --first-parent tfs/default..HEAD` commit which is the closest derivative of tfs/default, checkins it to TFS, fetches newly checkined commit back and rebases HEAD's tail onto it. Then it repeats this process until no more commits in the ancestry-path. So, technically speaking you'll have new line of commits with same changes. It is important to know if you have some work based on some of commits being rcheckin-ed.

## Known problems

Suppose you have situation similar to described in 'Merge preserving' section earlier but branching takes place from one of commits being rcheckin-ed, for example from B:

    A [tfs/default, C1] <- B <- C <- D <- E [master, HEAD]
                            \       /
                             M <-- N

Due to nature of rebase and workflow described in 'Internals' section after B will be checked in to TFS we got back new commit, say B'. And B' is not parent of M. So, when rcheckin will finish you'll have

    A [C1] <- B' [C2] <- C' [C3] <- D' [C4] <- E [tfs/default, master, HEAD, C5]
     \                             /
       B <---------- M <--------- N

Thus, commit B will stay in history as parent of M and equivalent commit B' will be fetched from TFS. It is confusing and hopefully will be fixed someday.



## See also

* [checkin](checkin.md)
* [checkintool](checkintool.md)
