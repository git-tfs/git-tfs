# Path too long problem

## What is the problem?
Sometimes, depending on where is located your repository folder on your drive and what is the file tree of your project,
you could face errors because you exceed the 259 characters limit of NTFS file system.

These errors could be :

`TF205022: The following path contains more than the allowed 259 characters:C:/Very/Long/Path/.git/tfs/default/workspace/Very/Long/Path/To/SubFolder/file.txt. Specify a shorter path.`

or

`Failed to stat file 'C:/Very/Long/Path/.git/tfs/default/workspace/Very/Long/Path/To/SubFolder/file.txt': The system cannot find the file specified.`

This is due to the fact that git-tfs use a temp folder located in the folder `.git/tfs/default/workspace` (`default` is use for the main branch but, if you use TFS branch feature of git-tfs, other --longer?-- folders following the branch name are used) where it create files received from TFS to create the git commits. The path of the files could became quickly very long... 

Note: If you look for this folder, you surely couldn't find it because it is created and deleted when a git-tfs command is run!

## Solutions

### Enabling Windows 10 Win32 Long Path Support

The last version of git-tfs support this windows 10 long path support feature but you have to enable it in Windows 10.

To do this you want to "Edit group policy" in the Start search bar or run "gpedit.msc" from the Run command (Windows-R).

In the Local Group Policy Editor navigate to `Local Computer Policy` -> `Computer Configuration` -> `Administrative Templates` -> `All Settings`. In this location you can find `Enable Win32 long paths`. Set it to `Enabled`.

Now, you should not have the error anymore.

See [here for more informations](https://blogs.msdn.microsoft.com/jeremykuhne/2016/07/30/net-4-6-2-and-long-paths-on-windows-10/)

### Move clone directory closer to the root drive

A first simple solution could be to move your folder from a very long path to a shorter one.
For example, move your repository folder from :
`C:\A\Very\Very\Long\Path\To\My\GitTfs\Repository` to `C:\repo`.

### Use git-tfs feature for custom workspace path

Another better solution if you faced this problem is to use a custom workspace directory!

You could set the workspace directory when cloning the tfs repository using the `--workspace` option :

```DOS
git tfs clone http://server/tfs $/Project/trunk project --workspace="c:\ws"
```

To set a custom workspace directory, you could also run the command (in a already existing repository):
`git config git-tfs.workspace-dir c:\ws`

Note:
- if you set this setting after having faced an error perhaps you should run a cleanup before fetching again ( `git tfs cleanup` )
- the `--workspace` option is also available with the `init`command

More informations : See [here](https://github.com/git-tfs/git-tfs/issues/314) or [there](https://github.com/git-tfs/git-tfs/issues/430) or [there](https://github.com/git-tfs/git-tfs/pull/266)

### Leverage TFVC workspace's Working Folders

You applied the solutions above [Enable Win32 long paths](#enabling-windows-10-win32-long-path-support) and [Short Workspace](#move-clone-directory-closer-to-the-root-drive), yet you still get a TF400889/TF205022, what else can you do ?  Leverage the multi working folder of TFVC workspaces.

Add the argument `--MultipleWorkingFoldersConfigFilePath={C:\path\to\config_file.json}`

The JSON file should follow this schema.

```JSON
[
  { 
    "SourceControlFolder" : "$/{enter the TFVC path that is failing}",
    "LocalFolder" : "{enter a local path (shorter the better) where files under the TFVC path above will end up}"
  }
]
```

This approach leverages the multiple Working Folders of a TFVC Workspace.  For more info on this, check Microsoft's article [Create and work with workspaces](https://learn.microsoft.com/en-us/azure/devops/repos/tfvc/create-work-workspaces?view=azure-devops).

#### Example scenario

Scenario:

- TFS Url: `http://tfs:8080/tfs/DefaultCollection`
- TFVC Path: `$/MyProject`
- Workspace: `W:\`
- Item too long: `$/MyProject/thisFolderIsIntentionalyLong/ReproduceThePathTooLongErrorWithTFVC/main/src/Foo/SomeClassThatIsSuperLongOrWhatever.cs`

The git command would be:

```DOS
git tfs clone --workspace=W:\ http://tfs:8080/tfs/DefaultCollection $/MyProject
```

You get the TFS Path Too long error on the `$/MyProject/thisFolderIsIntentionalyLong/ReproduceThePathTooLongErrorWithTFVC/main/src/Foo/SomeClassThatIsSuperLongOrWhatever.cs`

Apply the multiple Working Folders solution:

- Create a file `C:\mapping.json` (the filename or placement does not matter)
- Enter a TFVC Path and a short working folder.

Note that the TFVC path entered is a sub-path of the failing item.

```JSON
[
  { 
    "SourceControlFolder" : "$/MyProject/thisFolderIsIntentionalyLong/ReproduceThePathTooLongErrorWithTFVC/main",
    "LocalFolder" : "x:\\1"
  }
]
```

Rerun the git command with the argument `--MultipleWorkingFoldersConfigFilePath="c:\mapping.json"`

```DOS
git tfs clone --workspace=W:\ --MultipleWorkingFoldersConfigFilePath="c:\mapping.json" http://tfs:8080/tfs/DefaultCollection $/MyProject
```

The TFVC Workspace will contain the following Working Folders:

| Source Control Folder | Local Folder |
| --------------------- | ------------ |
| $/MyProject           | W:\          |
| $/MyProject/thisFolderIsIntentionalyLong/ReproduceThePathTooLongErrorWithTFVC/main | X:\1 |

Without this approach, the file `src/Foo/SomeClassThatIsSuperLongOrWhatever.cs` would end up under:

`W:\MyProject\thisFolderIsIntentionalyLong\ReproduceThePathTooLongErrorWithTFVC\main\src\Foo\SomeClassThatIsSuperLongOrWhatever.cs`

Triggering the Path too long exception.

With the multiple Working Folders, that file will be under:

`X:\1\src\Foo\SomeClassThatIsSuperLongOrWhatever.cs`
