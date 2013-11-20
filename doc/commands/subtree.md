## Summary

The `subtree` command permit to manage multiple TFS projects in a solution

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

When executing the following command:
```
git-tfs subtree add -p=[prefix] [tfs-url] [repo-path]
```
git-tfs will:
* Locate or create an "owner" remote for the subtree, this remote has no TfsRepositoryPath but rather delegates to other remotes which represent its subtrees.  Uses "default" unless instructed otherwise.
* Create a subtree remote with ID `[owner]_subtree/[prefix]
* Set the subtree remote's workspace to be inside the owning remote's workspace, at `workspace/[prefix]`
* Fetch the subtree remote
* execute `git subtree add --prefix=[prefix] [subtree remote] -m [commit msg]`, where the commit message includes a git-tfs-id line for the subtree master.

Afterwards, the revision history will look like this:
```
commit e75165c22d0415613129cbc5456cc7b491ec6903
Merge: 845abef 5618bb9
Author: Gordon Burgett <my.email@gmail.com>
Date:   Wed Apr 10 13:38:32 2013 -0500

     Add 'InternalTools/' from commit '5618bb9065d9df8b059e7218db1a639e38a54f22'
    
    git-tfs-id: [http://my.server.url:8080/tfs/myco];C19373
    
    git-subtree-dir: InternalTools
    git-subtree-mainline: 845abef174122eb5f5985d899a3875be965654c7
    git-subtree-split: 5618bb9065d9df8b059e7218db1a639e38a54f22

commit 5618bb9065d9df8b059e7218db1a639e38a54f22
Author: Someone Else <someone.else@company.com>
Date:   Mon Apr 8 21:20:27 2013 +0000

    Fixes #1094 - some other task
    
    git-tfs-id: [http://my.server.url:8080/tfs/myco]$/Production/InternalTools/MAIN;C19370

```

From this point on, a `git-tfs pull` against the owning remote ("default") will pull all changesets across all known subtree remotes (in this case "default_subtree/InternalTools") and will apply the changesets.
A `git-tfs checkin` will pend all changes across all subtree remotes in the same workspace.
Similarly with a shelve or unshelve.


## See also

[#350](https://github.com/git-tfs/git-tfs/issues/350)
[#344](https://github.com/git-tfs/git-tfs/issues/344)

