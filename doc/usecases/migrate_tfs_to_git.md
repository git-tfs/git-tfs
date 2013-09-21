Git-tfs could be easily used to migrate source history from TFSVC to a git repository.


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


