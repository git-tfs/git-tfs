* Replace usage of the deprecated `git rebase --preserve-merges` with its sucessor `git rebase --rebase-merges`.  
  `git rebase --preserve-merges` was introduced in git v2.18.0. Using any older git version won't work.  
  HINT: Rebasing actual merge commits is nevertheless not something recommended by the git project itself,
  as it will lose any conflict resolution of the merge commit when it is rebased. 
  This is independent on the used commandline switch.