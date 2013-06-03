
## Building on Mono

Some manual changes (see below) are required in order to build git-tfs on Mono.

Once you have made the changes, you can use `xbuild` to build and the `mono-git-tfs` shell script to run git-tfs.

```
$ xbuild
...
$ ./mono-git-tfs help
+ GIT_TFS_CLIENT=Fake
+ mono ./GitTfs/bin/Debug/git-tfs.exe --version
git-tfs version 0.12.1.0 (37b71b4d) (TFS client library (FAKE)) (32-bit)
```

### Manual changes required to build on Mono.

Create a script called GetCurrentVersion and make it executable.

```
#!/bin/sh
git show --stat HEAD
```

Use unix's `cp` instead of `xcopy` to copy the VsFake plugin into the runtime directory.

```
diff --git a/GitTfs.VsFake/GitTfs.VsFake.csproj b/GitTfs.VsFake/GitTfs.VsFake.csproj
index 86f48df..9f7d4b6 100644
--- a/GitTfs.VsFake/GitTfs.VsFake.csproj
+++ b/GitTfs.VsFake/GitTfs.VsFake.csproj
@@ -115,6 +115,6 @@
   </Target>
   -->
   <PropertyGroup>
-    <PostBuildEvent>xcopy /y "$(TargetDir)*.dll" "$(SolutionDir)GitTfs\$(OutDir)"</PostBuildEvent>
+    <PostBuildEvent>cp -v $(TargetDir)*.dll $(SolutionDir)GitTfs\$(OutDir)</PostBuildEvent>
   </PropertyGroup>
 </Project>
```

Disable building of the normal TFS plugins, and other projects that depend on them.

```diff
diff --git a/GitTfs.sln b/GitTfs.sln
index c7ead94..3a1f885 100644
--- a/GitTfs.sln
+++ b/GitTfs.sln
@@ -40,8 +40,6 @@ Global
                {55C169E0-93CC-488C-9885-1D4EAF4EA236}.Release|x86.Build.0 = Release|x86
                {55C169E0-93CC-488C-9885-1D4EAF4EA236}.Vs2010_Debug|x86.ActiveCfg = Debug|x86
                {55C169E0-93CC-488C-9885-1D4EAF4EA236}.Vs2010_Debug|x86.Build.0 = Debug|x86
-               {DDFB4746-2BCE-4B34-8E45-056324CF140D}.Debug|x86.ActiveCfg = Debug|x86
-               {DDFB4746-2BCE-4B34-8E45-056324CF140D}.Debug|x86.Build.0 = Debug|x86
                {DDFB4746-2BCE-4B34-8E45-056324CF140D}.Release|x86.ActiveCfg = Release|x86
                {DDFB4746-2BCE-4B34-8E45-056324CF140D}.Release|x86.Build.0 = Release|x86
                {DDFB4746-2BCE-4B34-8E45-056324CF140D}.Vs2010_Debug|x86.ActiveCfg = Debug|x86
@@ -52,19 +50,13 @@ Global
                {7C7FEA7A-24A1-4834-9815-1DF980C340F3}.Release|x86.Build.0 = Release|x86
                {7C7FEA7A-24A1-4834-9815-1DF980C340F3}.Vs2010_Debug|x86.ActiveCfg = Debug|x86
                {7C7FEA7A-24A1-4834-9815-1DF980C340F3}.Vs2010_Debug|x86.Build.0 = Debug|x86
-               {09BF8124-19A8-45BE-896B-536CA0F3F0FC}.Debug|x86.ActiveCfg = Debug|x86
-               {09BF8124-19A8-45BE-896B-536CA0F3F0FC}.Debug|x86.Build.0 = Debug|x86
                {09BF8124-19A8-45BE-896B-536CA0F3F0FC}.Release|x86.ActiveCfg = Release|x86
                {09BF8124-19A8-45BE-896B-536CA0F3F0FC}.Release|x86.Build.0 = Release|x86
                {09BF8124-19A8-45BE-896B-536CA0F3F0FC}.Vs2010_Debug|x86.ActiveCfg = Debug|x86
-               {C5A374D3-A2E1-407C-9D6D-541FDB53BD62}.Debug|x86.ActiveCfg = Debug|x86
-               {C5A374D3-A2E1-407C-9D6D-541FDB53BD62}.Debug|x86.Build.0 = Debug|x86
                {C5A374D3-A2E1-407C-9D6D-541FDB53BD62}.Release|x86.ActiveCfg = Release|x86
                {C5A374D3-A2E1-407C-9D6D-541FDB53BD62}.Release|x86.Build.0 = Release|x86
                {C5A374D3-A2E1-407C-9D6D-541FDB53BD62}.Vs2010_Debug|x86.ActiveCfg = Debug|x86
                {C5A374D3-A2E1-407C-9D6D-541FDB53BD62}.Vs2010_Debug|x86.Build.0 = Debug|x86
-               {B2BF1C1C-BB58-4DEF-BFEF-DE0D2FE2E8F7}.Debug|x86.ActiveCfg = Debug|x86
-               {B2BF1C1C-BB58-4DEF-BFEF-DE0D2FE2E8F7}.Debug|x86.Build.0 = Debug|x86
                {B2BF1C1C-BB58-4DEF-BFEF-DE0D2FE2E8F7}.Release|x86.ActiveCfg = Release|x86
                {B2BF1C1C-BB58-4DEF-BFEF-DE0D2FE2E8F7}.Release|x86.Build.0 = Release|x86
                {B2BF1C1C-BB58-4DEF-BFEF-DE0D2FE2E8F7}.Vs2010_Debug|x86.ActiveCfg = Debug|x86
```
