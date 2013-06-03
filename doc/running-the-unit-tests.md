The unit tests for git-tfs use [xUnit.net](http://xunit.codeplex.com). All the necessary bits of xunit should be installed by nuget.

## CI-style

If you want to run tests like the CI build, then do this: `msbuild CI.proj`.

## Building

To build the tests, you only need to build the GitTfs, GitTfs.VsFake, LibGit2Sharp, and GitTfsTest projects in VS. All of these will build if you right-click on GitTfsTest and choose 'Build'. From the command-line, you can `msbuild GitTfs.sln /t:GitTfsTest`.

## Running

To run the tests, you can use the gui runner or the console runner. If you have a VS extension with support for xUnit.net, then you may be able to run the tests inside Visual Studio.

### xUnit.net GUI runner

The gui runner is `packages\xunit.runners.1.9.1\tools\xunit.gui.clr4.x86.exe`. Load the GitTfsTest assembly (i.e. GitTfsTest\bin\Debug\GitTfsTest.dll).

### xUnit.net console runner

Here is an example running the tests with the console runner:

```
C:\src\git-tfs>packages\xunit.runners.1.9.1\tools
\xunit.console.clr4.x86.exe GitTfsTest\bin\Debug\GitTfsTest.dll
xUnit.net console test runner (32-bit .NET 4.0.30319.269)
Copyright (C) 2007-11 Microsoft Corporation.

xunit.dll:     Version 1.9.1.1600
Test assembly: C:\src\git-tfs\GitTfsTest\bin\Debug\GitTfsTest.dll

Sep.Git.Tfs.Test.Integration.CloneTests.FailOnNoProject [SKIP]
   eventually

Sep.Git.Tfs.Test.Integration.CloneTests.ClonesEmptyProject [SKIP]
   eventually

Sep.Git.Tfs.Test.Commands.HelpTest.ShouldWriteCommandHelp [SKIP]
   Not sure why this doesn't work.

Sep.Git.Tfs.Test.Commands.HelpTest.ShouldWriteGeneralHelp [SKIP]
   Not sure why this doesn't work.

103 total, 0 failed, 4 skipped, took 16.156 seconds
```