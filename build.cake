//Don't define #tool here. Just add there to 'paket.dependencies' 'build' group
//Don't use #addin here. Use #r to load the dll found in the nuget package.
#r "./build/Octokit.dll" //Use our custom version because offical one has a http request timeout of 100s preventing upload of github release asset :( https://github.com/octokit/octokit.net/issues/963
#r "./packages/build/Cake.Git/lib/net46/Cake.Git.dll"
#r "System.Net.Http.dll"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
readonly var Target = Argument("target", "Default");
readonly var Configuration = Argument("configuration", "Debug");
readonly var IsDryRun = Argument<bool>("isDryRun", true);
readonly var GitHubOwner = Argument("gitHubOwner", "git-tfs");
readonly var GitHubRepository = Argument("gitHubRepository", "git-tfs");
readonly var IdGitHubReleaseToDelete = Argument<int>("idGitHubReleaseToDelete", -1);

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////
const string ApplicationName = "GitTfs";
const string ZipFileTemplate = ApplicationName + "-{0}.zip";
const string ApplicationPath = "./" + ApplicationName;
const string PathToSln = ApplicationPath + ".sln";
const string BuildDirectory = ApplicationPath + "/bin";
const string buildAssetPath = @".\tmp\";
const string DownloadUrlTemplate ="https://github.com/git-tfs/git-tfs/releases/download/v{0}/";
string ReleaseNotesPath = @"doc\release-notes\NEXT.md";
const string ChocolateyBuildDir = buildAssetPath + "choc";
readonly var OutputDirectory = BuildDirectory + "/" + Configuration;

// Define directories.
readonly var buildDir = Directory(BuildDirectory) + Directory(Configuration);
string _semanticVersionShort = ""; //0.26.179
string _semanticVersionLong  = ""; //0.26.179+4890c16f54f1b354aa198773aa9530a04d575932.master
string _zipFilePath;
string _zipFilename;
string _downloadUrl;
string _releaseVersion;
bool _buildAllVersion = (Target == "AppVeyorRelease");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Help").Description("This help...")
	.Does(() =>
{
	Information("Trigger the release process to AppVeyor:");
	Information("----------------------------------------");
	Information("1. Setup the personal data in `PersonalTokens.config` file");
	Information("2. run `.\\build.ps1 -Target \"TriggerRelease\"`");
	Information("");

	Information("Release process from local machine:");
	Information("-----------------------------------");
	Information("1. Setup the personal data in `PersonalTokens.config` file");
	Information("2. run `.\\build.ps1 -Target \"Release\" -Configuration \"Release\"`");
	Information("");

	Information("Available tasks:");
	StartProcess("cake.exe", "build.cake -showdescription");
});

Task("TagVersion").Description("Handle release note and tag the new version")
	.Does(() =>
{
	var version = GitVersion();
	var tag = version.Major + "." + (version.Minor + 1);
	var nextVersion = tag + ".0";
	Information("Next version will be:" + nextVersion);

	if(!IsDryRun)
	{
		Information("Creating release tag...");
		if(FileExists(ReleaseNotesPath))
		{
			var newReleaseNotePath = @"doc\release-notes\v" + nextVersion + ".md";
			MoveFile(ReleaseNotesPath, newReleaseNotePath);

			GitAdd(".", newReleaseNotePath);
			GitRemove(".", false, ReleaseNotesPath);
			GitCommit(".", @"Git-tfs release bot", "no-reply@git-tfs.com", "Prepare release v" + tag);
			ReleaseNotesPath = newReleaseNotePath;
			GitPush(".", GetGithubUserAccount(), GetGithubAuthToken(), "master");
		}
		GitTag(".", "v" + tag);
		GitPushRef(".", GetGithubUserAccount(), GetGithubAuthToken(), "origin", "refs/tags/v" + tag);
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

Task("InstallTfsModels").Description("Install the missing TFS object models to be able to build git-tfs")
	.Does(() =>
{
	if(BuildSystem.IsRunningOnAppVeyor)
	{
		//AppVeyor build VM already contains tfs object model >= 2012 ...
		if(_buildAllVersion)
		{
			//...so need to install "tfs2010objectmodel" only when releasing from AppVeyor build (to speed up the build otherwise.)
			Information("Installing Tfs object model 2010 to be able to release for all versions...");
			ChocolateyInstall("tfs2010objectmodel");
		}
	}
	else
	{
		//Could be call locally and manually to install all the versions needed to release a git-tfs version
		ChocolateyInstall("tfs2010objectmodel");
		ChocolateyInstall("tfs2012objectmodel");
		ChocolateyInstall("tfs2013objectmodel");
	}
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
	var shortSha1 = version.Sha.Substring(0,8);
	var postFix = (version.BranchName == "master") ? string.Empty : "-" + shortSha1 + "." + normalizedBranchName;
	_zipFilename = string.Format(ZipFileTemplate, _semanticVersionShort + postFix);
	_zipFilePath = System.IO.Path.Combine(buildAssetPath, _zipFilename);
	_downloadUrl = string.Format(DownloadUrlTemplate, _semanticVersionShort) + _zipFilename;
	_releaseVersion = "v" + _semanticVersionShort;

	if(BuildSystem.IsRunningOnAppVeyor)
	{
		var appVeyorBuildVersion = _semanticVersionShort
				+ ((version.BranchName == "master") ? string.Empty : "+" + shortSha1 + "." + normalizedBranchName);
		appVeyorBuildVersion = appVeyorBuildVersion + "." + EnvironmentVariable("APPVEYOR_BUILD_NUMBER");
		Information("Updating Appveyor version to... " + appVeyorBuildVersion);
		AppVeyor.UpdateBuildVersion(appVeyorBuildVersion);
	}
});

string NormalizeBrancheName(string branchName)
{
	return branchName.Replace('/', '_').Replace('\\', '_');
}

Task("UpdateAssemblyInfo").Description("Update AssemblyInfo properties with the Git Version")
	.IsDependentOn("Version")
	.Does(() =>
{
	CreateAssemblyInfo("CommonAssemblyInfo.cs", new AssemblyInfoSettings {
		Company="SEP",
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
	// Use MSBuild
	// /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll" /nologo /p:BuildInParallel=true /m:4
	MSBuild(PathToSln, settings => {

		settings.SetConfiguration(Configuration)
			.SetVerbosity(Verbosity.Minimal)
			.SetMaxCpuCount(4);
		if(_buildAllVersion)
		{
			settings.WithTarget("GitTfs_Vs2010")
				.WithTarget("GitTfs_Vs2012")
				.WithTarget("GitTfs_Vs2013");
		}
		settings.WithTarget("GitTfs_Vs2015")
			.WithTarget("GitTfsTest");
	});
});

void SetGitUserConfig()
{
	if(BuildSystem.IsRunningOnAppVeyor)
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

	XUnit2("./**/bin/" + Configuration + "/GitTfsTest.dll", new XUnit2Settings()
		{
			XmlReport = true,
			OutputDirectory = ".",
			UseX86 =  true
		});
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
		throw new Exception("Fail to run the somke tests");
	}
});

Task("Package").Description("Generate the release zip file")
	.IsDependentOn("Build")
	.Does(() =>
{
	CreateDirectory(ChocolateyBuildDir);

	//Prepare the zip
	var libgit2NativeBinariesFolder = OutputDirectory + @"\NativeBinaries";
	if(!DirectoryExists(libgit2NativeBinariesFolder))
	{
		CopyDirectory(@".\packages\LibGit2Sharp.NativeBinaries\runtimes\win7-x86\native", libgit2NativeBinariesFolder);
	}
	CopyFiles(@".\packages\**\Microsoft.WITDataStore*.dll", OutputDirectory + @"\GitTfs.Vs2015\");
	CopyFiles(new[] {"README.md", "LICENSE", "NOTICE"}, OutputDirectory);
	DeleteFiles(OutputDirectory + @"\**\*.pdb");

	//Create the zip
	Zip(OutputDirectory, _zipFilePath);
	if(BuildSystem.IsRunningOnAppVeyor)
	{
		Information("Upload artifact to AppVeyor...");
		BuildSystem.AppVeyor.UploadArtifact(_zipFilePath);
		var msiFile = @".\GitTfs.Setup\GitTfs.Setup.msi";
		if(FileExists(msiFile))
			BuildSystem.AppVeyor.UploadArtifact(msiFile);
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
	var authTargetsFile = "PersonalTokens.config";

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
 gitHubToken: '" + GetGithubAuthToken() + @"'
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
});

Task("CreateGithubRelease").Description("Create a GitHub release")
	.IsDependentOn("Package")
	.WithCriteria(!IsDryRun)
	.Does(() =>
{
	var client = GetGithubClient();

	var releaseNotes = ReadReleaseNotes();

	var newRelease = new Octokit.NewRelease(_releaseVersion);
	newRelease.Name = _releaseVersion;
	newRelease.Body = releaseNotes;
	newRelease.Draft = false;
	newRelease.Prerelease = false;

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

	if(!IsDryRun)
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
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Default").Description("Run the unit tests")
	.IsDependentOn("Run-Unit-Tests");

Task("AppVeyorBuild").Description("Do the continuous integration build with AppVeyor")
	.IsDependentOn("Run-Smoke-Tests")
	.IsDependentOn("Package");

Task("AppVeyorRelease").Description("Do the release build with AppVeyor")
	.IsDependentOn("TagVersion")
	.IsDependentOn("InstallTfsModels")
	.IsDependentOn("Run-Smoke-Tests")
	.IsDependentOn("Package")
	.IsDependentOn("CreateGithubRelease")
	.IsDependentOn("Chocolatey")
	;

Task("Release").Description("Build the release and put it on github.com")
	.IsDependentOn("Chocolatey");

//TODO:
//- 'Clean all' Task!
//CodeFormatter!!!!!
//Sonar
//Coverage

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(Target);
