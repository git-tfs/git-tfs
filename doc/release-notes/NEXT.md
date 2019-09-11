* Remove support for building an MSI package. It was broken since 6732259 ("Moving source in the 'src' folder", 2017-10-13)
  and the official distribution channel for binaries is a ZIP file or a chocolatey package.
* Fix regression introduced by https://github.com/git-tfs/git-tfs/commit/742a29f5eb9e4a6117988147f96d58f3337f2575
