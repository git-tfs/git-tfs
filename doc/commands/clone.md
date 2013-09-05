## Summary

The `clone` command creates a new git repository, initialized from
a TFS source tree and fetch all the changesets

## Synopsis

    Usage: git-tfs clone [options] tfs-url-or-instance-name repository-path <git-repository-path>
      -h, -H, --help
      -V, --version
      -d, --debug                Show debug output about everything git-tfs does
      -i, --tfs-remote, --remote, --id=VALUE
                                 The remote ID of the TFS to interact with
                                   default: default
          --template=VALUE       Passed to git-init
          --shared[=VALUE]       Passed to git-init
          --ignore-regex=VALUE   a regex of files to ignore
          --no-metadata          leave out the 'git-tfs-id:' tag in commit
                                   messages
                                   Use this when you're exporting from TFS and
                                   don't need to put data back into TFS.
      -u, --username=VALUE       TFS username
      -p, --password=VALUE       TFS password
          --all, --fetch-all
          --parents
          --authors=VALUE        Path to an Authors file to map TFS users to Git users
## Examples

### Simple

To clone all of `$/Project1` from your TFS 2010 server `tfs`
into a new directory `Project1`, do this:

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1

### Clone only the trunk (or a branch)
Sometimes, it could be interesting to clone only a branch of a TFS repository (for exemple to extract only the trunk of your project and manage branches with `[branch](branch.md)`. 

Suppose you have on TFS:

    A <- B <- C <- D <- E  $/Project1/Trunk
               \                              
                M <- N     $/Project1/Branch

Then, do this (the clone will be done in the `MyProject1Directory` directory):

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1/Trunk MyProject1Directory

Note : It is highly recommanded to clone the root branch ( the branch that has no parents, here $/Project1/Trunk ) to be able to init the other branches after.
If you clone the branch $/Project1/Branch, you will never able to init the root branch $/Project1/Trunk after.

### What repository path to clone?

If you don't know exactly what repository path to clone, see [list-remote-branches](list-remote-branches.md) command to get a list of the existing repositories.

### Excludes

Let's say you want to clone `$/Project`, but you don't want to
clone exes.

    git tfs clone --ignore-regex=exe$ http://tfs:8080/tfs/DefaultCollection $/Project1

### Authentication

If the TFS server need an authentication, you could use the _--username_ and _--password_ parameters. If you don't specify theses informations, you will be prompted to enter them. If you use these parameters, the informations, git-tfs will store these informations (in the .git/config file --in clear--) and never prompt you again. If you don't want your password to be saved, don't use these options.

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1 -u=DISSRVTFS03\peter.pan -p=wendy


### Map TFS users to git users

With the parameter _--authors_, you could specify a file containing all the mapping of the TFS users to the git users. Each line describing a mapping following the syntax:

    DISSRVTFS03\peter.pan = Peter Pan <peter.pan@disney.com>

The clone command will be :

    git tfs clone http://tfs:8080/tfs/DefaultCollection $/Project1 --authors="c:\project_file\authors.txt"

Once the clone is done, the file is store in the `.git` folder (with the name git-tfs_authors) and used with later `fetch`. You could overwrite it by specifting another file (or go delete it).


## After cloning a repository

It is recommended, especially if the TFS repository is a big one, to run, after a clone :
* a git garbage collect : `git gc`
* a [cleanup](cleanup.md) : `git tfs cleanup`

## See also

* [list-remote-branches](list-remote-branches.md)
* [init](init.md)
* [fetch](fetch.md)
* [quick-clone](quick-clone.md)
