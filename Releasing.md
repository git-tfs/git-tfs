BUILDING AND RELEASING GIT-TFS
------------------------------

Normally, you should do this:

1. Make sure your HEAD is clean and that it is the same as the upstream master!

    git ls-remote https://github.com/git-tfs/git-tfs.git refs/heads/master
    git rev-parse HEAD
    git status

2. Build the release and include the version (e.g. X.Y.Z).

    msbuild Release.proj /t:Release /p:Version=X.Y.Z

3. Upload the release somewhere.

4. Update [the download button](https://github.com/git-tfs/git-tfs.github.com/edit/master/_includes/download_button.html) on the website.

5. Update [the README.md](https://github.com/git-tfs/git-tfs/edit/master/README.md).

5. Build the chocolatey package. (For this to work, you need to [set an API key](https://github.com/chocolatey/chocolatey/wiki/CommandsPush#note-to-use-this-command-you-must-have-your-api-key-saved-for-chocolateyorg-or-the-source-you-want-to-push-to).)

    msbuild Release.proj /t:Chocolatey /v:Version=X.Y.Z /p:DownloadUrl=https://whatever/path/to/GitTfs-X.Y.Z.zip
