[[git-tfs|http://git-tfs.com/]] is a two-way bridge between TFS and git, similar to git-svn.
It fetches TFS commits into a git repository, and lets you push your updates back to TFS.

The most recent version is 0.17.1.
See [[change history]] for details.

If you're having problems, check out the [[troubleshooting]] page.
And read [[how to report an issue|reporting issues]], before doing so ;)

## Get git-tfs

Either [[download a binary|http://git-tfs.com/]] ([[old versions|https://github.com/git-tfs/git-tfs/downloads]]), use [[Chocolatey|http://chocolatey.org/packages/gittfs]] or build from source:

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

* [[list-remote-branches]] - since 0.17
* [[clone]] - since 0.9
* [[quick-clone]] - since 0.9
* [[bootstrap]] - since [0.11][v0.11]
* [[init]] - since 0.9

### Pull from TFS

* [[clone]] - since 0.9
* [[fetch]] - since 0.9
* [[pull]] - since 0.9
* [[quick-clone]] - since 0.9
* [[unshelve]] - since [v0.12]
* [[shelve-list]] - since [v0.12]
* [[init-branch]] - since v0.16 ( prefer the [[branch]] command )
* [[labels]] - since v0.17

### Push to TFS

* [[rcheckin]] - since [v0.12]
* [[checkin]] - since 0.10
* [[checkintool]] - since 0.10
* [[shelve]] - since 0.9

### Manage TFS branches

* [[list-remote-branches]] - since 0.17
* [[branch]] - since 0.17
* [[init-branch]] - since v0.16 ( prefer the [[branch]] command )

### Other

* [[info]]
* [[cleanup]] - since 0.10
* [[cleanup-workspaces]] - since 0.10
* [[diagnostics]] - since 0.9
* [[help]] - since 0.9
* [[verify]] - since [0.11][v0.11]
* [[autotag]] option - since [v0.12]

## Contributing

If you contribute patches, please set `core.autocrlf` to `true`. (`git config core.autocrlf true`)

Contributions are always welcome. For more information about contributing,
please see [Contributing page](http://github.com/git-tfs/git-tfs/wiki/Contributing).

## More information

Check out the [[README|https://github.com/git-tfs/git-tfs#readme]].

### Migrations 
If you're migrating a TFS server from 2008 or 2005 to 2010, you might want to [[Specify Alternate TFS URLs]].

[v0.11]: http://mattonrails.wordpress.com/2011/03/11/git-tfs-0-11-0-release-notes/ "0.11 Release notes"
[v0.12]: http://sparethought.wordpress.com/2011/08/10/git-tfs-bridge-v0-12-released/

If you have questions or suggestions about how we could improve git-tfs you could go to [[google group|http://groups.google.com/group/git-tfs-dev]].

[[Example|http://sparethought.wordpress.com/2011/07/18/how-to-establish-git-central-repository-for-working-against-tfs-with-git-tfs-bridge/]] of setting up central git repository that tracks TFS automatically.