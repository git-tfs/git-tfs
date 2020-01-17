* Remove support for building an MSI package. It was broken since 6732259 ("Moving source in the 'src' folder", 2017-10-13)
  and the official distribution channel for binaries is a ZIP file or a chocolatey package.
* Added a new command to create a mapping file of changet ids and commit ids.
* Remove support for VS2012 & VS2013 (#1277)
* Update libgit2sharp to 0.26 (#1279)
* Fix the Verify command (#1297)
* Improve way to pass credentials in the CLI (#1299)
* Creates a mapping file of ChangesetId-Commit values (#1309)
* Fixed authors file not being found for the copy to .git/git-tfs_authors due to git-tfs changing the current directory before initiating the copy. (#1310)
* Fix NRE instead of suppressing and prevent invalid instances of GitCommit (#1311)
