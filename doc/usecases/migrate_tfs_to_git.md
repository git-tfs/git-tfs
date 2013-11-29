Git-tfs could be easily used to migrate source history from TFSVC to a git repository.

## Migrate toward external git repository

### Fetch All History

First fetch all the source history (with all branches) in a local git repository:

    git tfs clone https://tfs.codeplex.com:443/tfs/Collection $/project/trunk . --with-branches

See [clone](../commands/clone.md) command if you should use a password or an author file
 (recommended if you want an mail adresse instead of a windows login in commit messages), ...

Wait quite some time, fetching changesets from TFS is a slow process :(
 
### Clean commits (optional)

Clean all the git-tfs metadatas from the commit messages:

    git filter-branch -f --msg-filter 'sed "s/^git-tfs-id:.*$//g"' -- --all
	
Then verify that all is ok and delete the folder `.git/refs/original` ( to delete old branches)

Note: if you do that, you won't be able to fetch tfs changesets anymore.
You should do that if you want to migrate definitively away of TFS!

### Add a remote toward git central repository

Add a remote in your local repository toward an empty git (bare) central repository :

    git remote add origin https://github.com/user/project.git

### Push all the source history

Push all the branches on your remote repository:

    git push --all origin

Migration is done!

## Migrate toward TFS2013 git repository (keeping workitems)

### Migrate your workitems
Use [Total Tfs Migration](https://totaltfsmigration.codeplex.com/) to migrate your workitems from your old TFS(VC) project to your new TFS(Git) project.

When process is done, you should have in the subdirectory `map` of the application,
 a file named `ID_map_[Project1]_to_[Project2].txt` containing the mapping between 
 old work items and new workitems ids.

Note :
- For the moment, 'Tfs Integration Platform' doesn't support TFS2013 and consequently doesn't permit to migrate work items to a TFS(Git) project.
- If one day, it is possible, mapping between old work items and new workitems ids is store in the table 'RUNTIME_MIGRATION_ITEMS' of the database 'Tfs_IntegrationPlatform'.
Extract the datas to create a file with each line formated following: OldWorkItemId|NewWorkItemId

### Fetch All History

First fetch all the source history (with all branches) in a local git repository exporting workitems metadatas (using the mapping file obtained in the previous step):

    git tfs clone https://tfs.codeplex.com:443/tfs/Collection $/project/trunk . --with-branches --export --export-work-item-mapping="c:\workitems\mapping\file.txt"

See [clone](../commands/clone.md) command if you should use a password or an author file
 (recommended if you want an mail adresse instead of a windows login in commit messages), ...

Wait quite some time, fetching changesets from TFS is a slow process :(
 
### Clean commits (optional)

Clean all the git-tfs metadatas from the commit messages:

    git filter-branch -f --msg-filter 'sed "s/^git-tfs-id:.*$//g"' -- --all
	
Then verify that all is ok and delete the folder `.git/refs/original` ( to delete old branches)

Note: if you do that, you won't be able to fetch tfs changesets anymore.
You should do that if you want to migrate definitively away of TFS(VC)!

### Add a remote toward TFS(Git) repository

Add a remote in your local repository toward an empty git (bare) central repository :

    git remote add origin http://tfsserver:8080/tfs/defaultcollection/_git/MyGitProject

### Push all the source history

Push all the branches on your remote repository:

    git push --all origin

Migration is done!

