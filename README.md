﻿## Introduction

[git-tfs](http://git-tfs.com/) is a two-way bridge between TFS (Team Foundation Server) and git, similar to git-svn.
It fetches TFS commits into a git repository, and lets you push your updates back to TFS.

The most recent version is __0.21.2__. See the [change history](https://github.com/git-tfs/git-tfs/releases) for details.

If you're having problems, check out the [troubleshooting](doc/TROUBLESHOOTING.md) page.
And read [how to report an issue](doc/reporting-issues.md), before doing so ;)

## Get git-tfs

Three different ways to get git-tfs:

* Download a binary. Find it on the [release page](https://github.com/git-tfs/git-tfs/releases),
* Using Chocolatey. If [Chocolatey](http://chocolatey.org/) is already installed on your computer, run `cinst gittfs` to install the [Chocolatey package](http://chocolatey.org/packages/gittfs)
* Build from source code. See §[Building](#building) for more informations...

__Whatever the way you get git-tfs, you should have git-tfs.exe in your path (and git, too)__.

Add the git-tfs folder path to your PATH. You could also set it temporary (the time of your current terminal session) using :

    set PATH=%PATH%;%cd%\GitTfs\bin\Debug

## Use git-tfs

You need .NET 4 and either the 2010, 2012 or 2013 version of Team Explorer installed (or Visual Studio).

### Help

    #lists the available commands
    git tfs help

    #shows a summary of the usage of a given command
    git tfs help <command>

### Cloning

    # [optional] find a tfs repository path to clone :
    git tfs list-remote-branches http://tfs:8080/tfs/DefaultCollection

    # clone the whole repository (wait for a while...) :
    git tfs clone http://tfs:8080/tfs/DefaultCollection $/some_project

    # or, if you're impatient (and want to work from the last changeset) :
    git tfs quick-clone http://tfs:8080/tfs/DefaultCollection $/some_project

    # or, if you're impatient (and want a specific changeset) :
    git tfs quick-clone http://tfs:8080/tfs/DefaultCollection $/some_project -c=145

### Working

    cd some_project
    git log # shows your TFS history, unless you did quick-clone
    tf history # error: no workspace ;)

    # [do work, do work, just using git], then...
    # gets latest from TFS to the branch tfs/default :
    git tfs fetch

### Checkin

    # report all the commits on TFS :
    git tfs rcheckin

    # or commit using the tfs checkin window
    git tfs checkintool 

    # or commit with a message
    git tfs checkin -m "Did stuff"

    # or shelve your changes :
    git tfs shelve MY_AWESOME_CHANGES

git-tfs is designed to work outside of any existing TFS workspaces.

Have a look to more detailed git-tfs use cases:

* [Working with no branches](doc/usecases/working_with_no_branches.md)
* [Manage TFS branches with git-tfs](doc/usecases/manage_tfs_branches.md)
* [Migrate your history from TFSVC to a git repository](doc/usecases/migrate_tfs_to_git.md)
* [Working with shelvesets](doc/usecases/working_with_shelvesets.md)
* [Git and Tfs (ProGit v2 Book)](http://git-scm.com/book/en/v2/Git-and-Other-Systems-Git-as-a-Client#Git-and-TFS)
* [Migrate from Tfs to Git (ProGit v2 Book)](http://git-scm.com/book/en/v2/Git-and-Other-Systems-Migrating-to-Git#TFS)

## Available commands / options

This is the complete list of commands in the master branch on github.

### Repository setup

* [list-remote-branches](doc/commands/list-remote-branches.md): *list tfs branches that can be cloned or initialized* - since [0.17](../../releases/tag/v0.17.0)
* [clone](doc/commands/clone.md): *clone a tfs path/branch and its history in a git repository* - since 0.9
* [quick-clone](doc/commands/quick-clone.md): *clone a specific changeset of a tfs path/branch in a git repository* - since 0.9
* [bootstrap](doc/commands/bootstrap.md): *bootstrap an existing git-tfs repository cloned from an existing repository* - since [0.11][v0.11]
* [init](doc/commands/init.md): *initialize a git-tfs repository (without getting changesets)* - since 0.9

### Pull from TFS

* [clone](doc/commands/clone.md): *clone a tfs path/branch and its history in a git repository* - since 0.9
* [fetch](doc/commands/fetch.md): *get changesets from tfs and update the tfs remote* - since 0.9
* [pull](doc/commands/pull.md): *get changesets from tfs, update the tfs remote and update your work* - since 0.9
* [quick-clone](doc/commands/quick-clone.md): *clone a specific changeset (without history) of a tfs path/branch in a git repository* - since 0.9
* [unshelve](doc/commands/unshelve.md): *fetch a tfs shelvesets in your repository* - since [0.11][v0.12]
* [shelve-list](doc/commands/shelve-list.md): *list tfs shelvesets* - since [0.12][v0.12]
* [labels](doc/commands/labels.md): *fetch tfs labels* - since [0.17](../../releases/tag/v0.17.0)

### Push to TFS

* [rcheckin](doc/commands/rcheckin.md): *replicate your git commits as tfs changesets* - since [0.12][v0.12]
* [checkin](doc/commands/checkin.md): *checkin your git commits as one tfs changeset* - since 0.10
* [checkintool](doc/commands/checkintool.md): *checkin in tfs using the tfs checkin dialog* - since 0.10
* [shelve](doc/commands/shelve.md): *create a shelveset from git commits* - since 0.9

### Manage TFS branches

* [list-remote-branches](doc/commands/list-remote-branches.md): *list tfs branches that can be cloned or initialized* - since [0.17](../../releases/tag/v0.17.0)
* [branch](doc/commands/branch.md): *manage (initialize, create, remove) tfs branches* - since [0.17](../../releases/tag/v0.17.0)

### Other

* [info](doc/commands/info.md): *get some informations about git-tfs and tfs*
* [cleanup](doc/commands/cleanup.md): *clean some git-tfs internal objects* - since 0.10
* [cleanup-workspaces](doc/commands/cleanup-workspaces.md): *clean tfs workspaces created by git-tfs* - since 0.10
* [help](doc/commands/help.md): *get help on git-tfs commands* - since 0.9
* [verify](doc/commands/verify.md): *verify the changesets fetched* - since [0.11][v0.11]
* [autotag](doc/config.md#per-tfs-remote) option - since [0.12][v0.12]
* [subtree](doc/commands/subtree.md): *manage sparse tfs pathes with git-tfs* - since [0.19](../../releases/tag/v0.19.0)
* [reset-remote](doc/commands/reset-remote.md): *reset a tfs remote to a previous changeset to fecth again its history* - since [0.19](../../releases/tag/v0.19.0)
* [checkout](doc/commands/checkout.md): *checkout a commit by a changeset id* - since [0.21](../../releases/tag/v0.21.0)
* diagnostics (for git-tfs developpers only) - since 0.9

* [config file](doc/config.md)

## Building

### Prerequisites 

* MSBuild (included in .NET 4) 

And depending of the version of TFS you use :

* [Visual Studio 2013 SDK](http://www.microsoft.com/en-us/download/details.aspx?id=40758)
* [Visual Studio 2012 SDK](http://www.microsoft.com/en-us/download/details.aspx?id=30668)
* [Visual Studio 2010 SDK](http://www.microsoft.com/downloads/en/details.aspx?FamilyID=21307C23-F0FF-4EF2-A0A4-DCA54DDB1E21&displaylang=en)

### Get the source code and build

    #get the source code (with submodules source code!)
    git clone --recursive git://github.com/git-tfs/git-tfs.git
    cd git-tfs

    #building with MSBuild (with the default configuration)
    msbuild GitTfs.sln

    #or building with MSBuild in debug
    msbuild GitTfs.sln /p:Configuration=debug

    #or building with MSBuild in release
    msbuild GitTfs.sln /p:Configuration=release

    #or with Rake (Ruby)
    rake build:debug

Note : if the build fails because it can't find libgit2sharp dependency, update submodules with `git submodule init` followed by `git submodule update`

## Contributing
Contributions are always welcome.

There are some simple [guidelines](CONTRIBUTING.md).

Especially, don't forget to set `core.autocrlf` to `true`. (`git config core.autocrlf true`)

## Migrations 
If you're migrating a TFS server from 2008 or 2005 to 2010, you might want to [Specify Alternate TFS URLs](doc/specify-alternate-tfs-urls.md).

[v0.11]: http://mattonrails.wordpress.com/2011/03/11/git-tfs-0-11-0-release-notes/ "0.11 Release notes"
[v0.12]: http://sparethought.wordpress.com/2011/08/10/git-tfs-bridge-v0-12-released/

If you have questions or suggestions about how we could improve git-tfs you could go to [google group](http://groups.google.com/group/git-tfs-dev).

[Example](http://sparethought.wordpress.com/2011/07/18/how-to-establish-git-central-repository-for-working-against-tfs-with-git-tfs-bridge/) of setting up central git repository that tracks TFS automatically.

## Community

Drop in and chat in [slack](https://gittfs.slack.com/). (For an invite, email `spraints at gmail.com`.)
We also have a [mailing list](https://groups.google.com/group/git-tfs-dev).

Thanks to [jetbrains](http://www.jetbrains.com/teamcity)
([teamcity](http://teamcity.codebetter.com/project.html?projectId=project256))
for providing CI!
bump
bump
bump
bump
