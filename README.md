Introduction
------------

git-tfs is a two-way bridge between TFS and git, similar to git-svn.

Use it like this:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/some_project
    (wait for git-tfs to pull your history)

-or-

    git tfs quick-clone http://tfs:8080/tfs/DefaultCollection $/some_project

(`cd some_project` and hack away, using only git, until you need to share with TFS...)

When you need to submit your changes to TFS,

`git tfs shelve A_SHELVESET_NAME`
-or-
`git tfs checkin -m "Did stuff"`
-or-
`git tfs checkintool`


`git tfs help` for more info.


Installing
----------

Download (https://github.com/spraints/git-tfs/downloads#uploaded_downloads)
or build.

Add the directory that contains git-tfs.exe to your path. 

I've been using this with (msysgit 1.7.3.1|http://code.google.com/p/msysgit/) for a while.

You need .NET 4 and either the 2008 or 2010 version of Team Explorer installed.


Building
--------

msbuild (included in .NET 4) should be able to build the entire solution.

You can also do `rake build:debug`.

If you contribute patches, please set `core.autocrlf` to `true`. (`git config core.autocrlf true`)
