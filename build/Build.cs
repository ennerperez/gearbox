#if USING_7ZIP
using System.Text;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using DotNetEnv;
#if USING_DATABASE_PROVIDER
using Nuke.Common.Tools.EntityFramework;
#endif
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MauiCheck;
using Nuke.Common.Tools.ReportGenerator;
#if USING_SONARQUBE
using System.Text.Json;
using Nuke.Common.Tools.SonarScanner;
#endif
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1050 // Declare types in namespaces

public partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Pack);

    private const string EnvironmentProperty = "Environment";
    private const string PlatformProperty = "Platform";
    private const double CoverageLineThreshold = 85.0;
    private const double CoverageBranchThreshold = 60.0;

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

    #region MAUI

    [Parameter("Package Signing", Name = "Signing")]
    public readonly bool PackageSigning;

    [Secret]
    [Parameter("Android Signing Key Alias")]
    public readonly string AndroidSigningKeyAlias;

    [Secret]
    [Parameter("Android Signing Key Pass")]
    public readonly string AndroidSigningKeyPass;

    [Secret]
    [Parameter("Android Signing Store Pass")]
    public readonly string AndroidSigningStorePass;

    #endregion

    #region Locations

    [Solution]
    public readonly Solution Solution;

    [GitRepository]
    public readonly GitRepository Repository;

    static AbsolutePath SourceDirectory => RootDirectory / "src";
    static AbsolutePath TestsDirectory => RootDirectory / "tests";
    static AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    static AbsolutePath TestResultsDirectory => ArtifactsDirectory / "results";
    static AbsolutePath CoverageDirectory => ArtifactsDirectory / "coverage";
    static AbsolutePath PublishDirectory => ArtifactsDirectory / "publish";
    static AbsolutePath OutputDirectory => ArtifactsDirectory / "output";

    #endregion

    #region Information

    [Parameter]
    public readonly string Author;

    [Parameter]
    public readonly string Product;

    [Parameter]
    public readonly string ProjectUrl;

    [Parameter]
    public readonly string PackageId;

    Version _version = new("1.0.0.0");
    Version _fileVersion = new("1.0.0.0");
    string _informationalVersion = string.Empty;
    string _semVersion = string.Empty;
    string _hash = string.Empty;
    string _versionTag = string.Empty;

    bool _useMaui;
    string _repoUrl = string.Empty;
    string _androidExt = string.Empty;
    string _iOSExt = string.Empty;

    #endregion

    #region Projects

    [Parameter]
    public readonly string Project;

    [Required, Parameter("Projects to Build and Deploy")]
    public readonly string[] Projects;

    private Dictionary<string, string[]> _projects;
    private Project[] _webProjects;
    private Project[] _serviceProjects;
    private Project[] _desktopProjects;
    private Project[] _mobileProjects;
    private Project[] _packageProjects;
    private Project[] _testProjects;

    #endregion

    Target Information => d => d
        .DependsOn(Prepare)
        .Executes(() =>
        {
            if (Environment != Environment.Development)
            {
                return;
            }

            var envs = System.Environment.GetEnvironmentVariables();
            Log.Information("Reading Environment");
            foreach (var env in envs.Keys)
            {
                Log.Information("{Key}: {Value}", env, envs[env]);
            }
        });

    Target Prepare => d => d
        .Before(Restore)
        .Executes(() =>
        {
            var envFile = (RootDirectory / ".env");
            if (envFile.Exists())
            {
                Log.Information("Reading .env file");
            }
            else
            {
                File.Copy(RootDirectory / ".env.example", RootDirectory / ".env");
                Log.Warning(".env File not found");
            }

            Env.Load(envFile);

            #region Projects

            _projects = Projects.Select(m => new
                {
                    key = m.Split(":").FirstOrDefault(),
                    value = m.Split(":").LastOrDefault()?.Split(",")
                })
                .GroupBy(m => m.key)
                .ToDictionary(k => k.Key, v => v.SelectMany(m => m.value).ToArray());

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

            _webProjects = _projects.TryGetValue("Web", out var project1) ? Solution.AllProjects.Where(m => project1.Contains(m.Name)).ToArray() : [];
            _serviceProjects = _projects.TryGetValue("Service", out var project2) ? Solution.AllProjects.Where(m => project2.Contains(m.Name)).ToArray() : [];
            _desktopProjects = _projects.TryGetValue("Desktop", out var project3) ? Solution.AllProjects.Where(m => project3.Contains(m.Name)).ToArray() : [];
            _mobileProjects = _projects.TryGetValue("Mobile", out var project4) ? Solution.AllProjects.Where(m => project4.Contains(m.Name)).ToArray() : [];
            _packageProjects = _projects.TryGetValue("Package", out var project5) ? Solution.AllProjects.Where(m => project5.Contains(m.Name)).ToArray() : [];
            _testProjects = _projects.TryGetValue("Test", out var project6) ? Solution.AllProjects.Where(m => project6.Contains(m.Name)).ToArray() : [];

            #endregion

            #region Properties

            try
            {
                _repoUrl = GitTasks.Git("config --get remote.origin.url", Repository.LocalDirectory ?? RootDirectory).StdToText();
            }
            catch (Exception)
            {
                // ignored
            }

            _useMaui = Solution.AllProjects.Select(m =>
            {
                var evaluatedValue = m.GetMSBuildProject()?.GetProperty("UseMaui")?.EvaluatedValue;
                return evaluatedValue != null && bool.Parse(evaluatedValue);
            }).Any(m => m);

            #endregion

            #region Specials

            if (!_useMaui)
            {
                return;
            }

            _androidExt = Environment == Environment.Production ? "aab" : "apk";
            _iOSExt = Platform != null && Platform.Contains("iPhoneSimulator", StringComparison.InvariantCultureIgnoreCase) ? "app" : "ipa";

            #endregion
        });

    Target Restore => d => d
        .DependsOn(Prepare)
        .Executes(() =>
        {
            Log.Information("Restoring tools");
            DotNetToolRestore(s => s.SetProcessWorkingDirectory(RootDirectory));

            if (_useMaui)
            {
                Log.Information("Checking for MAUI workload installation");
                MauiCheckTasks.MauiCheck(c => c.SetNonInteractive(true));
            }

            Log.Information("Restoring nugets");
            DotNetRestore(s => s
                .SetWarningLevel(WarningLevel)
                .SetVerbosity(getDotNetVerbosity())
                .SetPlatform(Platform)
                .SetConfigFile(RootDirectory / "NuGet.config")
                .SetProperty(EnvironmentProperty, Environment)
                .CombineWith(Solution.AllProjects, (x, v) => x
                    .SetProjectFile(v)));
        });

    Target Clean => d => d
        .Before(Prepare)
        .Executes(() =>
        {
            Log.Information("Cleaning Directories");

            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach((path) => path.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach((path) => path.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
            PublishDirectory.CreateOrCleanDirectory();
            OutputDirectory.CreateOrCleanDirectory();

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

    Target Versioning => d => d
        .DependsOn(Restore)
        .Executes(() =>
        {
            try
            {
                var gitVersion = GitVersionTasks.GitVersion().Result;
                _version = new Version(gitVersion.AssemblySemVer); //"2.0.0"
                _fileVersion = new Version(gitVersion.AssemblySemFileVer); //"2.0.0.0"
                _versionTag = gitVersion.PreReleaseLabel; //"alpha"
                _semVersion = gitVersion.FullSemVer; //"2.0.0-alpha.12"
                _hash = gitVersion.Sha; //"7b6bb054ff5d0554e151b0802a818ae0dc4c24a6"
                _informationalVersion = $"{gitVersion.FullSemVer}+{gitVersion.Sha}"; //"2.0.0-alpha.12+7b6bb054ff5d0554e151b0802a818ae0dc4c24a6"
            }
            catch (Exception e)
            {
                Log.Warning(e, "{Message}", e.Message);
                var versionRegex = VersionRegex();
                Tuple<Version, string> tag;

                try
                {
                    tag = GitTasks.Git("describe --tags --always --abbrev=0").Select(m => m.Text)
                        .Select(m => versionRegex.Match(m))
                        .Where(m => m.Success)
                        .Select(m => new Tuple<Version, string>(Version.Parse(m.Groups[1].Value), m.Groups[2].Value))
                        .FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "{Message}", "Unable to get repository tags using CLI.");
                    using var gitTag = new Process();
                    gitTag.StartInfo = new ProcessStartInfo(fileName: "git", arguments: "describe --tags --always --abbrev=0") { WorkingDirectory = RootDirectory, RedirectStandardOutput = true, UseShellExecute = false };
                    gitTag.Start();
                    try
                    {
                        tag = new[] { gitTag.StandardOutput.ReadToEnd().Trim() }
                            .Select(m => versionRegex.Match(m))
                            .Where(m => m.Success)
                            .Select(m => new Tuple<Version, string>(Version.Parse(m.Groups[1].Value), m.Groups[2].Value))
                            .FirstOrDefault();
                    }
                    catch (Exception)
                    {
                        tag = new Tuple<Version, string>(new Version("1.0.0"), "dev");
                    }
                }

                _version = tag?.Item1;
                _fileVersion = _version;
                _versionTag = tag?.Item2;
                _semVersion = $"{_version:3}{(!string.IsNullOrWhiteSpace(_versionTag) ? $"-{_versionTag}" : "")}";
                _hash = Repository.Commit;
                _informationalVersion = $"{_semVersion}+{_hash}";
            }

            Log.Information("Version: {Version} \n Tag: {VersionTag} \n Hash: {Hash}", _version, _versionTag, _hash);

            if (_version == null)
            {
                Log.Warning("Version was not detected");
                _version = new Version("1.0.0");
            }

            if (Environment == Environment.Development)
            {
                return;
            }

            var assemblyInfoFiles = SourceDirectory.GetFiles("AssemblyInfo*.cs", int.MaxValue);
            foreach (var assemblyInfoVersionFile in assemblyInfoFiles)
            {
                Log.Information("Patching: {File}", assemblyInfoVersionFile);

                var content = File.ReadAllText(assemblyInfoVersionFile);
                content = AssemblyVersionRegex().Replace(content, $"[assembly: AssemblyVersion(\"{_version}\")]");
                content = AssemblyFileVersionRegex().Replace(content, $"[assembly: AssemblyFileVersion(\"{_fileVersion}\")]");
                content = AssemblyInformationalVersionRegex().Replace(content, $"[assembly: AssemblyInformationalVersion(\"{_informationalVersion}\")]");

                File.WriteAllText(assemblyInfoVersionFile, content);
            }

            if (!_useMaui)
            {
                return;
            }

            var applicationVersion = ((int)(DateTime.UtcNow.Ticks / 100000000)).ToString();
            var applicationDisplayVersion = _semVersion;

            if (Verbosity == Verbosity.Minimal)
            {
                Console.WriteLine($"// ApplicationVersion: {applicationVersion}");
                Console.WriteLine($"// ApplicationDisplayVersion: {applicationDisplayVersion}");
            }
            else
            {
                Log.Information("ApplicationVersion: {ApplicationVersion}", applicationVersion);
                Log.Information("ApplicationDisplayVersion: {ApplicationDisplayVersion}", applicationDisplayVersion);
            }

            var items = loadPublishProjects();
            foreach (var item in items.Where(m => m.useMaui).Select(m => m.project))
            {
                var project = item.GetMSBuildProject();

                // Version
                project.SetProperty("ApplicationVersion", applicationVersion);
                project.SetProperty("ApplicationDisplayVersion", applicationDisplayVersion);

                project.Save(item.Path);
                project.ReevaluateIfNecessary();
            }
        });

    Target Compile => d => d
        .DependsOn(Clean)
        .DependsOn(Prepare)
        .DependsOn(Restore)
        .DependsOn(Versioning)
        .Executes(() =>
        {
            var items = loadPublishProjects();
            var target = from item in items
                from framework in item.project.GetTargetFrameworks()?.Where(m => !string.IsNullOrEmpty(m))
                select new { framework, item.project };

            if (_useMaui)
            {
                target = from item in target
                    where item.framework.Contains("ios", StringComparison.OrdinalIgnoreCase) && Platform.Contains("iPhone", StringComparison.InvariantCultureIgnoreCase) ||
                          item.framework.Contains("android", StringComparison.OrdinalIgnoreCase) && Platform.Contains("Android", StringComparison.InvariantCultureIgnoreCase)
                    select new { item.framework, item.project };

                DotNetBuild(s => s
                    .SetProcessWorkingDirectory(Solution.Directory)
                    .SetWarningLevel(WarningLevel)
                    .SetVerbosity(getDotNetVerbosity())
                    .SetConfiguration(Configuration)
                    .SetProperty(EnvironmentProperty, Environment)
                    .EnableNoRestore()
                    .CombineWith(target, configurator: (x, v) => x
                        .SetProjectFile(v.project.Path)
                        .When(_ => v.framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("ArchiveOnBuild", true))
                        .When(_ => v.framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "ios-arm64"))
                        .When(_ => v.framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("AndroidPackageFormats", _androidExt))
                        .When(_ => v.framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "android-arm64"))
                    ));
                return;
            }

            DotNetBuild(s => s
                .SetProcessWorkingDirectory(Solution.Directory)
                .SetWarningLevel(WarningLevel)
                .SetVerbosity(getDotNetVerbosity())
                .SetConfiguration(Configuration)
                .SetProperty(EnvironmentProperty, Environment)
                .EnableNoRestore()
                .CombineWith(target, configurator: (x, v) => x
                    .SetProjectFile(v.project.Path)
                ));
        });

    Target Tests => d => d
        .DependsOn(Compile)
        .Executes(() =>
        {
            if (_testProjects.Length == 0)
            {
                throw new OperationCanceledException("No tests found");
            }

            DotNetBuild(c => c
                .SetNoIncremental(true)
                .SetProjectFile(Solution.Path)
            );

            DotNetToolInstall(s => s
                .SetPackageName("dotnet-coverage")
                .SetGlobal(true)
            );

            var coverageProcess = new ProcessStartInfo("dotnet-coverage",
                $"collect \"dotnet test {Solution.Path} --logger \"\"trx\"\" --collect:\"\"XPlat Code Coverage\"\" --results-directory \"\"{TestResultsDirectory}\"\" --verbosity normal --no-build --no-restore\" -f xml -o \"{(CoverageDirectory / "coverage.xml")}\"")
            {
                WorkingDirectory = RootDirectory
            };
            Process.Start(coverageProcess)?.WaitForExit();

            var mergeProcess = new ProcessStartInfo("dotnet-coverage", "merge ./coverage/**/coverage.cobertura.xml --output ./coverage/merged.coverage.xml --output-format xml")
            {
                WorkingDirectory = RootDirectory
            };
            Process.Start(mergeProcess)?.WaitForExit();

            ReportGeneratorTasks.ReportGenerator(s => s
                .SetReports($"{CoverageDirectory}/merged.coverage.xml")
                .SetAssemblyFilters("+*")
                .SetFileFilters("+*")
                .SetReportTypes("cobertura;html;teamcitysummary")
                .SetTargetDirectory(TestResultsDirectory / "reports")
            );

            AssertCoverageThresholds(TestResultsDirectory / "reports" / "Cobertura.xml");

            DotNet($"trx2junit {TestResultsDirectory}/*.trx");
        });

    Target Publish => d => d
        .DependsOn(Clean)
        .DependsOn(Prepare)
        .DependsOn(Restore)
        .DependsOn(Versioning)
        .DependsOn(Compile)
        .Executes(() =>
        {
            var items = loadPublishProjects();
            var publishProjects = from item in items
                from framework in item.project.GetTargetFrameworks()?.Where(m => !string.IsNullOrEmpty(m))
                select new { item.project, framework };

            if (_useMaui)
            {
                publishProjects = from item in publishProjects
                    where item.framework.Contains("ios", StringComparison.OrdinalIgnoreCase) && Platform.Contains("iPhone", StringComparison.InvariantCultureIgnoreCase) ||
                          item.framework.Contains("android", StringComparison.OrdinalIgnoreCase) && Platform.Contains("Android", StringComparison.InvariantCultureIgnoreCase)
                    select new { item.project, item.framework };
            }

            (bool hasWeb, bool hasService, bool hasMobile, bool hasDesktop, bool hasPackage) = GetProjectPresenceFlags(items.Select(m => m.project.Name).ToArray());

            if (hasWeb || hasService)
            {
                publishProjects = publishProjects.Where(m =>
                    _webProjects.Select(p => p.Name).Contains(m.project.Name) ||
                    _serviceProjects.Select(p => p.Name).Contains(m.project.Name)
                ).ToArray();
                DotNetPublish(s => s
                    .SetWarningLevel(WarningLevel)
                    .SetVerbosity(getDotNetVerbosity())
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .SetConfiguration(Configuration)
                    .DisablePublishSingleFile()
                    .CombineWith(publishProjects, configurator: (x, v) => x
                        .SetProject(v.project)
                        .SetFramework(v.framework)
                        .SetOutput(PublishDirectory / v.project.Name)));
            }
            else if (hasMobile)
            {
                publishProjects = publishProjects.Where(m =>
                    _mobileProjects.Select(p => p.Name).Contains(m.project.Name)
                ).ToArray();
                DotNetPublish(s => s
                    .SetWarningLevel(WarningLevel)
                    .SetVerbosity(getDotNetVerbosity())
                    .SetConfiguration(Configuration)
                    .SetPlatform(Platform)
                    .CombineWith(publishProjects, configurator: (x, v) => x
                        .SetProject(v.project)
                        .SetOutput(PublishDirectory / v.project.Name / v.framework)
                        .SetFramework(v.framework)
                        .When(_ => v.framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("ArchiveOnBuild", true))
                        .When(_ => v.framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "ios-arm64"))
                        .When(_ => v.framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("AndroidPackageFormats", _androidExt))
                        .When(_ => v.framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "android-arm64"))
                        .When(_ => v.framework.Contains("android", StringComparison.OrdinalIgnoreCase) && PackageSigning, (c) => c
                            .SetProperty("AndroidKeyStore", true)
                            .SetProperty("AndroidSigningKeyStore", SourceDirectory / ".certs" / $"{v.project.Name}.keystore")
                            .SetProperty("AndroidSigningKeyAlias", AndroidSigningKeyAlias)
                            .SetProperty("AndroidSigningKeyPass", AndroidSigningKeyPass)
                            .SetProperty("AndroidSigningStorePass", AndroidSigningStorePass)
                        )
                    )
                );
            }
            else if (hasDesktop)
            {
                publishProjects = publishProjects.Where(m =>
                    _desktopProjects.Select(p => p.Name).Contains(m.project.Name)
                ).ToArray();
                DotNetPublish(s => s
                    .SetWarningLevel(WarningLevel)
                    .SetVerbosity(getDotNetVerbosity())
                    .SetConfiguration(Configuration)
                    .EnableSelfContained()
                    .CombineWith(publishProjects, configurator: (x, v) => x
                        .When(_ => _useMaui, y => y.SetPlatform(Platform))
                        .SetProject(v.project)
                        .SetFramework(v.framework)
                        .SetOutput(PublishDirectory / v.project.Name)));
            }
            else if (hasPackage)
            {
                publishProjects = publishProjects.Where(m =>
                    _packageProjects.Select(p => p.Name).Contains(m.project.Name)
                ).ToArray();
                foreach (var item in publishProjects.Select(m => m.project))
                {
                    var projectInfo = Solution.AllProjects.FirstOrDefault(p => p.Name == item.Name);
                    if (projectInfo != null)
                    {
                        DotNetPack(s => s
                            .SetWarningLevel(WarningLevel)
                            .SetVerbosity(getDotNetVerbosity())
                            .SetProject(projectInfo)
                            .SetConfiguration(Configuration)
                            .AddProperty("Icon", ".editoricon.png")
                            .SetTitle($"{Product} {projectInfo.Name}")
                            .SetAuthors(Author)
                            .SetDescription($"{Product} {projectInfo.Name}")
                            .SetCopyright($"Copyright \u00a9 {Author}")
                            .SetVersion(_semVersion)
                            .SetVersionSuffix(_version.ToString(3))
                            .SetRepositoryUrl(_repoUrl)
                            .SetRepositoryType("git")
                            .SetOutputDirectory(PublishDirectory / item.Name));
                    }
                    else
                    {
                        throw new NotSupportedException("Project not supported");
                    }
                }
            }

            Log.Information("Output: {PublishDirectory}", PublishDirectory);
        });

    Target Pack => d => d
        .DependsOn(Publish)
        .Executes(() =>
        {
            var items = loadPublishProjects();
            var target = from item in items
                from framework in item.project.GetTargetFrameworks()?.Where(m => !string.IsNullOrEmpty(m))
                select new { framework, item.project };

            if (_useMaui)
            {
                target = from item in target
                    where item.framework.Contains("ios", StringComparison.OrdinalIgnoreCase) && Platform.Contains("iPhone", StringComparison.InvariantCultureIgnoreCase) ||
                          item.framework.Contains("android", StringComparison.OrdinalIgnoreCase) && Platform.Contains("Android", StringComparison.InvariantCultureIgnoreCase)
                    select new { item.framework, item.project };

                if (!PackageSigning)
                {
                    OutputDirectory.GlobFiles($"**/*-android/*Signed.{_androidExt}").DeleteFiles();
                }
            }

            (bool hasWeb, bool hasService, bool hasMobile, bool hasDesktop, bool hasPackage) = GetProjectPresenceFlags(items.Select(m => m.project.Name).ToArray());

#if USING_DATABASE_PROVIDER

            if (Startup != null && Targets != null && (hasWeb || hasService || hasPackage))
            {
                var scriptsDir = (OutputDirectory / "scripts");
                scriptsDir.CreateDirectory();

                var bundlesDir = (OutputDirectory / "bundles");
                bundlesDir.CreateDirectory();

                var stamp = DateTime.UtcNow.ToString("yyyyMMdd");

                var combinations = GetConnectionStringsCombinations()
                    .Where(IsDbContextRecord);

                foreach (var item in combinations)
                {
                    if (!string.Equals(item.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        var scriptFile = (scriptsDir / $"{item.Provider}_{item.Name}_{stamp}.sql");
                        scriptFile.DeleteFile();
                        var targets = Targets.Where(m => m.Name.EndsWith(item.Provider)).ToArray();
                        if (targets.Length == 0) { continue; }

                        EntityFrameworkTasks.EntityFrameworkMigrationsScript(c => c
                            .SetProcessWorkingDirectory(RootDirectory)
                            .EnableIdempotent()
                            .SetStartupProject(Startup)
                            .SetVerbose(true)
                            .SetContext(item.Name)
                            .SetOutput(scriptFile)
                            .CombineWith(targets, configurator: (buildSettings, v) => buildSettings
                                .SetProject(v)
                            )
                        );
                    }
                    else if (string.Equals(item.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
                    {
                        var databaseFile = (bundlesDir / $"{item.Provider}_{item.Name}_{stamp}.db");
                        databaseFile.DeleteFile();
                        var targets = Targets.Where(m => m.Name.EndsWith(item.Provider)).ToArray();
                        if (targets.Length == 0) { continue; }

                        EntityFrameworkTasks.EntityFrameworkDatabaseUpdate(c => c
                            .SetProcessWorkingDirectory(RootDirectory)
                            .SetStartupProject(Startup)
                            .SetVerbose(true)
                            .SetContext(item.Name)
                            .SetConnection($"Data Source={databaseFile}")
                            .CombineWith(targets, configurator: (buildSettings, v) => buildSettings
                                .SetProject(v)
                            )
                        );
                    }
                }
            }
#endif

            foreach (var item in target)
            {
                if (hasDesktop)
                {
#if USING_7ZIP
                    if (OperatingSystem.IsWindows())
                    {
                        AbsolutePathExtensions.DeleteFile($"{OutputDirectory}/{item.project.Name}.exe");
                        SevenZip.SevenZipBase.SetLibraryPath(Solution.Directory / ".build" / "7za.bin");
                        var compressor = new SevenZip.SevenZipCompressor();
                        compressor.CompressDirectory(Path.Combine(PublishDirectory, item.project.Name), Path.Combine(OutputDirectory, $"{item.project.Name}.7z"));

                        var configs = new[] { ";!@Install@!UTF-8!", $"Title=\"{Product} {item.project.Name}\"", $"ExecuteFile=\"{item.project.Name}.exe\"", ";!@InstallEnd@!" };
                        var array1 = File.ReadAllBytes(Solution.Directory / ".build" / $"{Product}.sfx");
                        var array2 = Encoding.UTF8.GetBytes(string.Join(System.Environment.NewLine, configs));
                        var array3 = File.ReadAllBytes($"{OutputDirectory / item.project.Name}.7z");
                        var data = array1.Concat(array2).Concat(array3).ToArray();
                        File.WriteAllBytes($"{OutputDirectory}/{item.project.Name}.exe", data);
                        AbsolutePathExtensions.DeleteFile($"{OutputDirectory}/{item.project.Name}.7z");
                    }
                    else
                    {
                      throw new InvalidOperationException("Unable to build in a non-windows machine");
                    }
#endif
                }
                else if (hasMobile)
                {
                    var change = AbsolutePath.Create(item.project.Directory / "Content").GlobFiles("CHANGES").FirstOrDefault();
                    var files = AbsolutePath.Create(item.project.Directory / "Content").GlobFiles("CHANGES.*.txt").ToArray();
                    files = files.Append(change).ToArray();
                    var notesPath = PublishDirectory / item.project.Name / item.framework / "Notes";
                    notesPath.CreateOrCleanDirectory();
                    foreach (var file in files)
                    {
                        File.Copy(file.ToString(), notesPath / file.Name);
                    }

                    AbsolutePath.Create(OutputDirectory / "Android").CreateOrCleanDirectory();
                    AbsolutePath.Create(OutputDirectory / "iOS").CreateOrCleanDirectory();

                    PublishDirectory.GlobFiles($"**/*-android/*{(PackageSigning ? "-Signed" : string.Empty)}.{_androidExt}").ForEach(x => x.MoveToDirectory(ArtifactsDirectory / "Android"));
                    PublishDirectory.GlobFiles($"**/*-ios/*.{_iOSExt}").ForEach(x => x.MoveToDirectory(OutputDirectory / "iOS"));

                    // R8 Mapping
                    PublishDirectory.GlobFiles($"**/*-android/mapping.txt").ForEach(x => x.MoveToDirectory(OutputDirectory / "Android"));

                    // Changes
                    PublishDirectory.GlobFiles($"**/*-android/Notes/CHANGES").ForEach(x =>
                    {
                        (OutputDirectory / "Android" / "Notes").CreateDirectory();
                        x.MoveToDirectory(OutputDirectory / "Android" / "Notes");
                    });
                    PublishDirectory.GlobFiles($"**/*-ios/Notes/*.*.txt").ForEach(x =>
                    {
                        (OutputDirectory / "iOS" / "Notes").CreateDirectory();
                        x.MoveToDirectory(OutputDirectory / "iOS" / "Notes");
                    });

                    //Rename Packages
                    (OutputDirectory / "Android").GlobFiles($"*.{_androidExt}").FirstOrDefault()?.RenameWithoutExtension($"{PackageId}", ExistsPolicy.FileOverwrite);
                    (OutputDirectory / "iOS" / "").GlobFiles($"*.{_iOSExt}").FirstOrDefault()?.RenameWithoutExtension($"{PackageId}", ExistsPolicy.FileOverwrite);
                }
                else if (hasWeb || hasService)
                {
                    if (!_webProjects.Select(m => m.Name).Contains(item.project.Name) && !_serviceProjects.Select(m => m.Name).Contains(item.project.Name))
                    {
                        continue;
                    }

                    (OutputDirectory / $"{item.project.Name}.zip").DeleteFile();
                    (PublishDirectory / $"{item.project.Name}" / ".env").DeleteFile();
                    ZipFile.CreateFromDirectory(PublishDirectory / item.project.Name, OutputDirectory / $"{item.project.Name}.zip");
                }
                else if (hasPackage)
                {
                    var packageId = item.project.GetProperty("PackageId");
                    (OutputDirectory / $"{packageId}.*.nupkg").DeleteFile();
                    var nugets = (PublishDirectory / item.project.Name).GetFiles($"{packageId}.*.nupkg");
                    foreach (var nuget in nugets)
                    {
                        (OutputDirectory / nuget.Name).DeleteFile();
                        nuget.Move((OutputDirectory / nuget.Name), ExistsPolicy.FileOverwrite);
                    }
                }
            }

            Log.Information("Output: {OutputDirectory}", OutputDirectory);
            return Task.CompletedTask;
        });

    Target Analyze => d => d
        .DependsOn(Clean)
        .DependsOn(Prepare)
        .DependsOn(Restore)
        .Executes(() =>
        {
#if USING_SONARQUBE
            var lintFile = (RootDirectory / ".sonarlint" / $"{Solution.Name}.json");
            SonarLint lint;
            if (lintFile.Exists())
            {
                lint = JsonSerializer.Deserialize<SonarLint>(File.ReadAllText(lintFile));
            }
            else
            {
                lint = new SonarLint(
                    System.Environment.GetEnvironmentVariable("SONAR_HOST_URL"),
                    System.Environment.GetEnvironmentVariable("SONAR_TOKEN"),
                    System.Environment.GetEnvironmentVariable("SONAR_PROJECT_KEY")
                );
            }

            if (lint != null)
            {
                SonarScannerTasks.SonarScannerBegin(s => s
                    .SetProjectKey(lint.projectKey)
                    .SetToken(lint.sonarQubeToken)
                    .SetVersion(_semVersion ?? _version.ToString())
                    .SetAdditionalParameter("sonar.host.url", lint.sonarQubeUri)
                    .SetAdditionalParameter("sonar.exclusions", "**/.sonarlint/*.*, **/.nuke/*.*, **/Migrations/**/*.*")
                    .SetAdditionalParameter("sonar.cs.vscoveragexml.reportsPaths", $"{CoverageDirectory}/merged.coverage.xml")
                );

                var items = loadPublishProjects();
                var target = from item in items
                    from framework in item.project.GetTargetFrameworks()?.Where(m => !string.IsNullOrEmpty(m))
                    select new { framework, item.project };

                if (!_useMaui)
                {
                    DotNetBuild(s => s
                        .SetProcessWorkingDirectory(Solution.Directory)
                        .SetWarningLevel(WarningLevel)
                        .SetVerbosity(getDotNetVerbosity())
                        .SetConfiguration(Configuration)
                        .SetProperty("Environment", Environment.ToString())
                        .EnableNoRestore()
                        .SetNoIncremental(true)
                        .CombineWith(target, configurator: (x, v) => x
                            .SetProjectFile(v.project.Path)
                        ));
                }
                else
                {
                    target = from item in target
                        where (item.framework.Contains("ios", StringComparison.OrdinalIgnoreCase) && Platform.Contains("iPhone", StringComparison.InvariantCultureIgnoreCase)) ||
                              item.framework.Contains("android", StringComparison.OrdinalIgnoreCase) && Platform.Contains("Android", StringComparison.InvariantCultureIgnoreCase)
                        select new { item.framework, item.project };

                    DotNetBuild(s => s
                        .SetProcessWorkingDirectory(Solution.Directory)
                        .SetWarningLevel(WarningLevel)
                        .SetVerbosity(getDotNetVerbosity())
                        .SetConfiguration(Configuration)
                        .SetProperty("Environment", Environment.ToString())
                        .EnableNoRestore()
                        .SetNoIncremental(true)
                        .CombineWith(target, configurator: (x, v) => x
                            .SetProjectFile(v.project.Path)
                            .When(_ => v.framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("ArchiveOnBuild", true))
                            .When(_ => v.framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "ios-arm64"))
                            .When(_ => v.framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("AndroidPackageFormats", _androidExt))
                            .When(_ => v.framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "android-arm64"))
                        ));
                }

                DotNetToolInstall(s => s
                    .SetPackageName("dotnet-coverage")
                    .SetGlobal(true)
                );

                var coverageProcess = new ProcessStartInfo("dotnet-coverage",
                    $"collect \"dotnet test {Solution.Path} --logger \"\"trx\"\" --collect:\"\"XPlat Code Coverage\"\" --results-directory \"\"{TestResultsDirectory}\"\" --verbosity normal --no-build --no-restore\" -f xml -o \"{(CoverageDirectory / "coverage.xml")}\"")
                {
                    WorkingDirectory = RootDirectory
                };
                Process.Start(coverageProcess)?.WaitForExit();

                var mergeProcess = new ProcessStartInfo("dotnet-coverage", "merge coverage/**/coverage.cobertura.xml --output coverage/merged.coverage.xml --output-format xml")
                {
                    WorkingDirectory = RootDirectory
                };
                Process.Start(mergeProcess)?.WaitForExit();

                SonarScannerTasks.SonarScannerEnd(s => s
                    .SetToken(lint.sonarQubeToken)
                );
            }
            else
            {
                Log.Warning("SonarQube Lint file not found: {LintFile}", lintFile);
            }
#endif
        });

    private sealed record PublishProjectRecord(Project project, bool useMaui);

    private PublishProjectRecord[] loadPublishProjects()
    {
        var projectsNames = !string.IsNullOrWhiteSpace(Project) ? _projects[Project] : _projects.SelectMany(m => m.Value).ToArray();
        projectsNames = projectsNames.Where(m => !m.StartsWith('_') && !m.Contains("Test")).ToArray();
        var items = Solution.AllProjects
            .Where(m => projectsNames.Contains(m.Name))
            .ToArray();
        var projects = items
            .Select(m => new { Project = m, Info = m.GetMSBuildProject() })
            .ToArray();
        var result = new List<PublishProjectRecord>();

        foreach (var item in projects)
        {
            var options = item.Info.Imports.FirstOrDefault(m => m.ImportedProject.EscapedFullPath.EndsWith("Options.props"));
            if (options.ImportedProject != null)
            {
                // Variables
                var pg1 = options.ImportedProject.PropertyGroups.FirstOrDefault(m => m.Label == EnvironmentProperty);
                if (pg1 != null)
                {
                    pg1.SetProperty(EnvironmentProperty, Environment);
                    pg1.SetProperty(PlatformProperty, Platform);
                }
                else
                {
                    options.ImportedProject.AddProperty(EnvironmentProperty, Environment);
                    options.ImportedProject.AddProperty(PlatformProperty, Platform);
                }

                if (options.ImportedProject.HasUnsavedChanges)
                {
                    options.ImportedProject.Save();
                }

                options.ImportedProject.Reload();
            }
            else
            {
                item.Info.SetProperty(EnvironmentProperty, Environment);
                item.Info.SetProperty(PlatformProperty, Platform);
                item.Info.Save();
            }

            item.Info.ReevaluateIfNecessary();
            var useMaui = item.Info.GetPropertyValue("UseMaui");
            result.Add(new PublishProjectRecord(item.Project, !string.IsNullOrWhiteSpace(useMaui) && bool.Parse(useMaui)));
        }

        return result.ToArray();
    }

#if USING_SONARQUBE
    private sealed record SonarLint(string sonarQubeUri, string sonarQubeToken, string projectKey);
#endif

    private (bool hasWeb, bool hasService, bool hasMobile, bool hasDesktop, bool hasPackage)
        GetProjectPresenceFlags(string[] projects) =>
        (_webProjects.Any(m => projects.Contains(m.Name)),
            _serviceProjects.Any(m => projects.Contains(m.Name)),
            _mobileProjects.Any(m => projects.Contains(m.Name)),
            _desktopProjects.Any(m => projects.Contains(m.Name)),
            _packageProjects.Any(m => projects.Contains(m.Name)));

    private static DotNetVerbosity getDotNetVerbosity()
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

    private static void AssertCoverageThresholds(AbsolutePath coberturaReport)
    {
        if (!File.Exists(coberturaReport))
        {
            throw new InvalidOperationException($"Coverage report not found: {coberturaReport}");
        }

        var root = XDocument.Load(coberturaReport).Root;
        if (root?.Name.LocalName != "coverage")
        {
            throw new InvalidOperationException($"Unsupported coverage report format. Expected Cobertura XML at: {coberturaReport}");
        }

        var lineCoverage = ReadCoverageRate(root, "line-rate")
                           ?? throw new InvalidOperationException($"Line coverage rate not found in: {coberturaReport}");
        Log.Information("Line coverage: {LineCoverage:0.##}% (threshold: {LineThreshold:0.##}%)", lineCoverage, CoverageLineThreshold);
        if (lineCoverage < CoverageLineThreshold)
        {
            throw new InvalidOperationException($"Line coverage {lineCoverage:0.##}% is below threshold {CoverageLineThreshold:0.##}%");
        }

        var branchCoverage = ReadCoverageRate(root, "branch-rate");
        if (!branchCoverage.HasValue)
        {
            Log.Warning("Branch coverage rate not found; branch threshold skipped.");
            return;
        }

        Log.Information("Branch coverage: {BranchCoverage:0.##}% (threshold: {BranchThreshold:0.##}%)", branchCoverage.Value, CoverageBranchThreshold);
        if (branchCoverage.Value < CoverageBranchThreshold)
        {
            throw new InvalidOperationException($"Branch coverage {branchCoverage.Value:0.##}% is below threshold {CoverageBranchThreshold:0.##}%");
        }
    }

    private static double? ReadCoverageRate(XElement coverage, string attributeName)
    {
        var value = (string)coverage.Attribute(attributeName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var rate = double.Parse(value, CultureInfo.InvariantCulture);
        return rate <= 1 ? rate * 100 : rate;
    }

    private string getReleaseNotes()
    {
        var gitOutput = GitTasks.Git("log -1 --pretty=%B");

        var releaseNotes = new List<string> { $"Environment: {Environment}", System.Environment.NewLine, "Release Notes:", System.Environment.NewLine };
        releaseNotes.AddRange(gitOutput.Where(x => !string.IsNullOrWhiteSpace(x.Text)).Select(x => x.Text).ToList());

        return string.Join(System.Environment.NewLine, releaseNotes);
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
#pragma warning restore CA1050 // Declare types in namespaces
#pragma warning restore IDE1006 // Naming Styles
