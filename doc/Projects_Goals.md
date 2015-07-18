## What v1.0 means

Here are some things we might want to do before 1.0:

* (required) replace (some of Process.Start and GitSharp) with LibGit2Sharp.
* (desired) speed up fetch/clone, possibly with a multi-argument form of Workspace.Get.
* (desired) use `git-config` to set command line options. For example, instead of `git tfs checkin --no-build-default-comment`, a user might be able to `git config tfs.builddefaultcomment=false` to get the same behavior.

Feel free to pull other things in from the wishlist

## Wishlist

Here are some things I'd like to do with git-tfs:

* Figure out [what 1.0 means](what-1.0-means.md) and get there.
* Merge other forks
  * non-default credentials (https://github.com/jhollingworth/git-tfs/commit/389aee4240630a1c30604d79e1194beaaa9a55bb)
  * no temp file (https://github.com/JamesDunne/git-tfs/commit/2524dc2700b2836721920e0a190aeabe894d7b8d) does this work faster? does it solve the 'large file' problem?
  * add a config param for using git commits in the tfs checkin comment (inspired by https://github.com/hammerdr/git-tfs/commit/7d9863775a53fd1664022cf3ff7e3920c4579f96)
* Use a newer build of henon/gitsharp to fix [the out of memory problem](https://github.com/git-tfs/git-tfs/issues/22).
* Use [libgit2/libgit2sharp](https://github.com/libgit2/libgit2sharp) for more git operations
* Clean up the object model.
* Translate TFS labels into git tags.
* Faster import (clone and/or quick-clone) ([in progress](https://github.com/git-tfs/git-tfs/issues/173))
  * git-fast-import?
  * TFS get specific version, then add?
* Config params to support a more concise notation for TFS server URLs and/or a default server URL.
* A more nuanced fetch spec, similar to working directory specs from TFS.
* make tfs a "real" remote with git-remote-&lt;vcs&gt; ?


## ToDo

git-svn has these commands:
  create/pull:
    clone (init + fetch)
    init
    fetch
    rebase
  commit:
    dcommit
    set-tree (lower-level form of dcommit)
    commit-diff (pushes diff of git revs to svn, does not require init)
  history:
    blame
    find-rev
    info
    log
  svn features:
    create-ignore
    propget
    proplist
    show-externals
    show-ignore
  internals:
    migrate (upgrade from one version of git-svn to another)

The commands that are implemented are:
  init
  fetch
  clone (init+fetch)
  shelve
  
The commands I plan to implement, in approximate order of priority:
  checkin (dcommit?) - to rewrite commits, or not to rewrite them.
  unshelve

These commands are in git-svn, but I'm not convinced git-tfs needs them.
  rebase
  find-rev
  log
  commit-diff ?
  migrate ?

Known bugs:
  None right now.

Other potential enhancements:
  * Use noodle for dependencies.
  * Query history in chunks, either in a background thread or just with 100 changeset chunks, in order to speed up the initial output from a fetch.
  * Support for TFS branches.
  * Make tags for changesets off by default.
  * More interesting workspace mappings, e.g. "$/Project1 -> Source; $/Project2/Libs -> Libs".

