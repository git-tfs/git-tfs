//Don't define #tool here. Just add there to 'paket.dependencies' 'build' group
//Don't use #addin here. Use #r to load the dll found in the nuget package.
#r "./packages/build/Octokit/lib/net45/Octokit.dll"
#r "./packages/build/Cake.Git/lib/net461/Cake.Git.dll"
#r "System.Net.Http.dll"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
readonly var Target = Argument("target", "Default");
readonly var Configuration = Argument("configuration", "Debug");
var runInDryRun = Argument<bool>("isDryRun", true);
readonly var GitHubOwner = Argument("gitHubOwner", "git-tfs");
readonly var GitHubRepository = Argument("gitHubRepository", "git-tfs");
readonly var IdGitHubReleaseToDelete = Argument<int>("idGitHubReleaseToDelete", -1);
readonly var IsMinorRelease = Argument<bool>("isMinorRelease", false);

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////
const string ApplicationName = "GitTfs";
const string ZipFileTemplate = ApplicationName + "-{0}.zip";
const string ApplicationPath = "./" + ApplicationName;
const string PathToSln = ApplicationPath + ".sln";
const string TargetFramework = "net462"; //due to new dotnet csproj format
readonly var OutDir = "bin/" + Configuration + "/" + TargetFramework + "/";
const string buildAssetPath = @".\.build\";
const string DownloadUrlTemplate ="https://github.com/git-tfs/git-tfs/releases/download/v{0}/";
string ReleaseNotesPath = @"..\doc\release-notes\NEXT.md";
const string ChocolateyBuildDir = buildAssetPath + "choc";
readonly var OutputDirectory = ApplicationPath + "/" + OutDir;
const string TestProjectName = "GitTfsTest";

// Define directories.
readonly var buildDir = Directory(OutputDirectory);
string _semanticVersionShort = ""; //0.26.179
string _semanticVersionLong  = ""; //0.26.179+4890c16f54f1b354aa198773aa9530a04d575932.master
string _zipFilePath;
string _zipFilename;
string _downloadUrl;
string _releaseVersion;
string _sha1;
string _appVeyorBuildVersion;
bool _buildAllVersion = (Target == "AppVeyorRelease");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Help").Description("This help...")
	.Does(() =>
{
	Information(
@"Trigger the release process to AppVeyor:
----------------------------------------
1. Setup the personal data in `PersonalTokens.config` file
2. run `.\build.ps1 -Target ""TriggerRelease""`

Release process from local machine:
-----------------------------------
1. Setup the personal data in `PersonalTokens.config` file
2. run `.\build.ps1 -Target ""Release"" -Configuration ""Release""`

Example with parameters:
------------------------
run `.\build.ps1 -Target ""DryRunRelease"" -isMinorRelease=true`

Available tasks:");
	StartProcess("cake.exe", "build.cake -showdescription");
});

Task("DryRun").Description("Set the dry-run flag")
	.Does(() =>
{
	Information("Doing a dry run!!!!");
	runInDryRun = true;
});

Task("TagVersion").Description("Handle release note and tag the new version")
	.Does(() =>
{
	var version = GitVersion();
	string nextVersion;
	string tag;
	if(!IsMinorRelease)
	{
		var tagVersion = version.Major + "." + (version.Minor + 1);
		tag =  "v" + tagVersion;
		nextVersion = tagVersion + ".0";
	}
	else
	{
		nextVersion = version.Major + "." + version.Minor + "." + version.CommitsSinceVersionSource;
		tag =  "v" + nextVersion;
	}
	Information("Next version will be:" + nextVersion);

	if(!runInDryRun)
	{
		Information("Creating release tag...");
		var githubAccount = GetGithubUserAccount();
		var githubToken = GetGithubAuthToken();
		if(FileExists(ReleaseNotesPath))
		{
			var newReleaseNotePath = @"..\doc\release-notes\v" + nextVersion + ".md";
			MoveFile(ReleaseNotesPath, newReleaseNotePath);

			GitAdd("..", newReleaseNotePath);
			GitRemove("..", false, ReleaseNotesPath);
			var releaseNoteCommit = GitCommit("..", @"Git-tfs release bot", "no-reply@git-tfs.com", "Prepare release " + tag);
			Information("Release note commit created:" + releaseNoteCommit.Sha);

			ReleaseNotesPath = newReleaseNotePath;
			GitPush("..", githubAccount, githubToken, "master");
		}
		if(!IsMinorRelease)
		{
			GitTag("..", tag);
			GitPushRef("..", githubAccount, githubToken, "origin", "refs/tags/" + tag);
		}
	}
	else
	{
		if(!IsMinorRelease)
		{
			Information("[DryRun] Should create the release tag: " + tag);
		}
		else
		{
			Information("[DryRun] Minor release => Should not create a release tag");
		}
	}
});

Task("Clean").Description("Clean the working directory")
	.Does(() =>
{
	MSBuild(PathToSln, settings => {

		settings.SetConfiguration(Configuration)
			.SetVerbosity(Verbosity.Minimal)
			.WithTarget("Clean");
	});
});

Task("Restore-NuGet-Packages").Description("Restore nuget dependencies (with paket)")
	.Does(() =>
{
	if(FileExists("paket.exe"))
		StartProcess("paket.exe", "restore");
	else
		StartProcess(@".paket\paket.exe", "restore");
});

Task("Version").Description("Get the version using GitVersion")
	.Does(() =>
{
	var version = GitVersion();
	_semanticVersionShort = version.Major + "." + version.Minor + "." + version.CommitsSinceVersionSource;
	_semanticVersionLong = _semanticVersionShort + "+" + version.Sha + "." + version.BranchName;
	Information("Semantic version (short):" + _semanticVersionShort);
	Information("Semantic version (long ):" + _semanticVersionLong);

	//Update all the variables now that we know the version number
	var normalizedBranchName = NormalizeBrancheName(version.BranchName);
	_sha1 = version.Sha;
	var shortSha1 = version.Sha.Substring(0,8);
	var postFix = (version.BranchName == "master") ? string.Empty : "-" + shortSha1 + "." + normalizedBranchName;
	_zipFilename = string.Format(ZipFileTemplate, _semanticVersionShort + postFix);
	_zipFilePath = System.IO.Path.Combine(buildAssetPath, _zipFilename);
	_downloadUrl = string.Format(DownloadUrlTemplate, _semanticVersionShort) + _zipFilename;
	_releaseVersion = "v" + _semanticVersionShort;

	_appVeyorBuildVersion = _semanticVersionShort
			+ ((version.BranchName == "master") ? string.Empty : "+" + shortSha1 + "." + normalizedBranchName)
			+ "." + EnvironmentVariable("APPVEYOR_BUILD_NUMBER");
});

void UpdateAppVeyorBuildNumber()
{
	Information("Updating Appveyor version to... " + _appVeyorBuildVersion);
	AppVeyor.UpdateBuildVersion(_appVeyorBuildVersion);
}

string NormalizeBrancheName(string branchName)
{
	return branchName.Replace('/', '_').Replace('\\', '_');
}

Task("UpdateAssemblyInfo").Description("Update AssemblyInfo properties with the Git Version")
	.IsDependentOn("Version")
	.Does(() =>
{
	CreateAssemblyInfo("CommonAssemblyInfo.cs", new AssemblyInfoSettings {
		Company="GitTfs",
		Product = "GitTfs",
		Copyright = "Copyright © 2009-" + DateTime.Now.Year,
		Version = _semanticVersionShort,
		FileVersion = _semanticVersionShort,
		InformationalVersion = _semanticVersionLong
	});
});

Task("Build").Description("Build git-tfs")
	.IsDependentOn("Restore-NuGet-Packages")
	.IsDependentOn("UpdateAssemblyInfo")
	.Does(() =>
{
		MSBuild(PathToSln, settings => {
		settings.WithTarget("restore");
	});

	// Use MSBuild
	// /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll" /nologo /p:BuildInParallel=true /m:4
	MSBuild(PathToSln, settings => {

		settings.SetConfiguration(Configuration)
			.SetVerbosity(Verbosity.Minimal)
			.SetMaxCpuCount(4);
		settings.WithTarget("GitTfs_Vs2015")
			.WithTarget(TestProjectName);
	});
});

void SetGitUserConfig()
{
	if(!BuildSystem.IsLocalBuild)
	{
		Information("Setting git user config to run some integration tests...");
		//Merge with libgit2sharp now require having user name and email to be set!
		StartProcess("git.exe", "config --global user.name \"git-tfs user for merge in unit tests\"");
		StartProcess("git.exe", "config --global user.email \"git-tfs@unit-tests.com\"");
	}
}

Task("Run-Unit-Tests").Description("Run the unit tests")
	.IsDependentOn("Build")
	.Does(() =>
{
	SetGitUserConfig();

	EnsureDirectoryExists(buildAssetPath);
	var coverageFile = System.IO.Path.Combine(buildAssetPath, "coverage.xml");
	OpenCover(tool => {
		tool.XUnit2("./"+ TestProjectName + "/" + OutDir + TestProjectName +".dll", new XUnit2Settings()
		{
			XmlReport = true,
			OutputDirectory = ".",
			UseX86 =  true
		});
	},
	new FilePath(coverageFile),
	new OpenCoverSettings()
		{
			WorkingDirectory = MakeAbsolute(Directory("./"+ TestProjectName + "/" + OutDir)),
			Register = "user"
		}
		 .WithFilter("+[git-tfs*]*")
		 .WithFilter("-[LibGit2Sharp]*")
		);

	if(BuildSystem.IsRunningOnAppVeyor)
	{
		Information("Upload coverage to AppVeyor...");
		BuildSystem.AppVeyor.UploadArtifact(coverageFile);
	}
	if(BuildSystem.IsRunningOnVSTS)
	{
		Information("Upload coverage to VSTS...");
		BuildSystem.TFBuild.Commands.UploadArtifact("reports", coverageFile, "coverage.xml");
	}

	var coverageResultFolder = System.IO.Path.Combine(buildAssetPath, "coverage");
	ReportGenerator(coverageFile, coverageResultFolder, new ReportGeneratorSettings(){
    	ToolPath = @".\packages\build\ReportGenerator\tools\ReportGenerator.exe"
	});
	if(!BuildSystem.IsLocalBuild)
	{
		var coverageZip = System.IO.Path.Combine(buildAssetPath, "coverage.zip");
		Zip(coverageResultFolder, coverageZip);
		if(BuildSystem.IsRunningOnAppVeyor)
		{
			Information("Upload coverage zipped to AppVeyor...");
			BuildSystem.AppVeyor.UploadArtifact(coverageZip);
		}
		if(BuildSystem.IsRunningOnVSTS)
		{
			Information("Upload coverage zipped to VSTS...");
			BuildSystem.TFBuild.Commands.UploadArtifact("reports", coverageZip, "coverage.zip");
		}
	}
});

Task("Run-Smoke-Tests").Description("Run the functional/smoke tests")
	.IsDependentOn("Run-Unit-Tests")
	.Does(() =>
{
	var tmpDirectory = System.IO.Path.Combine(EnvironmentVariable("TMP"), "gittfs");
	EnsureDirectoryExists(tmpDirectory);
	CleanDirectory(tmpDirectory);

	var aboluteBuildDir = MakeAbsolute(Directory(buildDir));
	var absoluteSmokeTestsScript = MakeAbsolute(File(@".\build\FunctionalTesting\smoke_tests.ps1"));

	var exitCode = StartProcess("powershell.exe", new ProcessSettings
		{
			Arguments = "-file \""+ absoluteSmokeTestsScript +"\" -gittfsFolder \""+ aboluteBuildDir + "\"",
			WorkingDirectory = tmpDirectory
		});
	if(exitCode != 0)
	{
		throw new Exception("Fail to run the smoke tests");
	}
});

Task("Package").Description("Generate the release zip file")
	.IsDependentOn("Build")
	.Does(() =>
{
	CreateDirectory(ChocolateyBuildDir);

	//Prepare the zip
	var libgit2NativeBinariesFolder = OutputDirectory + @"\NativeBinaries";




	CopyDirectory(@"..\doc", OutputDirectory + @"\doc");
	CopyFiles(@".\packages\**\Microsoft.WITDataStore*.dll", OutputDirectory + @"\GitTfs.Vs2015\");
	CopyFiles(new[] {@"..\README.md", @"..\LICENSE", @"..\NOTICE"}, OutputDirectory);
	CopyFiles(new[] {@".\build\CorFlags.exe", @".\build\enable_checkin_policies_support.bat", @".\build\disable_checkin_policies_support.bat"}, OutputDirectory);
	DeleteFiles(OutputDirectory + @"\**\*.pdb");

	//Create the zip
	Zip(OutputDirectory, _zipFilePath);
	if(!BuildSystem.IsLocalBuild)
	{
		if(BuildSystem.IsRunningOnAppVeyor)
		{
			Information("Upload artifacts to AppVeyor...");
			BuildSystem.AppVeyor.UploadArtifact(_zipFilePath);
		}
		if(BuildSystem.IsRunningOnVSTS)
		{
			Information("Upload artifacts to VSTS...");
			BuildSystem.TFBuild.Commands.UploadArtifact("install", _zipFilePath, _zipFilename);
		}
	}
});

void DisplayAuthTokenErrorMessage()
{
	var errorMessage = @"Please create a file 'PersonalTokens.config' containing your authentication tokens
See the file 'PersonalTokens.config.example' for the format and content.";

	throw new Exception(errorMessage);
}

string ReadToken(string tokenKey, string tokenRegexFormat = null)
{
	var authTargetsFile = @"..\PersonalTokens.config";

	Information("Reading token..." + tokenKey);

	if(!FileExists(authTargetsFile))
		DisplayAuthTokenErrorMessage();

	var personalToken = System.IO.File.ReadAllLines(authTargetsFile).FirstOrDefault(l => l.StartsWith(tokenKey + "="));
	if(personalToken == null)
		DisplayAuthTokenErrorMessage();

	personalToken = personalToken.Trim();
	personalToken = personalToken.Substring(tokenKey.Length+1,personalToken.Length-tokenKey.Length-1);
	if(tokenRegexFormat == null)
		return personalToken;

	var regexToken = new System.Text.RegularExpressions.Regex(tokenRegexFormat);
	if(!regexToken.IsMatch(personalToken))
		DisplayAuthTokenErrorMessage();
	return personalToken;
}

string GetChocolateyToken()
{
	var token = Argument("chocolateyToken", "");
	if(!string.IsNullOrEmpty(token))
	{
		Information("Chocolatey token found in arguments!");
		return token;
	}

	token = EnvironmentVariable("chocolateyToken");
	if(!string.IsNullOrEmpty(token))
	{
		Information("Chocolatey token found in env variables!");
		return token;
	}

	return ReadToken("Chocolatey");
}

string GetGithubUserAccount()
{
	var token = Argument("gitHubUserAccount", "");
	if(!string.IsNullOrEmpty(token))
	{
		Information("GitHub user account '" + token + "' found in script arguments!");
		return token;
	}

	token = EnvironmentVariable("gitHubUserAccount");
	if(!string.IsNullOrEmpty(token))
	{
		Information("GitHub user account '" + token + "' found in env variables!");
		return token;
	}

	return ReadToken("GitHubUserAccount");
}

string GetGithubAuthToken()
{
	var token = Argument("gitHubToken", "");
	if(!string.IsNullOrEmpty(token))
	{
		Information("GitHub token found in script arguments!");
		return token;
	}

	token = EnvironmentVariable("gitHubToken");
	if(!string.IsNullOrEmpty(token))
	{
		Information("GitHub token found in env variables!");
		return token;
	}

	return ReadToken("GitHub", "^[0-9a-f]{40}$");
}

string ReadReleaseNotes()
{
	if(!FileExists(ReleaseNotesPath))
		return string.Empty;
	return System.IO.File.ReadAllText(ReleaseNotesPath);
}

Octokit.GitHubClient GetGithubClient()
{
	var githubToken = GetGithubAuthToken();
	var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("git-tfs-releasing"));
	var tokenAuth = new Octokit.Credentials(githubToken);
	client.Credentials = tokenAuth;
	return client;
}

Task("TriggerRelease").Description("Trigger a release from the AppVeyor build server")
	.Does(() =>
{
	TriggerRelease(false);
});

Task("TriggerMinorRelease").Description("Trigger a minor release from the AppVeyor build server")
	.Does(() =>
{
	TriggerRelease(true);
});

void TriggerRelease(bool isMinorRelease)
{
	Information("gitHubUserAccount: "+ GetGithubUserAccount());
	var httpClient = new System.Net.Http.HttpClient();
	//AppVeyor build data to trigger the git-tfs build + parameters passed to the release build
	var content = @"{
accountName: 'pmiossec',
projectSlug: 'git-tfs-v2qcm',
branch: 'master',
environmentVariables: {
 target: 'AppVeyorRelease',
 chocolateyToken: '"+ GetChocolateyToken() + @"',
 gitHubUserAccount: '"+ GetGithubUserAccount() + @"',
 gitHubToken: '" + GetGithubAuthToken() + @"',
 isMinorRelease: '" + isMinorRelease + @"'
 }
}";
	var appVeyorToken = ReadToken("AppVeyor");
	httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", appVeyorToken);
	httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

	var taskTriggerRelease = httpClient.PostAsync("https://ci.appveyor.com/api/builds",
		new System.Net.Http.StringContent(content, System.Text.Encoding.UTF8, "application/json"));
	taskTriggerRelease.Wait();
	var httpResponseMessage = taskTriggerRelease.Result;
	if(httpResponseMessage.IsSuccessStatusCode)
	{
		Information("Release build successfully triggered.");
	}
	else
	{
		Error("Fail to trigger the release build:" + httpResponseMessage.ReasonPhrase);
	}
}

Task("CreateGithubRelease").Description("Create a GitHub release")
	.IsDependentOn("Package")
	.WithCriteria(!runInDryRun)
	.Does(() =>
{
	var client = GetGithubClient();
	// change timeout to be able to upload the package without getting a timeout
	client.SetRequestTimeout(TimeSpan.FromMinutes(30));

	var releaseNotes = ReadReleaseNotes();

	releaseNotes += Environment.NewLine +  "![Git-Tfs " + _releaseVersion + " download count](https://img.shields.io/github/downloads/git-tfs/git-tfs/" + _releaseVersion + "/total.svg)";

	var newRelease = new Octokit.NewRelease(_releaseVersion);
	newRelease.Name = _releaseVersion;
	newRelease.Body = releaseNotes;
	newRelease.Draft = false;
	newRelease.Prerelease = false;
	newRelease.TargetCommitish = _sha1;

	var taskCreateRelease = client.Repository.Release.Create(GitHubOwner, GitHubRepository, newRelease);
	taskCreateRelease.Wait();
	var gitHubRelease = taskCreateRelease.Result;
	Information("Github Release created. Id:" + gitHubRelease.Id);
	Information("If needed, delete the Github Release with the command:");
	Information(@".\tools\Cake\Cake.exe build.cake -target=DeleteRelease -idGitHubReleaseToDelete="+ gitHubRelease.Id);
	UploadReleaseAsset(client, gitHubRelease);
});

Task("DeleteRelease").Description("Delete a (broken) GitHub release")
	.WithCriteria(IdGitHubReleaseToDelete != -1)
	.Does(() =>
{
	Information("Deleting release '" + IdGitHubReleaseToDelete +"'...");
	var client = GetGithubClient();
	var taskDeleteRelease = client.Repository.Release.Delete(GitHubOwner, GitHubRepository, IdGitHubReleaseToDelete);
	taskDeleteRelease.Wait();
});

void UploadReleaseAsset(Octokit.GitHubClient client, Octokit.Release release)
{
	Information("Uploading asset...");
	var archiveContents = System.IO.File.OpenRead(_zipFilePath);
	var assetUpload = new Octokit.ReleaseAssetUpload()
	{
		FileName = _zipFilename,
		ContentType = "application/zip",
		RawData = archiveContents
	};

	var uploadTask = client.Repository.Release.UploadAsset(release, assetUpload);
	uploadTask.Wait();
	if(uploadTask.Exception != null)
	{
		throw new Exception("Fail to upload asset!!" + uploadTask.Exception.Message);
	}
}

Task("Chocolatey").Description("Generate the chocolatey package")
	.IsDependentOn("TagVersion")
	.IsDependentOn("Package")
	.Does(() =>
{
	EnsureDirectoryExists(ChocolateyBuildDir);
	CleanDirectory(ChocolateyBuildDir);

	CopyFiles(@".\build\ChocolateyTemplates\*.*", ChocolateyBuildDir);
	var nuspecPathInBuildDir = System.IO.Path.Combine(ChocolateyBuildDir, "gittfs.nuspec");

	//Template 'chocolateyInstall.ps1'
	var installScriptPathInBuildDir = System.IO.Path.Combine(ChocolateyBuildDir, "chocolateyInstall.ps1");
	string text = TransformTextFile(installScriptPathInBuildDir, "${", "}")
		.WithToken("DownloadUrl", _downloadUrl)
		.ToString();
	System.IO.File.WriteAllText(installScriptPathInBuildDir, text);

	var releaseNotes = ReadReleaseNotes();
	if(string.IsNullOrEmpty(releaseNotes))
	{
		releaseNotes = "See https://github.com/git-tfs/git-tfs/releases/tag/v" + _semanticVersionShort;
	}
	//http://cakebuild.net/dsl/chocolatey
	Information("Creating Chocolatey package:" + nuspecPathInBuildDir);
	ChocolateyPack(nuspecPathInBuildDir, new ChocolateyPackSettings {
								Version			= _semanticVersionShort,
								ReleaseNotes	= releaseNotes.Split(new string[] { Environment.NewLine }, StringSplitOptions.None),
								OutputDirectory = ChocolateyBuildDir
								});

	var chocolateyPackage = "gittfs." + _semanticVersionShort + ".nupkg";
	var chocolateyPackagePath = System.IO.Path.Combine(ChocolateyBuildDir, chocolateyPackage);

	if(BuildSystem.IsRunningOnAppVeyor)
	{
		Information("Uploading chocolatey package as AppVeyor artifact...");
		BuildSystem.AppVeyor.UploadArtifact(chocolateyPackagePath);
	}
	if(BuildSystem.IsRunningOnVSTS)
	{
		Information("Uploading chocolatey package as VSTS artifact...");
		BuildSystem.TFBuild.Commands.UploadArtifact("install", chocolateyPackagePath, chocolateyPackage);
	}

	if(!runInDryRun)
	{
		ChocolateyPush(chocolateyPackagePath, new ChocolateyPushSettings {
			Source				= "https://chocolatey.org/",
			ApiKey				= GetChocolateyToken(),
			Timeout				= TimeSpan.FromSeconds(300),
			Debug				= false,
			Verbose				= false,
			Force				= false,
			Noop				= false,
			LimitOutput			= false,
			ExecutionTimeout	= 300
			// CacheLocation		= @"C:\temp",
			// AllowUnofficial		= false
		});
	}
	else
	{
		Information("[DryRun] Should upload chocolatey package...");
	}
});

Task("FormatCode").Description("Format c# code source to keep formatting consistent")
	.Does(() =>
{
	var codeFormatter = @"packages\build\Octokit.CodeFormatter\tools\CodeFormatter.exe";
	var args = "GitTfs.sln /rule-:FieldNames /nounicode /nocopyright";
	Information("Will run:" + codeFormatter + " " + args);
	var exitCode = StartProcess(codeFormatter, new ProcessSettings
	{
		Arguments = args
	});
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Default").Description("Run the unit tests")
	.IsDependentOn("Run-Unit-Tests");

Task("AppVeyorBuild").Description("Do the continuous integration build with AppVeyor")
	.IsDependentOn("Run-Unit-Tests")
	//.IsDependentOn("Run-Smoke-Tests") //TFS Projects on CodePlex are no more reachable
	.IsDependentOn("Package")
	.Finally(() =>
	{
		//Update the AppVeyor build number the latter possible to let the build accessible
		//through the GitHub link until the build end
		UpdateAppVeyorBuildNumber();
	});

Task("AppVeyorRelease").Description("Do the release build with AppVeyor")
	.IsDependentOn("TagVersion")
	.IsDependentOn("Run-Unit-Tests")
	//.IsDependentOn("Run-Smoke-Tests") //TFS Projects on CodePlex are no more reachable
	.IsDependentOn("Package")
	.IsDependentOn("CreateGithubRelease")
	.IsDependentOn("Chocolatey")
	.Finally(() =>
	{
		//Update the AppVeyor build number the latter possible to let the build accessible
		//through the GitHub link until the build end
		UpdateAppVeyorBuildNumber();
	});


Task("Release").Description("Build the release and put it on github.com")
	.IsDependentOn("Chocolatey");

Task("DryRunRelease").Description("Do a 'dry-run' release to verify easily most of the release tasks")
	.IsDependentOn("DryRun")
	.IsDependentOn("Release");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(Target);

//TODO:
// - Improve Release note generation
// - Sonar
// - 'Clean all' Task!
