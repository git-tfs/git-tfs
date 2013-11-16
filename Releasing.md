BUILDING AND RELEASING GIT-TFS
------------------------------

Normally, you should do this:

1. Make sure your HEAD is clean and that it is the same as the upstream master!

    git ls-remote https://github.com/git-tfs/git-tfs.git refs/heads/master
    git rev-parse HEAD
    git status

2. Set the auth.targets file with with your OAuth token (see auth.targets.example)

3. Build the release and include the version (e.g. X.Y.Z) and the name of a changelog file (optional).

    msbuild Release.proj /t:Release /p:Version=X.Y.Z /p:ReleaseNotes="ReleaseNoteFile.md"

4. Build the chocolatey package. (For this to work, you need to [set an API key](https://github.com/chocolatey/chocolatey/wiki/CommandsPush#note-to-use-this-command-you-must-have-your-api-key-saved-for-chocolateyorg-or-the-source-you-want-to-push-to).)

    msbuild Release.proj /t:Chocolatey /p:Version=X.Y.Z
