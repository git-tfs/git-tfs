If you see an error like this:

```
[ERROR] Policy: Internal error in Changeset Comments Policy.
Error loading the Changeset Comments Policy policy (The policy assembly 'Microsoft.TeamFoundation.PowerTools.CheckinPolicies.ChangesetComments, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' is not registered.).
Installation instructions: To install this policy, follow the instructions in CheckForComments.cs.
```

The first thing to check is that you can perform normal TFS checkins (in Visual Studio). If you can't, first solve that problem, and then check git-tfs again.

If you can perform checkins in Visual Studio, then the problem is that git-tfs can't find the checkin policy (in this example above, `ChangesetComments`) that the Team Foundation Server told it to run. Depending on how this happened, there are a couple of possible solutions.

## Git-tfs should be using the same version of Visual Studio that you are.

Sometimes git-tfs uses a different version of Visual Studio than you are. This most commonly happens when you are developing with the previous version of Visual Studio (e.g. VS2010), and install the latest version (e.g. VS2012) to try it out, but the checkin policies are not yet registered with the new version of Visual Studio. Git-tfs by default prefers the newest version of Visual Studio. To see which version of the TFS client libraries git-tfs is using, do this:

```
C:\> git tfs info

git version 1.8.0.msysgit.0

git-tfs version 0.16.1.0 (TFS client library 11.0.0.0 (MS)) (32-bit)
 C:\tools\gittfs\git-tfs.exe
```

TFS client library 11 is VS 2012, 10 is VS 2010, and 9 is VS 2008.

You can tell git-tfs to use a specific client library by setting the `GIT_TFS_CLIENT` environment variable to the version of Visual Studio you are using (e.g. `2010`). If you set this in the Environment Variables control panel, then git-tfs will use the specified client library in all future cmd windows that you open. Don't forget to relaunch your console after setting the Environment Variable otherwise the old ones will be used.

## The checkin policies need to be registered.

There are some cases where Visual Studio can find the checkin policy implementation, but git-tfs still can't (especially VS2012 because a bug in the install of the TFS component). This can be solved by adding information about the checkin policy to the Windows registry.

Depending of the version of Visual Studio (here VS2012), look for the registry key :

    - for a 32bits system :
    [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\11.0\TeamFoundation\SourceControl\Checkin Policies]

    - for a 64bits system :
    [HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\11.0\TeamFoundation\SourceControl\Checkin Policies]
 
If some of the values are not set, the paths to the corresponding files should be specified. You should  find and specify the paths to the assemblies. Here are the paths for a standard install of VS2012:

    - for "Microsoft.TeamFoundation.Build.Controls" :
    C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\PrivateAssemblies\Microsoft.TeamFoundation.Build.Controls.dll

    - for "Microsoft.TeamFoundation.VersionControl.Controls" :
    C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\PrivateAssemblies\Microsoft.TeamFoundation.VersionControl.Controls.dll

You could also create a reg file using [[this gist|https://gist.github.com/pmiossec/5678176]].

See #258 for a [[more informations|https://github.com/git-tfs/git-tfs/issues/258#issuecomment-11247086]] to this problem.

(Note: this [[may be caused by running git-tfs as a 32-bit executable on some 64-bit systems|https://github.com/git-tfs/git-tfs/issues/258#issuecomment-11588802]].)