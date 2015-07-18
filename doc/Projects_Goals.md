## What v1.0 means

Here are some things we might want to do before 1.0:

* (required) Do `rebase` with LibGit2Sharp.
* (desired) speed up fetch/clone, possibly with a multi-argument form of Workspace.Get.

Feel free to pull other things in from the wishlist

## Wishlist

Here are some things I'd like to do with git-tfs:

* Add a `config` command to be able to set internal git options
* Manage changesets cross branches (permited by TFVC :( ) See #793
* Better display git errors when exit code != 0
* Clean tfs workspace "on the fly" when detecting that there is already an existing one (not removed the last time!)
* Create a TFS Label from a git tag
* Improve branch reliability when TFVC history is complexe

* Merge other forks
  * add a config param for using git commits in the tfs checkin comment (inspired by https://github.com/hammerdr/git-tfs/commit/7d9863775a53fd1664022cf3ff7e3920c4579f96)
* Use a newer build of henon/gitsharp to fix [the out of memory problem](https://github.com/git-tfs/git-tfs/issues/22).
* Clean up the object model.
* Faster import (clone and/or quick-clone) ([in progress](https://github.com/git-tfs/git-tfs/issues/173))
  * git-fast-import?
  * TFS get specific version, then add?
* Config params to support a more concise notation for TFS server URLs and/or a default server URL.
* A more nuanced fetch spec, similar to working directory specs from TFS.
* make tfs a "real" remote with git-remote-&lt;vcs&gt; ?
* Make tags for changesets off by default.
* More interesting workspace mappings, e.g. "$/Project1 -> Source; $/Project2/Libs -> Libs".


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
  unshelve
  checkin (dcommit?) - to rewrite commits, or not to rewrite them.
  find-rev

These commands are in git-svn, but I'm not convinced git-tfs needs them.
  rebase
  log
  commit-diff ?
  migrate ?

