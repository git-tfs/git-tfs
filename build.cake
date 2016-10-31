#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=xunit.runner.console"
#r "./build/Octokit.dll" //Use our custom version because offical one has a http request timeout of 100s preventing upload of github release asset :( https://github.com/octokit/octokit.net/issues/963

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
readonly var Target = Argument("target", "Default");
readonly var Configuration = Argument("configuration", "Release");
readonly var IsDryRun = Argument<bool>("dryRun", true);
readonly var GitHubOwner = Argument("gitHubOwner", "git-tfs");
readonly var GitHubRepository = Argument("gitHubRepository", "git-tfs");
readonly var IdGitHubReleaseToDelete = Argument<int>("idGitHubReleaseToDelete", -1);
var GitHubOAuthToken = Argument("gitHubToken", "temporary_test_of_argument_passing");
var ChocolateyToken = Argument("chocolateyToken", "");

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
const string ReleaseNotesPath = @"doc\release-notes\NEXT.md";
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
	Information("Release process:");
	Information("----------------");
	Information("1. Tag the commit with a version following the format 'v1.4'");
	Information("2. run `Cake.exe build.cake -target=Release`");
	Information("");

	Information("Available tasks:");
	StartProcess("cake.exe", "build.cake -showdescription");
});

Task("PrepareRelease").Description("TODO")
	.Does(() =>
{
	//Create Release Note
	//Create Tag
});

Task("Debug").Description("TODO")
	.Does(() =>
{
	Information("1.Target:" + Target);
	Information("1.GitHubOAuthToken:" + GitHubOAuthToken);
	Information("1.ChocolateyToken:" + ChocolateyToken);

});

Task("Debug2").Description("TODO")
	.Does(() =>
{
	Information("2.Target:" + Target);
	Information("2.GitHubOAuthToken:" + GitHubOAuthToken);
	Information("2.ChocolateyToken:" + ChocolateyToken);

});

Task("Clean").Description("Clean the working directory")
	.Does(() =>
{
 //	CleanDirectory(buildDir);
});

Task("InstallTfsModels").Description("Install the missing TFS object models to be able to build git-tfs")
	.Does(() =>
{
	if(BuildSystem.IsRunningOnAppVeyor)
	{
		if(_buildAllVersion)
		{
			ChocolateyInstall("tfs2010objectmodel");
		}
	}
	else
	{
		ChocolateyInstall("tfs2010objectmodel");
		ChocolateyInstall("tfs2012objectmodel");
		ChocolateyInstall("tfs2013objectmodel");
	}
});

Task("Restore-NuGet-Packages").Description("Restore nuget dependencies")
	.IsDependentOn("Clean")
	.Does(() =>
{
	if(FileExists("paket.exe"))
		StartProcess("paket.exe", "restore");
	else
		StartProcess(@".paket\paket.exe", "restore");
});

Task("Version").Description("Get the version using GitVersion")
	.IsDependentOn("Clean")
	.Does(() =>
{
	var version = GitVersion();
	_semanticVersionShort = version.Major + "." + version.Minor + "." + version.CommitsSinceVersionSource;
	_semanticVersionLong = _semanticVersionShort + "+" + version.Sha + "." + version.BranchName;
	Information("Semantic version (short):" + _semanticVersionShort);
	Information("Semantic version (long ):" + _semanticVersionLong);

	//Update all the variables now that we know the version number
	_zipFilename = string.Format(ZipFileTemplate, _semanticVersionShort);
	_zipFilePath = System.IO.Path.Combine(buildAssetPath, _zipFilename);
	_downloadUrl = string.Format(DownloadUrlTemplate, _semanticVersionShort) + _zipFilename;
	_releaseVersion = "v" + _semanticVersionShort;
});

Task("UpdateAssemblyInfo").Description("Update AssemblyInfo properties with the Git Version")
	.IsDependentOn("Version")
	.Does(() =>
{
	if(BuildSystem.IsRunningOnAppVeyor)
	{
		AppVeyor.UpdateBuildVersion(_semanticVersionShort);
	}
	
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
			OutputDirectory = "."
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
		CopyDirectory(@".\packages\LibGit2Sharp.NativeBinaries\libgit2\windows", libgit2NativeBinariesFolder);
	}
	CopyFiles(@".\packages\**\Microsoft.WITDataStore*.dll", OutputDirectory + @"\GitTfs.Vs2015\");
	CopyFiles(new[] {"README.md", "LICENSE", "NOTICE"}, OutputDirectory);
	DeleteFiles(OutputDirectory + @"\*.pdb");

	//Create the zip
	Zip(OutputDirectory, _zipFilePath);
	if(BuildSystem.IsRunningOnAppVeyor)
	{
		// Upload artifact to AppVeyor.
		BuildSystem.AppVeyor.UploadArtifact(_zipFilePath);
		var msiFile = @".\GitTfs.Setup\GitTfs.Setup.msi";
		if(FileExists(msiFile))
			BuildSystem.AppVeyor.UploadArtifact(msiFile);
	}
});

void DisplayAuthTokenErrorMessage()
{
	var errorMessage = @"Please create a file 'Auth.targets' containing only your authentication token generated from https://github.com/settings/tokens
See the file 'auth.targets.example'.
(Add at least the scope 'repo' or 'public_repo')";

	throw new Exception(errorMessage);
}

string ReadGithubAuthToken()
{
	var regexToken = new System.Text.RegularExpressions.Regex("^[0-9a-f]{40}$");
	var authTargetsFile = "Auth.targets";
	if(!FileExists(authTargetsFile))
		DisplayAuthTokenErrorMessage();
	var authTargetsContent = System.IO.File.ReadAllLines(authTargetsFile);
	if(authTargetsContent.Length == 0)
		DisplayAuthTokenErrorMessage();
	var personalToken = authTargetsContent[0].Trim();
	if(!regexToken.IsMatch(personalToken))
		DisplayAuthTokenErrorMessage();
	return personalToken;
}

string ReadReleaseNotes()
{
	if(!FileExists(ReleaseNotesPath))
		return string.Empty;
	return System.IO.File.ReadAllText(ReleaseNotesPath);
}

Octokit.GitHubClient GetGithubClient()
{
	var githubToken = ReadGithubAuthToken();
	var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("git-tfs-releasing"));
	var tokenAuth = new Octokit.Credentials(githubToken);
	client.Credentials = tokenAuth;
	return client;
}

Task("CreateGithubRelease").Description("Create a GithHub release")
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
	ChocolateyPack(nuspecPathInBuildDir, new ChocolateyPackSettings {
								Version			= _semanticVersionShort,
								ReleaseNotes	= releaseNotes.Split(new string[] { Environment.NewLine }, StringSplitOptions.None),
								OutputDirectory = ChocolateyBuildDir
								});
	var chocolateyPackage = "gittfs." + _semanticVersionShort + ".nupkg";
	var chocolateyPackagePath = System.IO.Path.Combine(ChocolateyBuildDir, chocolateyPackage);
	if(!IsDryRun)
	{
		ChocolateyPush(chocolateyPackagePath, new ChocolateyPushSettings {
			Source				= "http://example.com/chocolateyfeed",
			ApiKey				= "4003d786-cc37-4004-bfdf-c4f3e8ef9b3a",
			Timeout				= TimeSpan.FromSeconds(300),
			Debug				= false,
			Verbose				= false,
			Force				= false,
			Noop				= false,
			LimitOutput			= false,
			ExecutionTimeout	= 13,
			CacheLocation		= @"C:\temp",
			AllowUnofficial		= false
		});
	}
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Default").Description("")
	.IsDependentOn("Package");

Task("AppVeyorBuild").Description("Do the continuous integration build with AppVeyor")
	.IsDependentOn("Run-Smoke-Tests")
	.IsDependentOn("Package");

Task("AppVeyorRelease").Description("Do the release build with AppVeyor")
	.IsDependentOn("InstallTfsModels")
	.IsDependentOn("Run-Smoke-Tests")
	.IsDependentOn("Package");

Task("Release").Description("Build the release and put it on github.com")
	.IsDependentOn("Chocolatey");
//Release =>Package; ReleaseOnGitHub; Chocolatey

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(Target);
