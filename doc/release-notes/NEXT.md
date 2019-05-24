* Replace usage of the deprecated `git rebase --preserve-merges` with its sucessor `git rebase --rebase-merges`.  
  `git rebase --preserve-merges` was introduced in git v2.18.0. Using any older git version won't work.  
  HINT: Rebasing actual merge commits is nevertheless not something recommended by the git project itself,
  as it will lose any conflict resolution of the merge commit when it is rebased. 
  This is independent on the used commandline switch. (#1342)
* Improve performance by caching branch objects instead of looking them up over and over (#1286)
* Pin paket version to 5.251.0 to avoid spurious changes during build in paket generated files
* Update ReportGenerator from 3.0.2 to 4.7.1
* Update Moq from 4.3.1 to 4.14.7
* Update LibGit2Sharp from 0.26.1 to 0.26.2
* Update NLog from 4.5.10 to 4.7.5
* Update Cake from 0.30 to 0.38.5 and Cake.Git from 0.19 to 0.22
* Update OpenCover from 4.6.519 to 4.7.922
* Update GitVersion.CommandLine from 3.6.5 to 5.5
