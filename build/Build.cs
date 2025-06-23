using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using DotNetTasks = Nuke.Common.Tools.DotNet.DotNetTasks;
using Serilog;

partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    #region Options

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Environment to build - Default is 'Development' (local) or 'Production' (server)")]
    public readonly Environment Environment = IsLocalBuild ? Environment.Development : Environment.Production;

    [Parameter("Platform to build - Default is 'AnyCPU'")]
    public string Platform = "AnyCPU";

    [Parameter("Warning Level")]
    public readonly int WarningLevel;

    [Parameter("Dry Run")]
    public readonly bool DryRun;

    #endregion

    #region Info

    Version _version = new Version("1.0.0.0");
    string _hash = string.Empty;
    string _versionTag = string.Empty;
    string _repoUrl = string.Empty;

    #endregion

    #region Locations

    [Solution]
    public readonly Solution Solution;

    [GitRepository]
    public readonly GitRepository Repository;

    static AbsolutePath SourceDirectory => RootDirectory / "src";

    static AbsolutePath TestsDirectory => RootDirectory / "tests";

    static AbsolutePath PublishDirectory => RootDirectory / "publish";

    static AbsolutePath ArtifactsDirectory => RootDirectory / "output";

    static AbsolutePath TestResultsDirectory => RootDirectory / "tests" / "results";

    static AbsolutePath ScriptsDirectory => RootDirectory / "scripts";

    #endregion

    Target Prepare => d => d
        .Before(Compile)
        .Executes(() =>
        {
            #region Projects

            Log.Information("Reading projects");
            foreach (var item in Solution.AllProjects)
            {
                var project = item.GetMSBuildProject();
                var build = project.Imports.FirstOrDefault(m => m.ImportedProject.EscapedFullPath.EndsWith("Directory.Build.props"));
                if (build.ImportedProject != null)
                {
                    // Variables
                    var pg1 = build.ImportedProject.PropertyGroups.FirstOrDefault(m => m.Properties.Any(p => p.Name == "SolutionDir"));
                    pg1?.SetProperty("SolutionDir", Solution.Directory);
                    if (build.ImportedProject.HasUnsavedChanges)
                    {
                        build.ImportedProject.Save();
                    }

                    build.ImportedProject.Reload();
                }

                project.ReevaluateIfNecessary();
            }

            #endregion

            #region Properties

            using var gitUrl = new Process();
            gitUrl.StartInfo = new ProcessStartInfo(fileName: "git", arguments: "config --get remote.origin.url") { WorkingDirectory = SourceDirectory, RedirectStandardOutput = true, UseShellExecute = false };
            gitUrl.Start();
            _repoUrl = gitUrl.StandardOutput.ReadLine()?.Trim().Split(separator: " ", StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            gitUrl.WaitForExit();

            #endregion
        });

    Target Clean => d => d
        .Before(Restore)
        .Executes(() =>
        {
            Log.Information("Cleaning Output Directories");
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach((path) => path.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach((path) => path.DeleteDirectory());
            Log.Information("Cleaning Test Results Directory");
            AbsolutePath.Create(TestResultsDirectory).CreateOrCleanDirectory();
            Log.Information("Cleaning Scripts Directory");
            AbsolutePath.Create(ScriptsDirectory).CreateOrCleanDirectory();
            Log.Information("Cleaning Publish Directory");
            AbsolutePath.Create(PublishDirectory).CreateOrCleanDirectory();
            Log.Information("Cleaning Artifacts Directory");
            AbsolutePath.Create(ArtifactsDirectory).CreateOrCleanDirectory();

            AbsolutePath.Create(TestResultsDirectory).CreateOrCleanDirectory();
            AbsolutePath.Create(TestsDirectory / "coverage").CreateOrCleanDirectory();

            if (!DryRun)
            {
                return;
            }

            Log.Information("Cleaning NuGet Cache Directory");
            var packages = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            if (Path.Exists(packages))
            {
                Directory.Delete(packages, true);
            }
        });

    Target Restore => d => d
        .Executes(() =>
        {
            Log.Information("Restoring tools");

            DotNetTasks.DotNetToolRestore(options => options.SetProcessWorkingDirectory(SourceDirectory));

            Log.Information("Restoring nugets");

            DotNetTasks.DotNetRestore(s => s
                .SetWarningLevel(WarningLevel)
                .SetVerbosity(GetDotNetVerbosity())
                .SetPlatform(Platform)
                .SetConfigFile(RootDirectory / ".nuget" / "NuGet.config")
                .CombineWith(Solution.AllProjects, (x, v) => x
                    .SetProjectFile(v)));
        });

    Target Compile => d => d
        .DependsOn(Clean)
        .DependsOn(Restore)
        .DependsOn(Prepare)
        .Executes(() =>
        {
            var items = Solution.AllProjects;
            var target = from item in items
                from framework in item.GetTargetFrameworks()?.Where(m => !string.IsNullOrEmpty(m))
                select new { framework, item };

            DotNetTasks.DotNetBuild(s => s
                .SetProcessWorkingDirectory(Solution.Directory)
                .SetWarningLevel(WarningLevel)
                .SetVerbosity(GetDotNetVerbosity())
                .SetConfiguration(Configuration)
                .SetProperty("Environment", Environment.ToString())
                .EnableNoRestore()
                .CombineWith(target, configurator: (x, v) => x
                    .SetProjectFile(v.item.Path)
                ));
        });

    Target Test => d => d
        .DependsOn(Compile)
        .Executes(() =>
        {
            var tests = Solution.AllProjects
                .Where(s => s.Name.Contains("Test", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (tests.Length == 0)
            {
                throw new NullReferenceException("No tests found");
            }

            DotNetTasks.DotNetTest(s => s
                .EnableNoRestore()
                .EnableNoBuild()
                .SetConfiguration(Configuration)
                .When(v => true, configurator: x => x
                    .SetLoggers("trx")
                    .SetResultsDirectory(TestResultsDirectory))
                .CombineWith(tests, configurator: (x, v) => x
                    .SetProjectFile(v.Path)));

            DotNetTasks.DotNetTest(s => s
                .SetVerbosity(GetDotNetVerbosity())
                .SetConfiguration(Configuration)
                .SetCollectCoverage(true)
                .SetCoverletOutputFormat("cobertura")
                .SetCoverletOutput(TestsDirectory / "coverage")
                .SetLoggers("trx")
                .SetResultsDirectory(TestResultsDirectory)
                .SetDataCollector("XPlat Code Coverage")
                .CombineWith(tests, configurator: (x, v) => x
                    .SetProjectFile(v.Path)));
        });

    Target Coverage => d => d
        .DependsOn(Test)
        .After(Test)
        .Executes(() =>
        {
            DotNetTasks.DotNetToolRestore(s => s.SetProcessWorkingDirectory(SourceDirectory));
            AbsolutePath.Create(TestsDirectory / "coverage").CreateOrCleanDirectory();
            DotNetTasks.DotNet($"reportgenerator -reports:\"{TestResultsDirectory}/**/coverage.cobertura.xml\" -targetdir:{TestsDirectory / "coverage"} -reporttypes:\"cobertura;html;teamcitysummary\"");
            TestResultsDirectory.GlobFiles("*.trx")
                .ForEach(m =>
                {
                    DotNetTasks.DotNet($"trx2junit {m}");
                });
        });

    static DotNetVerbosity GetDotNetVerbosity()
    {
        return Verbosity switch
        {
            Verbosity.Minimal => DotNetVerbosity.minimal,
            Verbosity.Verbose => DotNetVerbosity.detailed,
            Verbosity.Quiet => DotNetVerbosity.quiet,
            Verbosity.Normal => DotNetVerbosity.normal,
            _ => DotNetVerbosity.diagnostic
        };
    }

    [GeneratedRegex(@"v?\=?((?:[0-9]{1,}\.{0,}){1,})\-?(.*)", RegexOptions.Compiled)]
    private static partial Regex VersionRegex();

    [GeneratedRegex(@"\[assembly: AssemblyVersion\(.*\)\]", RegexOptions.Compiled)]
    private static partial Regex AssemblyVersionRegex();

    [GeneratedRegex(@"\[assembly: AssemblyFileVersion\(.*\)\]", RegexOptions.Compiled)]
    private static partial Regex AssemblyFileVersionRegex();

    [GeneratedRegex(@"\[assembly: AssemblyInformationalVersion\(.*\)\]", RegexOptions.Compiled)]
    private static partial Regex AssemblyInformationalVersionRegex();
}
