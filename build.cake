#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=xunit.runner.console"
#tool "JetBrains.ReSharper.CommandLineTools"
#addin "nuget:?package=Cake.ReSharperReports"
#tool "nuget:?package=OctopusTools"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=ReportGenerator"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var applicationName = "Unet.WebApplication";
var applicationPath = "./src/" + applicationName;
var pathToSln = applicationPath + ".sln";
string assemblyVersion = null;
var buildDirectory = applicationPath + "/bin";
var octopusDeployApiKey = "API-NV9OXOE0SHNVNMNWRERSZ7OGAXA";


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory(buildDirectory) + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore(pathToSln);
});

Task("UpdateAssemblyInfo")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var version = GitVersion();
	var semanticVersionShort = version.Major + "." + version.Minor + "." + version.CommitsSinceVersionSource;
	var semanticVersionLong = semanticVersionShort + "+" + version.Sha + "." + version.BranchName;
	Information("Semantic version (short):" + semanticVersionShort);
	Information("Semantic version (long ):" + semanticVersionLong);
	
	//To make nuget/octopack happy!
	assemblyVersion = semanticVersionShort;
	
	var assemblyInfoPath = applicationPath + "/Properties/AssemblyInfo.cs";
	var infos = ParseAssemblyInfo(assemblyInfoPath);
	CreateAssemblyInfo(assemblyInfoPath, new AssemblyInfoSettings {
    Product = infos.Product,
	Title=infos.Title,
	Description=infos.Description,
	Configuration=infos.Configuration,
	Company=infos.Company,
	Trademark=infos.Trademark,
	//Culture=infos.Culture,
	ComVisible=infos.ComVisible,
	Guid=infos.Guid,
    Version = semanticVersionShort,
    FileVersion = semanticVersionShort,
    InformationalVersion = semanticVersionLong,
    Copyright = string.Format("Copyright © Valtech {0}", DateTime.Now.Year)
	});
});

Task("Build")
    .IsDependentOn("UpdateAssemblyInfo")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild(pathToSln, settings =>
        settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild
      XBuild(pathToSln, settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    XUnit2("./src/**/bin/" + configuration + "/*.Tests.dll", new XUnit2Settings()
		{
			XmlReport = true,
			OutputDirectory = "."
		}
		);
});

Task("Cover")
    .IsDependentOn("Build")
    .Does(() =>
{
	var coverResultFile = "./cover.xml";
	
    OpenCover(tool => {
		tool.XUnit2("./src/**/bin/" + configuration + "/*.Tests.dll", new XUnit2Settings()
			{
				XmlReport = true,
				OutputDirectory = "."
			}
		);
	},
	new FilePath(coverResultFile), new OpenCoverSettings()
	// {
	  // Register = "path64"
	// }
	//.WithFilter("+[Unet.WebApplication*]*")
	//.WithFilter("-[Unet.WebApplication.Tests]*")
	);
	
	ReportGenerator(coverResultFile, "./cover_result");
});

Task("Quality")
    .IsDependentOn("Build")
    .Does(() =>
{
	// //ReSharper code inspection
	// var resharperOutput =  "./inspectcode-output.xml";
    // InspectCode(pathToSln, new InspectCodeSettings {
		// SolutionWideAnalysis = true,
		// //Profile = "./MySolution.sln.DotSettings",
		// //MsBuildProperties = msBuildProperties;
		// OutputFile = resharperOutput,
		// //ThrowExceptionOnFindingViolations = true;
	// });
	// ReSharperReports(resharperOutput, "./resharper.html");

	// //ReSharper code duplication finder
	// var dupfinderOutput =  "./dupfinder-output.xml";
	// DupFinder(pathToSln, new DupFinderSettings {
		// ShowStats = true,
		// ShowText = true,
		// // ExcludePattern = new String[]
		// // {
			// // rootDirectoryPath + "/**/*Designer.cs",
		// // },
		// OutputFile = dupfinderOutput,
		// //ThrowExceptionOnFindingDuplicates = true
	// });
	// ReSharperReports(dupfinderOutput, "./dupfinder.html");
	
	//Visual Studio code analysis
	var codeAnalysisReport = "./" + applicationName + "_CodeAnalysis.xml";
	MSBuild(pathToSln, settings =>
		settings
			.WithProperty("RunCodeAnalysis", new string[]{ "true" })
			.WithProperty("CodeAnalysisLogFile", new string[]{ codeAnalysisReport })
	);
});

Task("PackageForOctopusDeploy")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
      MSBuild(pathToSln, settings =>
        settings.SetConfiguration(configuration)
		  .WithProperty("RunOctoPack", new string[]{ "true" })
		  .WithProperty("OctoPackPackageVersion", new string[]{ assemblyVersion })
		  );
});

Task("PushPackage")
    .IsDependentOn("PackageForOctopusDeploy")
    .Does(() =>
{
		var packageNuget = buildDirectory + "/" + applicationName + "." + assemblyVersion + ".nupkg";
		Console.WriteLine("nuget package: " + packageNuget);
		
		NuGetPush(packageNuget, new NuGetPushSettings
			{
				Source = "http://localhost/nuget/packages",
				ApiKey = octopusDeployApiKey
			}
		);
});

Task("CreateRelease")
    .IsDependentOn("PushPackage")
    .Does(() =>
{
	// OctoCreateRelease(projectNameOnServer, new CreateReleaseSettings {
		// Server = "http://octopus-deploy.example",
		// ApiKey = octopusDeployApiKey,
		// //ToolPath = "./tools/OctopusTools/Octo.exe"
		// EnableDebugLogging = true,
		// IgnoreSslErrors = true,
		// EnableServiceMessages = true, // Enables teamcity services messages when logging
		// ReleaseNumber = "1.8.2",
		// DefaultPackageVersion = "1.0.0.0", // All packages in the release should be 1.0.0.0
		// Packages = new Dictionary<string, string>
					// {
						// { "PackageOne", "1.0.2.3" },
						// { "PackageTwo", "5.2.3" }
					// },
		// PackagesFolder = @"C:\MyOtherNugetFeed",
				
		// // One or the other
		// ReleaseNotes = "Version 2.0 \n What a milestone we have ...",
		// ReleaseNotesFile = "./ReleaseNotes.md",
				
		// IgnoreExisting = true // if this release number already exists, ignore it
	// });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
