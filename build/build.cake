#tool "dotnet:?package=coverlet.console"
#addin "nuget:?package=Cake.Coverlet"

using System.Linq;

// Target - The task you want to start. Runs the Default task if not specified.
var target = Argument("Target", "Default");

// Configuration - The build configuration (Debug/Release) to use.
// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if an Environment variable exists, use that.
var configuration = 
    HasArgument("Configuration") 
        ? Argument<string>("Configuration") 
        : EnvironmentVariable("Configuration") ?? "Release";

// A directory path to an Artifacts directory.
var artifactsDirectory = MakeAbsolute(Directory("./artifacts"));

// Directories with tests template
var testsPathTemplate = "../tests/**/*.Tests.csproj";
 
// Deletes the contents of the Artifacts folder if it should contain anything from a previous build.
Task("Clean")
    .Does(() =>
    {
        CleanDirectory(artifactsDirectory);
    });
 
// Find all csproj projects and build them using the build configuration specified as an argument.
 Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetBuild(
            "..", 
            new DotNetBuildSettings
            {
                Configuration = configuration,
                ArgumentCustomization = args => args.Append("--configfile ./NuGet.config")
            }
        );
    });

// Look under a 'Tests' folder and run dotnet test against all of those projects.
Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var projects = GetFiles(testsPathTemplate);
        var settings = new DotNetTestSettings
        {
            Configuration = configuration,
            NoRestore = true,
            NoBuild = true,
        };

        foreach(var project in projects)
            DotNetTest(project.FullPath, settings);
    });

// Look under a 'test' folder and calculate tests against all of those projects.
// Then calculates coverage and stores results in the artifacts directory
Task("CalculateCoverage")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var projects = GetFiles(testsPathTemplate).ToArray();
        var temporaryCoverageFile = artifactsDirectory.CombineWithFilePath("coverage.json");

        var coverletsettings = new CoverletSettings 
        {
            CollectCoverage = true,
            CoverletOutputDirectory = artifactsDirectory,
            CoverletOutputName = temporaryCoverageFile.GetFilenameWithoutExtension().ToString(),
            MergeWithFile = temporaryCoverageFile,
            Include = new List<string> {"[*]*"},
            Exclude = new List<string> 
            {
                "[xunit*]*",
                "[*.Tests]*"
            }
        };

        for(var i = 0; i < projects.Length; i++)
        {
            var project = projects[i];
            var projectName = project.GetFilenameWithoutExtension();
            var projectAbsolutePath = project.GetDirectory();
            var projectDll = GetFiles($"{projectAbsolutePath}/bin/Debug/*/*{projectName}.dll").First();

            if(i == projects.Length - 1)
                coverletsettings.CoverletOutputFormat = CoverletOutputFormat.opencover;

            Coverlet(projectDll, project, coverletsettings);
        }
    });

// Run dotnet pack to produce NuGet packages from our projects.
Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetPack(
            "..", 
            new DotNetPackSettings
            {
                Configuration = configuration,
                NoRestore = true,
                NoBuild = true,
                OutputDirectory = artifactsDirectory,
                IncludeSymbols = true,
                MSBuildSettings 
                    = new DotNetMSBuildSettings
                      {
                          Properties = { {"SymbolPackageFormat", new[] {"snupkg"} } }
                      }
            }
        );
    });

// The default task to run if none is explicitly specified. In this case, we want
// to run everything starting from Clean, all the way up to Test.
Task("Default")
    .IsDependentOn("Test");
 
// Executes the task specified in the target argument.
RunTarget(target);