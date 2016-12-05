Git-tfs could be easily used to migrate source history from TFSVC to a git repository.

## Migrate toward external git repository

### Fetch TFS (TFVC) History

Depending on the TFVC changesets history, git-tfs could have more or less difficulties to retrieve the history
(Especially in case of renamed branches).
Here are described, in order, the different options that could be tried to retrieve as much history as possible. 
If you are not insterested by all the history (because it could be very long), you could just choose the option that suits you the best...

#### Fetch all the history, for all branches
First fetch all the source history (with all branches) in a local git repository:

    git tfs clone https://tfs.codeplex.com:443/tfs/Collection $/project/trunk . --branches=all

See [clone](../commands/clone.md) command if you should use a password or an author file
 (recommended if you want an email address instead of a windows login in commit messages), ...

Wait quite some time, fetching changesets from TFS is a slow process :(

#### Fetch all the history, for only merged branches

If only the complete history of the main branch is important for you, you could fetch only the history
of the main branch and onk=ly the branches merged into it.

To do that, do not specify the `--branches` or use the default value of the option `--branches=auto`, like that:

    git tfs clone https://tfs.codeplex.com:443/tfs/Collection $/project/trunk . --branches=auto

#### Fetch all the history, for just the main branch (ignoring all the other branches)

Unfortunately, the way how changesets are store in TFVC history, make that git-tfs is not able to handle every cases.
If you still want to retrieve the history, one of the solution is to fetch the history of only one branch ignoring 
all the other branches and the merge changesets.


To do that, use the option `--branches=none`, like that:

    git tfs clone https://tfs.codeplex.com:443/tfs/Collection $/project/trunk . --branches=none

#### Ignoring history before a specific changeset

Sometimes, the history is too long and take too much time too retrieve. Or too messy and git-tfs fails to retrieve it :(
In this case, you could try to retrieve less history by passing to git-tfs, the id of a changeset from where to fetch the history.

To do that, use the option `--changeset=3245`, and run:

    git tfs clone https://tfs.codeplex.com:443/tfs/Collection $/project/trunk . --changeset=3245

#### Fetch only the last changeset

In the last resort, when none of the solution before has worked, your only solution remains to clone only the last Changeset.
Even if it's the way that Microsoft recommend to migrate to git (surely because they doesn't provide a better way!?!), that's 
for us the last solution to try (or if you don't care about your history...).
You could do it by running:

    git tfs quick-clone https://tfs.codeplex.com:443/tfs/Collection $/project/trunk .

#### Speed up process by providing a `.gitignore` file

For every way to clone, you could provide to git-tfs a `.gitignore` file.
That way, all the files that will be ignored won't be downloaded, speeding the process.
That could be particulary usefull to ignore dependencies packages that has been commited but that should not...

### Clean commits (optional)

Clean all the git-tfs metadatas from the commit messages:

    git filter-branch -f --msg-filter "sed 's/^git-tfs-id:.*$//g'" -- --all

Then verify that all is ok and delete the folder `.git/refs/original` ( to delete old branches)

If you want to keep the old changesets ids in a more human format, you could use instead something like:

    git filter-branch -f --msg-filter "sed 's/^git-tfs-id:.*;C\([0-9]*\)$/Changeset:\1/g'" -- --all

Note: if you do that, you won't be able to fetch tfs changesets any more.
You should do that if you want to migrate definitively away of TFS!

### Add a remote toward git central repository

Add a remote in your local repository toward an empty git (bare) central repository :

    git remote add origin https://github.com/user/project.git

### Push all the source history

Push all the branches on your remote repository:

    git push --all origin

Migration is done!

## Migrate toward TFS2013 git repository (keeping workitems)

### Migrate your work-items
Use [Total Tfs Migration](https://totaltfsmigration.codeplex.com/) (or even [here](https://github.com/pmiossec/TotalTfsMigrationTool) for a version with some more bugfixes) to migrate your work-items from your old TFS(VC) project to your new TFS(Git) project.

When process is done, you should have in the subdirectory `map` of the application,
 a file named `ID_map_[Project1]_to_[Project2].txt` containing the mapping between
 old work items and new work-items ids.

Note :
- For the moment, 'Tfs Integration Platform' doesn't support TFS2013 and consequently doesn't permit to migrate work items to a TFS(Git) project.
- If one day, it is possible, mapping between old work items and new work-items ids is store in the table 'RUNTIME_MIGRATION_ITEMS' of the database 'Tfs_IntegrationPlatform'.
Extract the data to create a file with each line formatted following: OldWorkItemId|NewWorkItemId

### Fetch All History

First fetch all the source history (with all branches) in a local git repository exporting work-items metadatas (using the mapping file obtained in the previous step):

    git tfs clone https://tfs.codeplex.com:443/tfs/Collection $/project/trunk . --branches=all --export --export-work-item-mapping="c:\workitems\mapping\file.txt"

See [clone](../commands/clone.md) command if you should use a password or an author file
 (recommended if you want an mail address instead of a windows login in commit messages), ...

Wait quite some time, fetching changesets from TFS is a slow process :(

### Clean commits (optional)

Clean all the git-tfs metadatas from the commit messages:

    git filter-branch -f --msg-filter "sed 's/^git-tfs-id:.*$//g'" -- --all

Then verify that all is ok and delete the folder `.git/refs/original` ( to delete old branches)

Note: if you do that, you won't be able to fetch tfs changesets any more.
You should do that if you want to migrate definitively away of TFS(VC)!

### Add a remote toward TFS(Git) repository

Add a remote in your local repository toward an empty git (bare) central repository :

    git remote add origin http://tfsserver:8080/tfs/defaultcollection/_git/MyGitProject

### Push all the source history

Push all the branches on your remote repository:

    git push --all origin

Migration is done!

