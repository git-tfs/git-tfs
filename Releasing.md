BUILDING AND RELEASING GIT-TFS
------------------------------

Normally, you should do this:

1. Make sure your HEAD is clean and that it is the same as the upstream master!

    git ls-remote https://github.com/git-tfs/git-tfs.git refs/heads/master
    git rev-parse HEAD
    git status

2. Build the release and include the version (e.g. X.Y.Z) and the login and password for the user having the rights on git-tfs repository.

    msbuild Release.proj /t:Release /p:Version=X.Y.Z /p:User="login:password"

3. Update [the README.md](https://github.com/git-tfs/git-tfs/edit/master/README.md).

4. Build the chocolatey package. (For this to work, you need to [set an API key](https://github.com/chocolatey/chocolatey/wiki/CommandsPush#note-to-use-this-command-you-must-have-your-api-key-saved-for-chocolateyorg-or-the-source-you-want-to-push-to).)

    msbuild Release.proj /t:Chocolatey /p:Version=X.Y.Z /p:DownloadUrl=https://whatever/path/to/GitTfs-X.Y.Z.zip
