If you use a TFS server which version is 2008 and that you use a version of Visual Studio more recent (2010 and later), you could have the following error using git-tfs :

```
error: the version of the tfs dlls on your computer used by git-tfs doesn't match the version of the TFS Server!
You may be able to resolve this problem.
- Add an environment variable \"GIT_TFS_CLIENT\" with the correct value : 2008
- Perhaps you will have to install the corresponding sdk (See https://github.com/git-tfs/git-tfs#prerequisites )
- See https://github.com/git-tfs/git-tfs/doc/troubleshooting-GIT_TFS_CLIENT.md for more details...
```

That's due to the fact that by default, git-tfs use ( and load) the more recent _TFS client library_ it can find on your computer.
But due to the fact that some functionalities, that are used by git-tfs, are not present in your (too old!) version of TFS, this error appears.
You should tell git-tfs to load the dll corresponding to your TFS server version, e.g. 2008, that don't use these functionnalities (but with a loss of feature, of course).

At the moment, if you look at the version of the _TFS client library_ used, you should see something like that :
```
C:\> git tfs info

git version 1.8.0.msysgit.0

git-tfs version 0.18.0.0 (TFS client library 11.0.0.0 (MS)) (32-bit)
 C:\tools\gittfs\git-tfs.exe
```

TFS client library 11 is VS 2012, 10 is VS 2010, and 9 is VS 2008.

You can tell git-tfs to use a specific client library by setting the `GIT_TFS_CLIENT` environment variable to the version of TFS Server you are using (e.g. `2008`). If you set this in the Environment Variables control panel, then git-tfs will use the specified client library in all future cmd windows that you open. Don't forget to relaunch your console after setting the Environment Variable otherwise the old ones will be used.

Note that perhaps you should have to install the [TFS 2008 sdk](../git-tfs#prerequisites) if you don't have Visual Studio 2008 installed in your computer.

After setting the Environment Variable, you should see something like that :

```
C:\> git tfs info

git version 1.8.0.msysgit.0

git-tfs version 0.18.0.0 (TFS client library 9.0.0.0 (MS)) (32-bit)
 C:\tools\gittfs\git-tfs.exe
```

