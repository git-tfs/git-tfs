BUILDING AND RELEASING GIT-TFS
------------------------------

Normally, you should do this:

1. Make sure your HEAD is clean and that it is the same as the upstream master!

```
> git ls-remote https://github.com/git-tfs/git-tfs.git refs/heads/master
> git rev-parse HEAD
> git status
```

2. Set the auth.targets file with with your OAuth token (see auth.targets.example)

3. Build the release and include the version (e.g. X.Y.Z) and the name of a changelog file (optional).

```
> msbuild Release.proj /t:Release /p:Version=X.Y.Z /p:ReleaseNotes="ReleaseNoteFile.md"
```
