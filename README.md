## Introduction

[git-tfs](http://git-tfs.com/) is a two-way bridge between TFS (Team Foundation Server) and git, similar to git-svn.
It fetches TFS commits into a git repository, and lets you push your updates back to TFS.

The most recent version is __0.19.2__. See the [change history](https://github.com/git-tfs/git-tfs/releases) for details.

If you're having problems, check out the [troubleshooting](doc/TROUBLESHOOTING.md) page.
And read [how to report an issue](doc/reporting-issues.md), before doing so ;)

## Get git-tfs

Three differents ways to get git-tfs:

* Download a binary. Find it on the [release page](https://github.com/git-tfs/git-tfs/releases),
* Using Chocolatey. If [Chocolatey](http://chocolatey.org/) is already installed on your computer, run `cinst gittfs` to install the [Chocolatey package](http://chocolatey.org/packages/gittfs)
* Build from source code. See §[Building](#building) for more informations...

__Whatever the way you get git-tfs, you should have git-tfs.exe in your path (and git, too)__.

Add the git-tfs folder path to your PATH. You could also set it temporary (the time of your current terminal session) using :

    set PATH=%PATH%;%cd%\GitTfs\bin\Debug

## Use git-tfs

You need .NET 4 and either the 2008, 2010 or 2012 version of Team Explorer installed.

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

You could find more interesting [use cases](doc/usecases/usecases.md) on how to use git-tfs.
	
git-tfs is designed to work outside of any existing TFS workspaces.

## Available commands / options

This is the complete list of commands in the master branch on github.

### Repository setup

* [list-remote-branches](doc/commands/list-remote-branches.md) - since 0.17
* [clone](doc/commands/clone.md) - since 0.9
* [quick-clone](doc/commands/quick-clone.md) - since 0.9
* [bootstrap](doc/commands/bootstrap.md) - since [0.11][v0.11]
* [init](doc/commands/init.md) - since 0.9

### Pull from TFS

* [clone](doc/commands/clone.md) - since 0.9
* [fetch](doc/commands/fetch.md) - since 0.9
* [pull](doc/commands/pull.md) - since 0.9
* [quick-clone](doc/commands/quick-clone.md) - since 0.9
* [unshelve](doc/commands/unshelve.md) - since [0.11][v0.12]
* [shelve-list](doc/commands/shelve-list.md) - since [0.12][v0.12]
* [init-branch](doc/commands/init-branch.md) - since v0.16 (prefer the [branch](doc/commands/branch.md) command)
* [labels](doc/commands/labels.md) - since v0.17

### Push to TFS

* [rcheckin](doc/commands/rcheckin.md) - since [0.12][v0.12]
* [checkin](doc/commands/checkin.md) - since 0.10
* [checkintool](doc/commands/checkintool.md) - since 0.10
* [shelve](doc/commands/shelve.md) - since 0.9

### Manage TFS branches

* [list-remote-branches](doc/commands/list-remote-branches.md) - since 0.17
* [branch](doc/commands/branch.md) - since 0.17
* [init-branch](doc/commands/init-branch.md) - since v0.16 (prefer the [branch](doc/commands/branch.md) command)

### Other

* [info](doc/commands/info.md)
* [cleanup](doc/commands/cleanup.md) - since 0.10
* [cleanup-workspaces](doc/commands/cleanup-workspaces.md) - since 0.10
* [diagnostics](doc/commands/diagnostics.md) - since 0.9
* [help](doc/commands/help.md) - since 0.9
* [verify](doc/commands/verify.md) - since [0.11][v0.11]
* [autotag](doc/commands/autotag.md) option - since [0.12][v0.12]

## Building

### Prerequisites 

* MSBuild (included in .NET 4) 

And depending of the version of TFS you use :

* [Visual Studio 2012 SDK](http://www.microsoft.com/en-us/download/details.aspx?id=30668)
* [Visual Studio 2010 SDK](http://www.microsoft.com/downloads/en/details.aspx?FamilyID=21307C23-F0FF-4EF2-A0A4-DCA54DDB1E21&displaylang=en)
* [Visual Studio 2008 SDK](http://www.microsoft.com/download/en/details.aspx?id=21827)

### Get the source code and build

    #get the source code
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

Note : if the build fails because it can't find libgit2sharp dependency, update submodules with `git submodule update`

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

`#git-tfs` on FreeNode, and the [mailing list](https://groups.google.com/group/git-tfs-dev)


[![Build Status](https://secure.travis-ci.org/git-tfs/git-tfs.png)](http://travis-ci.org/git-tfs/git-tfs)

Thanks to [travis-ci](http://travis-ci.org/) and [jetbrains](http://www.jetbrains.com/teamcity)
([teamcity](http://teamcity.codebetter.com/project.html?projectId=project256))
for providing CI!
