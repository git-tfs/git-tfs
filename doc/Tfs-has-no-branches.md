For those who aren't very familiar with TFS, you may come across a TFS repository whose directories are just Folders, not branches.

In this case, the command `git tfs list-remote-branches tfs-url` will return the result `No TFS branches were found!`.  Similarly, `git tfs clone` will not work.

To fix this, open 'Source Control Explorer'. For each folder corresponding to a branch, right click on your source folder and select 'Branching and Merging' > 'Convert to branch'.