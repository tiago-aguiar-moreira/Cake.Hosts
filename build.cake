//TODO Checkout https://github.com/cake-contrib/Cake.Recipe/blob/develop/build.cake
//#addin nuget:?package=Cake.Git&version=0.13.0

#load "./build/credentials.cake"
#load "./build/parameters.cake"
#load "./build/gitversion.cake"
#load "./build/environment.cake"
#load "./build/addins.cake"
#load "./build/paths.cake"
#load "./build/toolsettings.cake"
#load "./build/nuget.cake"
#load "./build/appveyor.cake"



Environment.SetVariableNames();


var rootDirectoryPath         = MakeAbsolute(Context.Environment.WorkingDirectory);
var sourceDirectoryPath       = "./src";
var title                     = "Cake.Hosts";
var solutionFilePath          = "./src/Cake.Hosts.sln";


BuildParameters.SetParameters(Context, 
                            BuildSystem, 
                            sourceDirectoryPath, 
                            title, 
                            solutionFilePath: solutionFilePath,
                            shouldPostToGitter: false,
                            shouldPostToSlack: false,
                            shouldPostToTwitter: false,
                            shouldPublishChocolatey: false,
                            shouldPublishNuGet: false,
                            shouldPublishGitHub: false,
                            shouldGenerateDocumentation: false);

BuildParameters.PrintParameters(Context);
ToolSettings.SetToolSettings(Context);

var publishingError = false;


Setup(context =>
{
    if(BuildParameters.IsMasterBranch && (context.Log.Verbosity != Verbosity.Diagnostic)) {
        Information("Increasing verbosity to diagnostic.");
        context.Log.Verbosity = Verbosity.Diagnostic;
    }

    BuildParameters.SetBuildPaths(BuildPaths.GetPaths(Context));


    var semanticVersion = BuildVersion.CalculatingSemanticVersion(Context);
    BuildParameters.SetBuildVersion(semanticVersion);
    
    
    Information("Building version {0} of " + title + " ({1}, {2}) using version {3} of Cake. (IsTagged: {4})",
        BuildParameters.Version.SemVersion,
        BuildParameters.Configuration,
        BuildParameters.Target,
        BuildParameters.Version.CakeVersion,
        BuildParameters.IsTagged);
});


Task("Debug")
    .IsDependentOn("Show-Info");

Task("Show-Info")
    .Does(() => 
    {
        Information("Target: {0}", BuildParameters.Target);
        Information("Configuration: {0}", BuildParameters.Configuration);

        Information("Source DirectoryPath: {0}", MakeAbsolute(BuildParameters.SourceDirectoryPath));
        Information("Build DirectoryPath: {0}", MakeAbsolute(BuildParameters.Paths.Directories.Build));
    });

Task("Clean")
    .Does(() =>
    {
        Information("Cleaning...");

        CleanDirectories(BuildParameters.Paths.Directories.ToClean);
        // CleanDirectories("./src/**/bin/**");
        // CleanDirectories("./src/**/obj/**");
    });



Task("Restore")
    .Does(() =>
    {
        Information("Restoring {0}...", BuildParameters.SolutionFilePath);

        var settings = new DotNetCoreRestoreSettings
        {
            Sources = new[] { "https://www.nuget.org/api/v2" },
            PackagesDirectory = "./packages",
            DisableParallel = false,
            WorkingDirectory = sourceDirectoryPath,
        };

        DotNetCoreRestore(settings);
    });

Task("Build")
    .IsDependentOn("Show-Info")
    .IsDependentOn("Print-AppVeyor-Environment-Variables")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() => 
    {
        Information("Building {0}", BuildParameters.SolutionFilePath);

        // var msbuildSettings = new MSBuildSettings().
        //         SetPlatformTarget(ToolSettings.BuildPlatformTarget)
        //         .WithProperty("TreatWarningsAsErrors","true")
        //         .WithTarget("Build")
        //         .SetMaxCpuCount(ToolSettings.MaxCpuCount)
        //         .SetConfiguration(BuildParameters.Configuration)
        //         .WithLogger(
        //             Context.Tools.Resolve("MSBuild.ExtensionPack.Loggers.dll").FullPath,
        //             "XmlFileLogger",
        //             string.Format(
        //                 "logfile=\"{0}\";invalidCharReplacement=_;verbosity=Detailed;encoding=UTF-8",
        //                 BuildParameters.Paths.Files.BuildLogFilePath)
        //         );


        // MSBuild(BuildParameters.SolutionFilePath, msbuildSettings);

        var settings = new DotNetCoreBuildSettings
        {
            Configuration = BuildParameters.Configuration,
        };

        DotNetCoreBuild(sourceDirectoryPath, settings);
    });



Task("Package")
    // .IsDependentOn("Export-Release-Notes")
    // .IsDependentOn("Create-NuGet-Packages")
    // .IsDependentOn("Create-Chocolatey-Packages")
    // .IsDependentOn("Test")
    // .IsDependentOn("Analyze")
    ;

Task("Default")
    .IsDependentOn("Package");

// Task("AppVeyor")
//     .IsDependentOn("Upload-AppVeyor-Artifacts")
//     .IsDependentOn("Upload-Coverage-Report")
//     .IsDependentOn("Publish-MyGet-Packages")
//     .IsDependentOn("Publish-Chocolatey-Packages")
//     .IsDependentOn("Publish-Nuget-Packages")
//     .IsDependentOn("Publish-GitHub-Release")
//     .IsDependentOn("Publish-Documentation")
//     .Finally(() =>
// {
//     if(publishingError)
//     {
//         throw new Exception("An error occurred during the publishing of " + BuildParameters.Title + ".  All publishing tasks have been attempted.");
//     }
// });

RunTarget(BuildParameters.Target);