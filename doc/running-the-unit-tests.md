The unit tests for git-tfs use [xUnit.net](https://xunit.github.io/).

## CI-style

If you want to run tests like the CI build, then go to the `src` folder and run in a powershell console: `.\build.ps1`.

## Running

To run the tests, you can use the console runner.
You also may be able to run the tests inside Visual Studio in the 'Test Explorer' (or use another launcher like Reshaper, ncrunch, ...).

### xUnit.net console runner

Here is an example running the tests with the console runner:

```
C:\src\git-tfs\src>packages\build\xunit.runner.console\tools\net452\xunit.console.exe GitTfsTest\bin\Debug\GitTfsTest.dll
xUnit.net console test runner (32-bit .NET 4.0.30319.269)
Copyright (C) 2007-11 Microsoft Corporation.

xunit.dll:     Version 1.9.1.1600
Test assembly: C:\src\git-tfs\GitTfsTest\bin\Debug\GitTfsTest.dll

GitTfs.Test.Integration.CloneTests.FailOnNoProject [SKIP]
   eventually

GitTfs.Test.Integration.CloneTests.ClonesEmptyProject [SKIP]
   eventually

GitTfs.Test.Commands.HelpTest.ShouldWriteCommandHelp [SKIP]
   Not sure why this doesn't work.

GitTfs.Test.Commands.HelpTest.ShouldWriteGeneralHelp [SKIP]
   Not sure why this doesn't work.

103 total, 0 failed, 4 skipped, took 16.156 seconds
```