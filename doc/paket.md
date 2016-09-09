The git-tfs project has chosen to use [paket](https://fsprojects.github.io/Paket/) the quite good package manager.

It is somewhat simple to use even if it is quite different to use than nuget that we used to use in the .net world.

Here is a small guide to help you with the basics...

**Note**:
* Paket only replace partly nuget and could be seen as a better wrapper/manager to install and manager nuget packages. 
* You could find help on each paket command using option `--help` or on the [website](https://fsprojects.github.io/Paket/index.html) 

## The paket files

The executables are in the folder `.paket`. It should contains (at least):
* `paket.bootstrapper.exe` which automatically download the `paket.exe` file. It must be committed in the git repository.
* `paket.exe` which is the main executable that we will call if needed.  It must NOT be committed in the git repository (paket team release very often!!!).

The others files used by paket are (all must be committed):
* the `paket.dependencies` file, situated along the sln file, which contains all the nuget packages used in the project with the wished version.
* multiple `paket.references`s (one for each project), situated along each csproj, which contains just the names of the nuget packages used by this project.
* the `paket.lock` file, situated along the `paket.dependencies` file, that is the result of the dependency graph calculated by paket. It shows the version used and 
permit to understand which packages introduce a dependency, 2 very interesting information.

## Usage

### At building time

There is nothing to do. The `paket.exe` file is automatically downloaded and the packages are automatically restored, too.

### When updating a package

To update a package, the easier is surely to modify the version number in the `paket.dependencies` file.
After that, you should run `.paket\paket.exe install` that will regenerate the `paket.lock` file with the new versions.  

You could also do that calling:

     .paket\paket.exe update nuget <id_of_the_nuget_package>

**Very important note**:

For the `GitTfs.Vs2015` project, we use the tfs nuget packages now provided by Microsoft.
But they come with a lot of assemblies that are useless for git-tfs.
To reduce the git-tfs package size, we have excluded the useless references from the project but each command of paket (and from nuget before), try to reintroduce these dependencies :(

**If you just update a nuget package, you could reset the changes done by paket to the `GitTfs.Vs2015.csproj` file.**

### When adding a new package

Use the command line, if the package should be added to only one project:

      .paket\paket.exe add nuget <id_of_the_nuget_package> project <project_name>

Or in interactive mode to add to multiple projects:

      .paket\paket.exe add nuget <id_of_the_nuget_package> -i


It will take care to update all the paket files and the csproj file(s).


**Very important note**:

Same as with the update processe...

For the `GitTfs.Vs2015` project, we use the tfs nuget packages now provided by Microsoft.
But they come with a lot of assemblies that are useless for git-tfs.
To reduce the git-tfs package size, we have excluded the useless references from the project but each command of paket (and from nuget before), try to reintroduce these dependencies :(

**If you want to add a package to the `GitTfs.Vs2015` project, you should be very carefull,
reset most of the changes done by paket to the `GitTfs.Vs2015.csproj` file and stage and commit only the changes linked to the dependency you just added.**


Note: Sometimes Visual Studio is greedy and lock some assemblies. It could help to close Visual Studio before running a paket command...
