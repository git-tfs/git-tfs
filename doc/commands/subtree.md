## Summary

The `subtree` command allows multiple TFS repository paths to be combined in one Git repo.  Each individual TFS repository path is added as a sub directory in the local Git repo.  Future uses of `fetch`, `shelve`, or `checkin` will perform the operation against all subtrees.  The primary benefit of this is to checkin as one changeset a change that spans multiple TFS repository paths.

## Synopsis

	Usage: git-tfs subtree [add|pull|split] [options] [remote | ( [tfs-url] [repository-path] )]
	  -h, -H, --help
	  -V, --version
	  -d, --debug                Show debug output about everything git-tfs does
	  -i, --tfs-remote, --remote, --id=VALUE
								 The remote ID of the TFS to interact with
								   default: default
	  -A, --authors=VALUE        Path to an Authors file to map TFS users to Git
								   users (will be kept in cache and used for all
								   the following commands)
	  -p, --prefix=VALUE
		  --squash
		  --all, --fetch-all
		  --parents
	  -l, --with-labels, --fetch-labels
								 Fetch the labels also when fetching TFS
								   changesets
	  -b, --bare-branch=VALUE    The name of the branch on which the fetch will
								   be done for a bare repository
		  --force                Force fetch of tfs changesets when there is
								   ahead commits (ahead commits will be lost!)
	  -x, --export               Export metadatas
		  --ignore-regex=VALUE   a regex of files to ignore
		  --except-regex=VALUE   a regex of exceptions to ingore-regex
	  -u, --username=VALUE       TFS username


## Use

### Subtree Add

When executing the following command:
```
git-tfs subtree add -p=[prefix] [tfs-url] [repo-path]
```
git-tfs will:
* create a git remote named "default" and one named "default_subtree/[prefix]", which is the subtree remote.
* Fetch the subtree remote, pulling changes from [repo-path]
* execute `git subtree add --prefix=[prefix] [subtree remote] -m [commit msg]` to add the code from the repo as a subtree

Afterwards, the revision history will look like this:
```
commit e75165c22d0415613129cbc5456cc7b491ec6903
Merge: 845abef 5618bb9
Author: Gordon Burgett <my.email@gmail.com>
Date:   Wed Apr 10 13:38:32 2013 -0500

     Add 'SubtreeProject/' from commit '5618bb9065d9df8b059e7218db1a639e38a54f22'
    
    git-tfs-id: [http://my.server.url:8080/tfs/myco];C19373
    
    git-subtree-dir: SubtreeProject
    git-subtree-mainline: 845abef174122eb5f5985d899a3875be965654c7
    git-subtree-split: 5618bb9065d9df8b059e7218db1a639e38a54f22

commit 5618bb9065d9df8b059e7218db1a639e38a54f22
Author: Someone Else <someone.else@company.com>
Date:   Mon Apr 8 21:20:27 2013 +0000

    Fixes #1094 - some other task
    
    git-tfs-id: [http://my.server.url:8080/tfs/myco]$/Production/SubtreeProject/MAIN;C19370

```

From this point on, a `git-tfs pull` against the owning remote ("default") will pull all changesets across all known subtree remotes (in this case "default_subtree/SubtreeProject") and will apply the changesets.
A `git-tfs checkin` will pend all changes across all subtree remotes in the same workspace, as will a `shelve`.

At this point the subtree can be split using a `git subtree split -P <prefix>` command.  The split subtree will behave exactly as a normal git-tfs repository.  Pulls, shelves and checkins can be performed on the split subtree.

### Subtree Pull

A Subtree Pull will perform a fetch and merge of the specified prefix only.  This can be useful to get the latest changes of only one TFS project.
Example:
```
$ git-tfs subtree pull -p=SubtreeProject
executing subtree pull
Already up-to-date.
```

### Subtree Split

A Subtree Split extracts an artificial revision history containing only commits that affected the files in the specified subdirectory.  This is identical to `git subtree split` except that it also advances the git-tfs remote.

## See also

* [#350](https://github.com/git-tfs/git-tfs/issues/350)
* [#344](https://github.com/git-tfs/git-tfs/issues/344)
* [Git subtree command](http://git-scm.com/book/ch6-7.html)
* [Original subtree man page](https://github.com/apenwarr/git-subtree/blob/master/git-subtree.txt)
