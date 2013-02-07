## Introduction

git-tfs is a two-way bridge between TFS and git, similar to git-svn.

## Usage

### Cloning a repository

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/some_project
    (wait for git-tfs to pull your history)

-or-

    git tfs quick-clone http://tfs:8080/tfs/DefaultCollection $/some_project

(`cd some_project` and hack away, using only git, until you need to share with TFS...)

### Pushing your changes to TFS

#### Check-in

    git tfs checkintool 

-or-

    git tfs checkin -m "Did stuff"

#### Shelveset

    git tfs shelve A_SHELVESET_NAME

### Other commands

    git tfs help


## Installing

Using [Chocolatey](http://chocolatey.org/):

``` cinst GitTfs ```

You need .NET 4 and either the 2008 or 2010 version of Team Explorer installed.

## Building

### Prerequisites 

* [Visual Studio 2010 SDK](http://www.microsoft.com/downloads/en/details.aspx?FamilyID=21307C23-F0FF-4EF2-A0A4-DCA54DDB1E21&displaylang=en)
* [Visual Studio 2008 SDK](http://www.microsoft.com/download/en/details.aspx?id=21827)
* MSBuild (included in .NET 4) 

### Building

#### Building With MSBuild
1. Update submodules. `git submodule update`  to get the libgit2sharp dependencies.
2. Build with `msbuild GitTfs.sln /p:Configuration=debug` for the default debug build.

####Building With Rake
You can also do `rake build:debug`.

## Contributing

If you contribute patches, please set `core.autocrlf` to `true`. (`git config core.autocrlf true`)


Contributions are always welcome. For more information about contributing,
please see [the wiki](http://github.com/git-tfs/git-tfs/wiki/Contributing).

### Community

`#git-tfs` on FreeNode, and the [mailing list](https://groups.google.com/group/git-tfs-dev)


[![Build Status](https://secure.travis-ci.org/git-tfs/git-tfs.png)](http://travis-ci.org/git-tfs/git-tfs)

Thanks to [travis-ci](http://travis-ci.org/) and [jetbrains](http://www.jetbrains.com/teamcity)
([teamcity](http://teamcity.codebetter.com/))
for providing CI!
