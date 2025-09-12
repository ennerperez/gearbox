#if USING_7ZIP
using System.Text;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConfigurationSubstitution;
using DotNetEnv;
#if USING_DATABASE_PROVIDER
using Microsoft.Extensions.Configuration;
using Nuke.Common.Tools.EntityFramework;
#endif
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MauiCheck;
using Nuke.Common.Tools.ReportGenerator;
#if USING_SONARQUBE
using System.Text.Json;
using Nuke.Common.Tools.DotCover;
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
    static AbsolutePath TestResultsDirectory => ArtifactsDirectory / "test-results";
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

    GitVersion _version;

    bool _useMaui;
    string _repoUrl = string.Empty;
    string _androidExt = string.Empty;
    string _iOSExt = string.Empty;

    #endregion

    #region Projects

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
            var envs = System.Environment.GetEnvironmentVariables();
            Log.Information("Reading Environment");
            foreach (var env in envs.Keys)
            {
                Log.Information($"{env}: {envs[env]}");
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
                Env.Load(envFile);
            }
            else
            {
                Log.Warning(".env File not found");
            }

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

            _repoUrl = GitTasks.Git("config --get remote.origin.url", Repository.LocalDirectory ?? RootDirectory).StdToText();
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

            try
            {
                if (_useMaui)
                {
                    Log.Information("Checking for MAUI workload installation");
                    MauiCheckTasks.MauiCheck(c => c.SetNonInteractive(true));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{Message}", ex.Message);
                throw;
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
            Log.Information("Cleaning Output Directories");
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach((path) => path.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach((path) => path.DeleteDirectory());

            Log.Information("Cleaning Artifacts Directory");
            AbsolutePath.Create(ArtifactsDirectory).CreateOrCleanDirectory();

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
        .DependsOn(Prepare)
        .Executes(() =>
        {
            _version = GitVersionTasks.GitVersion().Result;

            if (Environment == Environment.Development)
            {
                return;
            }

            var assemblyInfoFiles = SourceDirectory.GetFiles("AssemblyInfo*.cs", int.MaxValue);
            foreach (var assemblyInfoVersionFile in assemblyInfoFiles)
            {
                Log.Information("Patching: {File}", assemblyInfoVersionFile);

                var content = File.ReadAllText(assemblyInfoVersionFile);
                content = AssemblyVersionRegex().Replace(content, $"[assembly: AssemblyVersion(\"{_version.AssemblySemVer}\")]");
                content = AssemblyFileVersionRegex().Replace(content, $"[assembly: AssemblyFileVersion(\"{_version.AssemblySemFileVer}\")]");
                content = AssemblyInformationalVersionRegex().Replace(content, $"[assembly: AssemblyInformationalVersion(\"{_version.InformationalVersion}\")]");

                File.WriteAllText(assemblyInfoVersionFile, content);
            }

            if (!_useMaui)
            {
                return;
            }

            if (Verbosity == Verbosity.Minimal)
            {
                Console.WriteLine($"// ApplicationVersion: {_version.AssemblySemVer}");
                Console.WriteLine($"// ApplicationDisplayVersion: {_version.FullSemVer}");
            }
            else
            {
                Log.Information("ApplicationVersion: {AssemblySemVer}", _version.AssemblySemVer);
                Log.Information("ApplicationDisplayVersion: {FullSemVer}", _version.FullSemVer);
            }

            var items = loadPublishProjects();
            foreach (var item in items.Where(m => m.useMaui))
            {
                var project = item.project.GetMSBuildProject();

                // Version
                project.SetProperty("ApplicationVersion", _version.AssemblySemVer);
                project.SetProperty("ApplicationDisplayVersion", _version.FullSemVer);

                project.Save(item.project.Path);
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
                        .When(w => w.Framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("ArchiveOnBuild", true))
                        .When(w => w.Framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "ios-arm64"))
                        .When(w => w.Framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("AndroidPackageFormats", _androidExt))
                        .When(w => w.Framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "android-arm64"))
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

            DotNetTest(s => s
                .SetVerbosity(getDotNetVerbosity())
                .EnableNoRestore()
                .EnableNoBuild()
                .SetResultsDirectory(TestResultsDirectory)
                .SetCollectCoverage(true)
                .SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
                .SetLoggers("trx", "html")
                .SetDataCollector("XPlat Code Coverage")
                .CombineWith(_testProjects, configurator: (x, v) => x
                    .SetProjectFile(v.Path)));

            ReportGeneratorTasks.ReportGenerator(s => s
                .SetReports($"{TestResultsDirectory}/**/coverage.cobertura.xml")
                .SetAssemblyFilters("+*")
                .SetFileFilters("+*")
                .SetReportTypes("cobertura;html;teamcitysummary")
                .SetTargetDirectory(TestResultsDirectory / "reports")
            );

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

            (bool hasWeb, bool hasService, bool hasMobile, bool hasDesktop, bool hasPackage) = GetProjectPresenceFlags();

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
                        .When(w => w.Framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("ArchiveOnBuild", true))
                        .When(w => w.Framework.Contains("ios", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "ios-arm64"))
                        .When(w => w.Framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("AndroidPackageFormats", _androidExt))
                        .When(w => w.Framework.Contains("android", StringComparison.OrdinalIgnoreCase), c => c.SetProperty("RuntimeIdentifier", "android-arm64"))
                        .When(w => w.Framework.Contains("android", StringComparison.OrdinalIgnoreCase) && PackageSigning, (c) => c
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
                foreach (var item in publishProjects)
                {
                    var projectInfo = Solution.AllProjects.FirstOrDefault(p => p.Name == item.project.Name);
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
                            .SetRepositoryUrl(_repoUrl)
                            .SetRepositoryType("git")
                            .SetOutputDirectory(PublishDirectory / item.project.Name));
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

            (bool hasWeb, bool hasService, bool hasMobile, bool hasDesktop, bool hasPackage) = GetProjectPresenceFlags();

#if USING_DATABASE_PROVIDER

            if (hasWeb || hasService || hasPackage)
            {
                if (Startup != null && Target != null)
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

                            EntityFrameworkTasks.EntityFrameworkMigrationsScript(c => c
                                .SetProcessWorkingDirectory(RootDirectory)
                                .EnableIdempotent()
                                .SetProject(Target)
                                .SetStartupProject(Startup)
                                .SetContext(item.Name)
                                .SetOutput(scriptFile)
                            );
                        }
                        else if (string.Equals(item.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
                        {
                            var databaseFile = (bundlesDir / $"{item.Provider}_{item.Name}_{stamp}.db");
                            databaseFile.DeleteFile();

                            EntityFrameworkTasks.EntityFrameworkDatabaseUpdate(c => c
                                .SetProcessWorkingDirectory(RootDirectory)
                                .SetProject(Target)
                                .SetStartupProject(Startup)
                                .SetContext(item.Name)
                                .SetConnection($"Data Source={databaseFile}")
                            );
                        }
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
                    var nugets = (PublishDirectory / item.project.Name / $"{packageId}.*.nupkg").GetFiles();
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
                    .SetProcessWorkingDirectory(Solution.Directory)
                    .SetProjectKey(lint.projectKey)
                    .SetToken(lint.sonarQubeToken)
                    .SetAdditionalParameter("sonar.host.url", lint.sonarQubeUri)
                    .SetAdditionalParameter("sonar.exclusions", "**/.sonarlint/*.*, **/.nuke/*.*")
                    .SetAdditionalParameter("sonar.cs.vscoveragexml.reportsPaths", CoverageDirectory / "coverage.xml")
                );

                DotNetBuild(c => c
                    .SetNoIncremental(true)
                    .SetProjectFile(Solution.Path)
                );

                DotNetToolInstall(s => s
                    .SetPackageName("dotnet-coverage")
                    .SetGlobal(true)
                );

                Process.Start("dotnet-coverage",
                    $"collect \"dotnet test {Solution.FileName} --no-build --no-restore\" -f xml -o \"{(CoverageDirectory / "coverage.xml")}\"")?.WaitForExit();

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

    private record PublishProjectRecord(Project project, bool useMaui);

    private PublishProjectRecord[] loadPublishProjects()
    {
        var items = Solution.AllProjects
            .Where(m => !m.Name.StartsWith("_"))
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
    private record SonarLint(string sonarQubeUri, string sonarQubeToken, string projectKey);
#endif

    private (bool hasWeb, bool hasService, bool hasMobile, bool hasDesktop, bool hasPackage)
        GetProjectPresenceFlags() =>
        (_webProjects?.Length > 0,
            _serviceProjects?.Length > 0,
            _mobileProjects?.Length > 0,
            _desktopProjects?.Length > 0,
            _packageProjects?.Length > 0);

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

    private string getReleaseNotes()
    {
        var gitOutput = GitTasks.Git("log -1 --pretty=%B");

        var releaseNotes = new List<string> { $"Environment: {Environment}", System.Environment.NewLine, "Release Notes:", System.Environment.NewLine };
        releaseNotes.AddRange(gitOutput.Where(x => !string.IsNullOrWhiteSpace(x.Text)).Select(x => x.Text).ToList());

        return string.Join(System.Environment.NewLine, releaseNotes);
    }

    [GeneratedRegex(@"\[assembly: AssemblyVersion\(.*\)\]", RegexOptions.Compiled)]
    private static partial Regex AssemblyVersionRegex();

    [GeneratedRegex(@"\[assembly: AssemblyFileVersion\(.*\)\]", RegexOptions.Compiled)]
    private static partial Regex AssemblyFileVersionRegex();

    [GeneratedRegex(@"\[assembly: AssemblyInformationalVersion\(.*\)\]", RegexOptions.Compiled)]
    private static partial Regex AssemblyInformationalVersionRegex();
}
#pragma warning restore CA1050 // Declare types in namespaces
#pragma warning restore IDE1006 // Naming Styles
