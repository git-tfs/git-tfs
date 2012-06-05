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

Use `msbuild GitTfs.sln /p:Configuration=Vs2010_Debug` to build for the 2010 version only.

You can also do `rake build:debug`.

## Contributing

If you contribute patches, please set `core.autocrlf` to `true`. (`git config core.autocrlf true`)


Contributions are always welcome. For more information about contributing,
please see [the wiki](http://github.com/git-tfs/git-tfs/wiki/Contributing).

### Community

`#git-tfs` on FreeNode, and the [mailing list](https://groups.google.com/group/git-tfs-dev)


