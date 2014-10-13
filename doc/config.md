# Git-tfs config values

Git-tfs uses git's configuration system to track most of the important
information about repositories.

## Repository-wide configuration

By default, git-tfs sets these configuration values for the repository
during `git tfs init`.

* `core.ignorecase` is set to `true`, in an attempt to deal with
  casing issues.
* `core.autocrlf` is set to `false`. This will make git preserve all
  characters (including CR and LF) in all files. The reason for doing
  this is to make the result of `git tfs clone` as nearly identical,
  byte-wise, as possible, to the version in TFS.

There is other git-tfs configuration values for the repository:

* `git-tfs.batch-size` define the number of changesets fetched in the same time
  from TFS (Could also be set with the `clone` command). 
* `git-tfs.work-item-regex` could be used to define the regular expression to 
  extract workitems reference from commit message.
* `git-tfs.workspace-dir` is used to define a new directory as the workspace
  used by TFS to circumvent problem with long paths.
  The path should be the shortest possible (i.e. "c:\w")
* `git-tfs.export-metadatas` is set to `true` to export all metadata in the
  commit messages.

## Per-TFS remote

Git-tfs can map multiple TFS branches to git branches. Each TFS
branch is tracked as a separate "remote", and several config values
are stored for each branch.

Each git-tfs remote is assigned an ID. All of a remote's config keys
are prefixed with `tfs-remote.<id>.` So, for example, the full `url`
key for the remote `default` is `tfs-remote.default.url`.

* `url`
  is the URL of the TFS project collection.
* `legacy-urls`
  is a list, comma-separated, of previous URLs of the TFS project
  collection. For example, if you started your git-tfs clone from
  a 2005 or 2008 TFS server ('http://tfs:8080/tfs'), and the server
  migrated to 2010 or later, moving your project into a project
  collection ('http://tfs:8080/tfs/DefaultCollection'), then the
  `url` for your git-tfs remote should be the current url, and
  `legacy-urls` would be the old url.
* `repository`
  is the TFS repository path that was cloned to the root of your
  git-tfs project. Typically this is a TFS project path
  (`$/MyProject`), but it can be a subdirectory (`$/MyProject/Dir`)
  or a branch (`$/MyProject/trunk`).
* `username` and `password`
  are your TFS credentials. Normally, if you connect to a TFS
  server on your local Windows domain, you won't need to provide
  these values, because git-tfs defaults to using integrated
  authentication.
* `ignore-paths`
  is a regular expression of TFS paths to ignore when fetching.
* `autotag`
  can be set to `true` to make git-tfs create a tag for each
  TFS commit. This is disabled by default, because creating
  a lot of tags will slow down your git operations.
