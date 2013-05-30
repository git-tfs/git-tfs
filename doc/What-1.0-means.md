Here are some things we might want to do before 1.0:

* (required) replace (some of Process.Start and GitSharp) with LibGit2Sharp.
* (desired) speed up fetch/clone, possibly with a multi-argument form of Workspace.Get.
* (desired) use `git-config` to set command line options. For example, instead of `git tfs checkin --no-build-default-comment`, a user might be able to `git config tfs.builddefaultcomment=false` to get the same behavior.

Feel free to pull other things in from the [[Wishlist]].