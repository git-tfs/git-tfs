* Replace usage of the deprecated `git rebase --preserve-merges` with its sucessor `git rebase --rebase-merges`.  
  `git rebase --preserve-merges` was introduced in git v2.18.0. Using any older git version won't work.  
  HINT: Rebasing actual merge commits is nevertheless not something recommended by the git project itself,
  as it will lose any conflict resolution of the merge commit when it is rebased. 
  This is independent on the used commandline switch. (#1342)
* Improve performance by caching branch objects instead of looking them up over and over (#1286)
* Upgrade to .NET Framework 4.7.2 and upgrade NuGet dependencies (#1344 by @siprbaum)
* Correct error message shown when `git tfs clone` has an error (#1347 by @siprbaum)
* Add support for Visual Studio 2017. To use it set the environment variable `GIT_TFS_CLIENT` to `2017`.
  Multiple versions of VS2017 installed side by side, either as different editions like VS2017 Enterprise
  and Premium or different VS2017 minor versions are not offically supported yet.
  The current implementation will simply use the first version found. (#1348 by siprbaum)
* Add support for Visual Studio 2019. To use it set the environment variable `GIT_TFS_CLIENT` to `2019`.
  The same restrictions as for VS2017 apply, e.g. multiple versions of VS2019 installed side by side,
  either as different editions like VS2019 Enterprise and Premium or different VS2019 minor
  versions are not offically supported yet. The current implementation will simply use
  the first version found. (#1355 by siprbaum)
* Add support for checkin policies for Visual Studio 2017 and Visual Studio 2019 (#1356 by siprbaum)

