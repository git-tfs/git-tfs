Sometimes, depending on where is located your repository folder on your drive and what is the file arborescence of your project,
you could face errors because you exceed the 259 characters limit of NTFS file system.

These errors could be :

`TF205022: The following path contains more than the allowed 259 characters:C:/Very/Long/Path/.git/tfs/default/workspace/Very/Long/Path/To/SubFolder/file.txt. Specify a shorter path.`

or

`Failed to stat file 'C:/Very/Long/Path/.git/tfs/default/workspace/Very/Long/Path/To/SubFolder/file.txt': The system cannot find the file specified.`

This is due to the fact that git-tfs use a temp folder located in the folder `.git/tfs/default/workspace` (`default` is use for the main branch but, if you use TFS branch feature of git-tfs, other --longer?-- folders following the branch name are used) where it create files received from TFS to create the git commits. The path of the files could became quickly very long... 

Note: If you look for this folder, you surely couldn't find it because it is created and deleted when a git-tfs command is run!

A first simple solution could be to move your folder from a very long path to a shorter one.
For exemple, move your repository folder from :
`C:\A\Very\Very\Long\Path\To\My\GitTfs\Repository` to `C:\repo`.

Another better solution if you faced this problem is to use a custom workspace directory!

To set a custom workspace directory, just run the command (in a already existing repository):
`git config git-tfs.workspace-dir c:\ws`

Note: if you set this setting after having faced an error perhaps you should run a cleanup before fetching again ( `git tfs cleanup` )

But it is possible that you faced the problem when you try to clone a big repository and, thus, you could not run the previous command because the repository is not yet created :(
So, in this case, you should just init the repository, set the custom workspace directory, then fetch the tfs changesets :

    git tfs init http://server/tfs $/Project/trunk project
    cd project
    git config git-tfs.workspace-dir c:\ws
    git tfs pull

More informations : See [here](https://github.com/git-tfs/git-tfs/issues/314) or [there](https://github.com/git-tfs/git-tfs/issues/430) or [there](https://github.com/git-tfs/git-tfs/pull/266)