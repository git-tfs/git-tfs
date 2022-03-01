## Summary

The `clone` command creates a new git repository, initialized from
a TFS source tree and fetch all the changesets

## Synopsis

	Usage: git-tfs clone [options] tfs-url-or-instance-name repository-path <git-repository-path>
	  ex : git tfs clone http://myTfsServer:8080/tfs/TfsRepository $/ProjectName/ProjectBranch

	  -h, -H, --help
	  -V, --version
	  -d, --debug                Show debug output about everything git-tfs does
	  -i, --tfs-remote, --remote, --id=VALUE
								 The remote ID of the TFS to interact with
								   default: default
		  --template=VALUE       Passed to git-init
		  --shared[=VALUE]       Passed to git-init
		  --initial-branch=VALUE Passed to git-init (requires Git >= 2.28.0)
		  --autocrlf=VALUE       Normalize line endings (default: false)
		  --ignorecase=VALUE     Ignore case in file paths (default: system default)
		  --bare                 clone the TFS repository in a bare git repository
		  --workspace=VALUE      set tfs workspace to a specific folder (a shorter path is better!)
		  --gitignore=VALUE      Path toward the .gitignore file which be
								   committed and used to ignore files
		  --ignore-regex=VALUE   a regex of files to ignore
		  --except-regex=VALUE   a regex of exceptions to ignore-regex
	  -u, --username=VALUE       TFS username
	  -p, --password=VALUE       TFS password
		  --no-parallel          Do not do parallel requests to TFS
		  --all, --fetch-all
		  --parents
		  --authors=VALUE        Path to an Authors file to map TFS users to Git users
	  -l, --with-labels, --fetch-labels
								 Fetch the labels also when fetching TFS changesets
	  -x, --export               Export metadata
		  --export-work-item-mapping=VALUE
								 Path to Work-items mapping export file
		  --branches=VALUE       Strategy to manage branches:
								 * none: Ignore branches and merge changesets,
								 fetching only the clone tfs path
								 * auto:(default) Manage merged changesets and
								 initialize the merged branches
								 * all: Manage merged changesets and initialize
								 all the branches during the clone
		  --ignore-branches-regex=VALUE
								 Don't initialize branches that match given regex
		  --ignore-not-init-branches
								 Don't initialize additional branches (only use what already was initialized)
		  --batch-size=VALUE     Size of the batch of tfs changesets fetched (-1 for all in one batch)
      -c, --changeset=VALUE      The changeset to clone from (must be a number)
      -t, --up-to=VALUE          up-to changeset # (optional, -1 for up to 
                                   maximum, must be a number, not prefixed with C)
		  --resumable            if an error occurred, try to continue when you restart clone
								 with same parameters

## Remark

Make sure that you use a local drive (and not a network share) where the clone is stored.
`git-tfs` didn't receive any explicit testing for cloning on a network share and there are known reports
like [Issue 1373](https://github.com/git-tfs/git-tfs/issues/1373) where cloning/fetching a shelveset
didn't work when the clone was done on a network share.

## Examples
### Simple
To clone all of `$/Project1` from your TFS server `tfs`
into a new directory `Project1`, do this in cmd or powershell:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1

In a git bash, run this command instead

    MSYS_NO_PATHCONV=1 git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1

Setting the environment `MSYS_NO_PATHCONV=1` prevents that the POSIX-to-Windows path conversion
will kick in, trying to convert `$/Project1` to a file system path. For further information
see the [Known Issues](https://github.com/git-for-windows/build-extra/blob/49063144d88bf3fdd35e53eceb8cb973ecb3163c/ReleaseNotes.md#known-issues)
in the release notes of Git.

Note: Equivalent to cloning with dependency branches (with option `--branches=auto`) if you are cloning the trunk branch.

### Clone from a specific changeset

To clone from a specific changeset in the history of `$/Project1` from your TFS server `tfs`
into a new directory `Project1`, do this:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1 -c=126

where `126` is the id of the changeset to clone.

This command will get all the history from this specific changeset.
It could be especially useful when you have a huge history and also when the entire history could not be clone due to not supported tfs specificities!

### Clone only the trunk (with dependency branches)

Sometimes, it could be interesting to clone only a branch of a TFS repository (for example to extract only the trunk of your project and manage branches using the [branch](branch.md) command.

Suppose you have on TFS:

    A <- B <- C <- D <- E  $/Project1/Trunk
               \
                M <- N     $/Project1/Branch

Then, do this (the clone will be done in the `MyProject1Directory` directory):

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1/Trunk MyProject1Directory

This command is equivalent to specifying explicitly the option `--branches=auto`:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1/Trunk MyProject1Directory --branches=auto

Note:

* Since v0.21, git-tfs will also initialize all the branches that have been merged in the trunk during its changeset history
and will try to manage all the merge changesets accordingly (See [Merge changesets and branches](#merge-changesets-and-branches) for more details).
* It is highly recommended to clone the root branch ( the branch that has no parents, here $/Project1/Trunk ) to be able to init the other branches after.
If you clone the branch $/Project1/Branch, you will never able to init the root branch $/Project1/Trunk after.
* Some complex branch scenario possible with TFS are actually not supported and the clone could end up with an error.
If that's the case, you will be obliged to clone without branch support and use the option `--branches=none`

### Clone only the trunk (without dependency branches or a branch)

If you want to clone the trunk without cloning dependency branches (because for example it fails at managing these branches) or you want to clone a child branch instead, you could use the option `--branches=none`.

Then, do this (the clone will be done in the `MyProject1Directory` directory):

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1/Trunk MyProject1Directory --branches=none

Note:
    * This is a way to clone that has the more chances to succeed (when you have a complex branch history with some cases not supported by git-tfs).
    * This is the one to choose when you clone another tfs path that the trunk.
    * All the merged branches are ignored and merge changesets are treated as normal changesets.

### Merge changesets and branches

Since version v0.21, when cloning the trunk from TFS, if git-tfs encounters a merge changeset, it initializes and fetches automatically the other branch merged.

Suppose you have on TFS:

    A <- B <- C <- D <- E <- X <- Y <- Z $/Project1/Trunk
               \           /
                M <- N <- O <- P <- Q    $/Project1/Branch

When cloning the tfs branch `$/Project1/Trunk`, after having fetched changesets A to E, git-tfs encountered merge changeset X. When it did, git-tfs also initialized the TFS branch `$/Project1/Branch` and fetched changesets M to O to be able to create the merge commit X and then continued to fetch changesets Y and Z.

If you don't want to initialize the merged branches automatically (or you can't because your use of TFS is not supported), you could use the option `--branches=none` to disable it.

Note: To successfully process the merge changeset (or come from a version older than TFS2010), you should have converted all the folders corresponding to a TFS branch to a branch in TFS (even the old deleted branches). To do that, open the 'Source Control Explorer', right click on a folder and choose `Branching and Merging` -> `Convert to Branch`.

### What repository path to clone?

If you don't know exactly what repository path to clone, see [list-remote-branches](list-remote-branches.md) command to get a list of the existing repositories.

### Clone all the branches (and merge changesets)

**Prerequisite**: To use this feature, all your source code folders corresponding to branches
should be converted into branches (a notion introduced by TFS2010).
To change that, you should open 'Source Control Explorer' and then, for each folder corresponding to a branch, right-click on your source folder and select `Branching and Merging` -> `Convert to Branch`.

If you want to clone your entire repository with all the branches or that the TFS branches are merged through merge changeset, perhaps you should use the option `--branches=all`:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1/Trunk --branches=all

All the TFS history (and all the branches) and the merge changesets will consequently be fetched from TFS and created in the git repository.

Note:
* Here again, some complex branch scenario possible with TFS are actually not supported and the clone could end up with an error.
If that's the case, you will be obliged to clone without branch support and use the option `--branches=none`

### Clone from a specific changeset

See [quick-clone](quick-clone.md#clone-a-specific-changeset).

### Excludes

Let's say you want to clone `$/Project`, but you don't want to
clone exes.

    git tfs clone --ignore-regex=exe$ http://tfs:8080/tfs/DefaultCollection $/Project1

You could also use the _--except-regex_ parameter to add an exception to the previous rule:

    git tfs clone --ignore-regex=exe$ --except-regex=i_want_this.exe$ http://tfs:8080/tfs/DefaultCollection $/Project1

### Exclude with a `.gitignore` file

To ignore some files, and prevent theses files to be committed, you could also provide a `.gitignore` file to git-tfs.
The `.gitignore` file will be committed as the first commit of the repository and then will be used by git-tfs to ignore all the files
matching one of the regex in the file. You need to give the path toward of an external `.gitignore` which will be used as a template.

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1 --gitignore="c:\path\toward\a\.gitignore"

You could download a `.gitignore` file for your language or project from the [github repository](https://github.com/github/gitignore)
 or generate one for multiple languages using [gitignore.io](https://www.gitignore.io/)

### Authentication

If the TFS server need an authentication, you could use the _--username_ and _--password_ parameters. If you don't specify theses informations, you will be prompted to enter them. If you use these parameters, the informations, git-tfs will store these informations (in the .git/config file --in clear--) and never prompt you again. If you don't want your password to be saved, don't use these options.

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1 -u=DISSRVTFS03\peter.pan -p=wendy


### Map TFS users to git users

With the parameter _--authors_, you could specify a file containing all the mapping of the TFS users to the git users. Each line describing a mapping following the syntax:

    DISSRVTFS03\peter.pan = Peter Pan <peter.pan@disney.com>

The clone command will be :

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1/Trunk --authors="c:\project_file\authors.txt"

Once the clone is done, the file is store in the `.git` folder (with the name `git-tfs_authors`) and used with later `fetch`. You could overwrite it by specifying another file (or go delete it).

Note: You could use the `tf history` command to help you find all the Tfs users logins that should be found in the `authors.txt` file.

    tf history $/Project1/Trunk /collection:"http://tfs:8080/tfs/TeamProjectCollectionUrl" /recursive | awk '{print $2}' | tail -n+3 | sort -u > authors.txt

Be aware that if your user logins contain spaces, you will need to use the `cut` command instead. The parameters of the `cut` command (column of beginning and column of end) depend on multiple parameters and you surely will have to find them experimentally.
The best way is perhaps to run the command 2 times and look inside the first file generated `authors_tmp.txt` where the users column began and end.

    tf history $/Project1/Trunk /collection:"http://tfs:8080/tfs/TeamProjectCollectionUrl" /recursive > authors_tmp.txt
    cat authors_tmp.txt | cut -b 11-28 | tail -n+3 | sort -u > authors.txt

### Set a custom Tfs Workspace directory

By default, git-tfs use as a Tfs workspace an internal directory and you shouldn't care about ;)
But, due to [file system limitations](../Set-custom-workspace.md), it could be useful to set a custom directory (with a path as short as possible) as a tfs workspace.
You could do it with the _--workspace_ parameter:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1 --workspace="c:\ws"

### Export metadatas

The option `--export` permit, when fetching changesets and creating commits, to add all the tfs metadatas to the commit message (work items ids, reviewers, ...).

This option could be used with other option `--export-work-item-mapping` that specify a mapping file to convert old work items ids to new work items ids.

It could be used to migrate sources away from TFSVC. See [Migrate from tfs to git](../usecases/migrate_tfs_to_git.md#migrate-toward-tfs2013-git-repository-keeping-workitems) for more details.

### Batch size of fetched changesets

The option `--batch-size` permit to specify the number of changesets fetched from tfs at the same time (default:100).
You could use this option to specify smaller batch size if git-tfs use too much memory because some changesets are huge.
This option is saved in the git config file (key `git-tfs.batch-size`). See [config file doc](../config.md).
Note: this option could also be specified during the `fetch`.

## Cloning the whole TFS Project Collection

You can clone all projects by specifying ``$/`` as the tfs-repository path. If you do not specify a git repository name, it will clone into ``tfs-collection``.

## After cloning a repository

It is recommended, especially if the TFS repository is a big one, to run, after a clone :
* a git garbage collect : `git gc`
* a [cleanup](cleanup.md) : `git tfs cleanup`

## See also

* [list-remote-branches](list-remote-branches.md)
* [init](init.md)
* [fetch](fetch.md)
* [quick-clone](quick-clone.md)

Feel free also to look at some special use cases:
* [Working with no branches](../usecases/working_with_no_branches.md)
* [Manage TFS branches with git-tfs](../usecases/manage_tfs_branches.md)
* [Migrate your history from TFSVC to a git repository](../usecases/migrate_tfs_to_git.md)
