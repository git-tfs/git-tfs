With git-tfs, you could manage TFS shelvesets.

If you began to use git-tfs to move away of TFS, perhaps you want to import into git some existing shelvesets.
Let's see how to do it!

# Listing existing shelvesets

The first thing to do to use an existing shelveset is to find it among your shelvesets or the ones of the other users.
To do that, you should them with the `shelve-list` command.

## List your shelvesets.

If you want to list your shelvesets, just do:

    git tfs shelve-list

The output of this command looks like that:

	SND\vtccds_cp          feature, find the good client
	SND\vtccds_cp          better solution

For each changeset, you've first the user login and then, what we need, the name of the shelveset!
	
Note : 
* use the `--full` option to get more informations
* Shelvesets are sorted by date by default

## List the shelvesets of all the TFS users.

If you want to include in the list the shelvesets of all the users, just do:

    git tfs shelve-list -u=all
	
# Unshelve a shelveset

Once you've found the name of the shelveset, you could use the `unshelve` command to fetch the shelveset in your repository.
 
## Unshelve one of your shelveset

When you want to unshelve a shelveset, the changeset defined by the shelveset is automatically created in a new branch.
You only have to specify the name of the shelveset and the name of the branch that will be created. 

    git tfs unshelve "better solution" MyShelvesetBranch

Note: - the branch is automatically created on the parent commit of the changeset

## Unshelve a shelveset of another user

To do that, you should specify the login of the user in addition to the shelveset name:

    git tfs unshelve -u=loginUser "paul solution" MyBranch

# Create a shelveset

Perhaps, for a reason of another, you should want to create a shelveset to exchange code with user still using TFS ( :( for them )
A shelveset is created with the changes of the commits done in the current branch since the last commit fetch from TFS. 
To create the shelveset, just do:

    git tfs shelve "try to solve bug234"

# Delete a shelveset

When you no longer need the shelveset, you can delete it from TFS by using:

    git tfs shelve-delete "try to solve bug234"


