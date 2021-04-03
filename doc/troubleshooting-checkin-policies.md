Checkin Policies are only working for Visual Studio Versions >= 2017 and **require** the
`enable_checkin_policies_support.bat` to be run. Executing this batch file forces `git-tfs.exe`
to run as a 32bit process, which is needed to read the private Visual Studio registry hive.

If you have executed the batch file and still see an error like this

```
[ERROR] Policy: Internal error in Changeset Comments Policy.
Error loading the Changeset Comments Policy policy (The policy assembly 'Microsoft.TeamFoundation.PowerTools.CheckinPolicies.ChangesetComments, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' is not registered.).
Installation instructions: To install this policy, follow the instructions in CheckForComments.cs.
```

then the first thing to check is that you can perform normal TFS checkins (in Visual Studio) with
the checkin policy active. If you can't, first solve that problem, and then come back and check `git-tfs` again!

If you can perform checkins in Visual Studio, then the most common problem is that git-tfs can't find
the check-in policy (in this example above, `ChangesetComments`) that the Team Foundation Server
told it to run. Depending on how this happened, there are a couple of possible solutions.

## Git-tfs should be using the same version of Visual Studio that you are.

Sometimes git-tfs uses a different version of Visual Studio than you are. This most commonly happens when you are developing
with the previous version of Visual Studio (e.g. VS2017) and have already installed later version (e.g. VS2019) to try it out,
but the check-in policies are not yet registered with the new version of Visual Studio.
`git-tfs` by default prefers to use the newest version of Visual Studio.
To actually check which version of the TFS client libraries git-tfs is using, run this command:

```
C:\> git tfs info

git version 2.30.0.windows.1

git-tfs version 0.x.y.z (TFS client library 16.0.0.0 (MS)) (64-bit)

Note: If you want to force git-tfs to use another version of the tfs client library,
set the environment variable `GIT_TFS_CLIENT` with the wished version (ie: '2015' for Visual Studio 2015,...)
Supported version: 2019, 2017, 2015
```

TFS client library major vesion 15 corresponds to Visual Studio 2017 and 16 corresponds to Visual Studio 2019.

You can tell git-tfs to use a specific client library by setting the `GIT_TFS_CLIENT` environment variable to the version of Visual Studio you are using (e.g. `2017`).
If you set this in the Environment Variables control panel, then git-tfs will use the specified client library in all future command windows that you open.
Don't forget to relaunch your console after setting the Environment Variable otherwise the old ones will be used.

Be aware that starting with Visual Studio 2017, multiple versions of the same major Visual Studio version can be installed, e.g. you can
have Visual Studio 2017 Enterprise and Visual Studio 2017 Premium installed.  To see which Studio installation `git-tfs` is using,
check the output, there should be a line similarlike this:
```
Found matching Visual Studio version at C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise
```

Currently, there is no way to select a specific installation of the same major version if `git-tfs` uses your VS2017 Premimun
instead of the VS2017 Enterprise version. The only known *workaround* is to deinstall the other one.

Pull requests to implement a better selection mechanism are welcome!


## Visual Studio 2017 and 2019: checkin policies need to be registred in the private registry hive

Visual Studio no longer stores its registry keys relevant for the checkin-policies directly
in the registry, instead it stores them in a private file which can be loaded as a registry hive.

The file is stored in a folder `C:\Users\<Username>\AppData\Local\Microsoft\VisualStudio\<VSMajor_Suffix>`
where `<Username>` is your local user and `<VSMajor_Suffix>` is something like `16.0_bce69cd`, where he
major tells the concrete Visual Studio version (e.g. 16.0 corresponds to VS2019) and the suffix is some
kind of differentiater if multiple Visual Studio versions with the same major are installed.
The file itself is named `privateregistry.bin` and can be loaded and inspected with regedit.exe.

To inspect it, follow these steps:
1. Make sure you have closed all Visual Studio instances, as loading the file multiple times is not supported
2. Open `regedit.exe` and select `Computer\HKEY_LOCAL_MACHINE`
3. Load the privateregistry.bin file via `File -> Load Hive`. If `Load Hive` is grayed out, you have not
   selected the key `Computer\HKEY_LOCAL_MACHINE`. Give the loaded hive a sensible name, e.g. `VS_PrivateHive`.
4. The content of the `privateregistry.bin` file is then available as `Computer\HKEY_LOCAL_MACHINE\VS_PrivateHive`
   (or whatever name you have chosen for the private hive).
5. The entries for the checkin policies can then be found under the following path (replace variables)
   `Computer\HKEY_LOCAL_MACHINE\VS_PrivateHive\Software\Microsoft\VisualStudio\<VSMajor_Suffix>_Config\TeamFoundation\SourceControl\Checkin Policies`,  
   e.g. for a Visual Studio 2019 it is  
   `Computer\HKEY_LOCAL_MACHINE\VS_PrivateHive\Software\Microsoft\VisualStudio\16.0_bce693cd_Config\TeamFoundation\SourceControl\Checkin Policies`

   This is how this key looks like exported as a .reg file on a standard installation:

   ```
   Windows Registry Editor Version 5.00

   [HKEY_LOCAL_MACHINE\VS_PrivateHive\Software\Microsoft\VisualStudio\16.0_bce693cd_Config\TeamFoundation\SourceControl\Checkin Policies]
   "Microsoft.TeamFoundation.Build.Controls"="c:\\program files (x86)\\microsoft visul studio\\2019\\enterprise\\common7\\ide\\commonextensions\\microsoft\\teamfoundation\\team explorer\\Microsoft.TeamFoundation.Build.Controls.dll"
   "Microsoft.TeamFoundation.VersionControl.Controls"="c:\\program files (x86)\\microsoft visual studio\\2019\\enterprise\\common7\\ide\\commonextensions\\microsoft\\teamfoundation\\team explorer\\Microsoft.TeamFoundation.VersionControl.Controls.dll"
   ```

6. Make sure to unload the hive again with selecting it and then using `File -> Unload Hive`,
   otherwise Visual Studio can't be started because the private registry is already loaded.

## Visual Studio 2015: checkin policies are not supported

Currently, the checkin policies are not supported by `git-tfs`. VS2015 was the first version where
the new hive base private registry approach was used, so from a code point of view they should
be quite similar to how the checkin policy support for VS2017 and VS2019 was done.
But right now, the suporting code for VS2015 is different to the one for the later versions
Merging them can probablby be done, but the current major blocking point is someone who actually
can do the work and actually test it with Visual Studio 2015.

## OBSOLETE: Visual Studio 2013 and older: check-in policies need to registered in the registry

ATTENTION: This section describes how checkin policies worked for Visual Studio 2013 (and older versions).
Visual Studio 2013 (or older) are no longer supported by git-tfs, but the information is left intact just in case
someone uses an old `git-tfs` version which still supports Visual Studio 2013
Furthermore, it contains some links to issues which might be valuable to read up on in case checkin policies
don't work in newer versions.

There are some cases where Visual Studio can find the check-in policy implementation, but git-tfs still can't (especially VS2012 because a bug in the install of the TFS component). This can be solved by adding information about the checkin policy to the Windows registry.

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

You could also create a reg file using [this gist](https://gist.github.com/pmiossec/5678176).

See [#258](https://github.com/git-tfs/git-tfs/issues/258) for [more information](https://github.com/git-tfs/git-tfs/issues/258#issuecomment-11247086) to this problem.

(Note: this [may be caused by running git-tfs as a 32-bit executable on some 64-bit systems](https://github.com/git-tfs/git-tfs/issues/258#issuecomment-11588802).)
