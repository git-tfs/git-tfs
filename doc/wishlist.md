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
