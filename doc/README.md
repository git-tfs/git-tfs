[git-tfs](http://git-tfs.com/) is a two-way bridge between TFS and git, similar to git-svn.
It fetches TFS commits into a git repository, and lets you push your updates back to TFS.

The most recent version is 0.17.1.
See the [change history](change-history.md) for details.

If you're having problems, check out the [troubleshooting](TROUBLESHOOTING.md) page.
And read [how to report an issue](reporting-issues.md), before doing so ;)

## Get git-tfs

Either [download a binary](http://git-tfs.com/) ([old versions](https://github.com/git-tfs/git-tfs/downloads)), use [Chocolatey](http://chocolatey.org/packages/gittfs) or build from source:

    git clone git://github.com/git-tfs/git-tfs.git
    cd git-tfs
    msbuild GitTfs.sln
    set PATH=%PATH%;%cd%\GitTfs\bin\Debug

The last step adds git-tfs.exe to your path. If you download a package, you'll need to complete this step, too.

## Use git-tfs

    # [optional] find a repository path to clone :
    git tfs list-remote-branches http://tfs:8080/tfs/DefaultCollection

    # clone the whole repository (wait for a while...) :
    git tfs clone http://tfs:8080/tfs/DefaultCollection $/some_project

    # or, if you're impatient (only last changeset) :
    git tfs quick-clone http://tfs:8080/tfs/DefaultCollection $/some_project

    cd some_project
    git log # shows your TFS history, unless you did quick-clone
    tf history # error: no workspace ;)

    # [do work, do work, just using git], then...
    # gets latest from TFS to the branch tfs/default :
    git tfs fetch

    # commit on TFS :
    git tfs rcheckin

    # or shelve your changes :
    git tfs shelve MY_AWESOME_CHANGES


git-tfs is designed to work outside of any existing TFS workspaces.

## Available commands / options

This is the complete list of commands in the master branch on github.

### Repository setup

* [list-remote-branches](commands/list-remote-branches.md) - since 0.17
* [clone](commands/clone.md) - since 0.9
* [quick-clone](commands/quick-clone.md) - since 0.9
* [bootstrap](commands/bootstrap.md) - since [0.11][v0.11]
* [init](commands/init.md) - since 0.9

### Pull from TFS

* [clone](commands/clone.md) - since 0.9
* [fetch](commands/fetch.md) - since 0.9
* [pull](commands/pull.md) - since 0.9
* [quick-clone](commands/quick-clone.md) - since 0.9
* [unshelve](commands/unshelve.md) - since [0.11][v0.12]
* [shelve-list](commands/shelve-list.md) - since [0.12][v0.12]
* [init-branch](commands/init-branch.md) - since v0.16 (prefer the [branch](commands/branch.md) command)
* [labels](commands/labels.md) - since v0.17

### Push to TFS

* [rcheckin](commands/rcheckin.md) - since [0.12][v0.12]
* [checkin](commands/checkin.md) - since 0.10
* [checkintool](commands/checkintool.md) - since 0.10
* [shelve](commands/shelve.md) - since 0.9

### Manage TFS branches

* [list-remote-branches](commands/list-remote-branches.md) - since 0.17
* [branch](commands/branch.md) - since 0.17
* [init-branch](commands/init-branch.md) - since v0.16 (prefer the [branch](commands/branch.md) command)

### Other

* [info](commands/info.md)
* [cleanup](commands/cleanup.md) - since 0.10
* [cleanup-workspaces](commands/cleanup-workspaces.md) - since 0.10
* [diagnostics](commands/diagnostics.md) - since 0.9
* [help](commands/help.md) - since 0.9
* [verify](commands/verify.md) - since [0.11][v0.11]
* [autotag](commands/autotag.md) option - since [0.12][v0.12]

## Contributing

Information about contributing is available in
[CONTRIBUTING.md](https://github.com/git-tfs/git-tfs/blob/master/CONTRIBUTING.md).

### Migrations 
If you're migrating a TFS server from 2008 or 2005 to 2010, you might want to [Specify Alternate TFS URLs](specify-alternate-tfs-urls.md).

[v0.11]: http://mattonrails.wordpress.com/2011/03/11/git-tfs-0-11-0-release-notes/ "0.11 Release notes"
[v0.12]: http://sparethought.wordpress.com/2011/08/10/git-tfs-bridge-v0-12-released/

If you have questions or suggestions about how we could improve git-tfs you could go to [google group](http://groups.google.com/group/git-tfs-dev).

[Example](http://sparethought.wordpress.com/2011/07/18/how-to-establish-git-central-repository-for-working-against-tfs-with-git-tfs-bridge/) of setting up central git repository that tracks TFS automatically.
