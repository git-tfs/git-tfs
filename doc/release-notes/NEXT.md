* Replace usage of the deprecated `git rebase --preserve-merges` with its sucessor `git rebase --rebase-merges`.  
  `git rebase --preserve-merges` was introduced in git v2.18.0. Using any older git version won't work.  
  HINT: Rebasing actual merge commits is nevertheless not something recommended by the git project itself,
  as it will lose any conflict resolution of the merge commit when it is rebased. 
  This is independent on the used commandline switch. (#1342)
* Improve performance by caching branch objects instead of looking them up over and over (#1286)
* Upgrade to .NET Framework 4.7.2 and upgrade NuGet dependencies (#1344 by @siprbaum)
* Correct error message shown when `git tfs clone` has an error (#1347 by @siprbaum)
