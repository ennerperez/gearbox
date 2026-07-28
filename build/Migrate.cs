#if USING_DATABASE_PROVIDER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ConfigurationSubstitution;
using Microsoft.Extensions.Configuration;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.EntityFramework;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable UsageOfDefaultStructEquality
// ReSharper disable UnusedMember.Local
public partial class Build
{
    [Parameter("The project is also known as the target project because it's where the commands add or remove files.")]
    public readonly string[] TargetProjects;

    [Parameter("The startup project is the one that the tools build and run.")]
    public readonly string StartupProject;

    [Parameter("The migration prefix is used to identify the migration files.")]
    public readonly string MigrationPrefix = "M";

    Project[] Targets => Solution.AllProjects.Where(m => TargetProjects.Contains(m.Name)).ToArray();
    Project Startup => Solution.AllProjects.FirstOrDefault(m => m.Name == StartupProject);

    static string MigrationsPath => Path.Combine("Databases", "Migrations");

    static string ScriptsPath => Path.Combine("Databases", "Scripts");

    IEnumerable<ConnectionStringRecord> GetConnectionStringsCombinations()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Startup.Directory, "appsettings.json"), false, true)
            .AddJsonFile(Path.Combine(Startup.Directory, $"appsettings.{Environment}.json"), true, true)
            .AddEnvironmentVariables()
            .EnableSubstitutions("${", "}")
            .Build();

        var connectionStrings = new Dictionary<string, string>();
        config.Bind(key: "ConnectionStrings", connectionStrings);

        var combinations = from item in connectionStrings
            select GetConnectionStringRecord(item.Key, item.Value);

        return combinations;
    }

    Target FastCompile => d => d
        .DependsOn(Restore)
        .Executes(() =>
        {
            var projects = Solution.AllProjects
                .Where(m => !m.Name.StartsWith('.'))
                .Where(m => Targets.Concat([Startup]).Contains(m))
                .ToArray();
            DotNetBuild(s => s
                .SetWarningLevel(0)
                .CombineWith(projects, configurator: (buildSettings, v) => buildSettings
                    .SetProjectFile(v)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()));
        });

    Target MigrationAdd => d => d
        .DependsOn(FastCompile)
        .Executes(() =>
        {
            var combinations = GetConnectionStringsCombinations()
                .Where(IsDbContextRecord);
            var name = $"{MigrationPrefix}{DateTime.Now.Ticks}";
            foreach (var item in combinations)
            {
                var targets = Targets.Where(m => m.Name.EndsWith(item.Provider)).ToArray();
                if (targets.Length == 0) { continue; }

                EntityFrameworkTasks.EntityFrameworkMigrationsAdd(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableNoBuild()
                    .SetStartupProject(Startup)
                    .SetName(name)
                    .SetContext(item.Name)
                    .SetOutputDirectory(Path.Combine(MigrationsPath, item.Path))
                    .CombineWith(targets, configurator: (buildSettings, v) => buildSettings
                        .SetProject(v)
                    )
                );
            }
        });

    Target MigrationRemove => d => d
        .DependsOn(FastCompile)
        .Executes(() =>
        {
            var combinations = GetConnectionStringsCombinations()
                .Where(IsDbContextRecord);

            foreach (var item in combinations)
            {
                var targets = Targets.Where(m => m.Name.EndsWith(item.Provider)).ToArray();
                if (targets.Length == 0) { continue; }

                EntityFrameworkTasks.EntityFrameworkMigrationsRemove(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableNoBuild()
                    .SetStartupProject(Startup)
                    .SetContext(item.Name)
                    .CombineWith(targets, configurator: (buildSettings, v) => buildSettings
                        .SetProject(v)
                    )
                );
            }
        });

    Target MigrationOutput => d => d
        .DependsOn(FastCompile)
        .Executes(() =>
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
                        .EnableNoBuild()
                        .SetStartupProject(Startup)
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
                        .EnableNoBuild()
                        .SetStartupProject(Startup)
                        .SetContext(item.Name)
                        .SetConnection($"Data Source={databaseFile}")
                        .CombineWith(targets, configurator: (buildSettings, v) => buildSettings
                            .SetProject(v)
                        )
                    );
                }
            }
        });

    Target DatabaseUpdate => d => d
        .DependsOn(FastCompile)
        .Executes(() =>
        {
            var combinations = GetConnectionStringsCombinations()
                .Where(IsDbContextRecord);
            foreach (var item in combinations)
            {
                var targets = Targets.Where(m => m.Name.EndsWith(item.Provider ?? string.Empty)).ToArray();
                if (targets.Length == 0) { continue; }

                EntityFrameworkTasks.EntityFrameworkDatabaseUpdate(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableNoBuild()
                    .SetStartupProject(Startup)
                    .SetContext(item.Name)
                    .CombineWith(targets, configurator: (buildSettings, v) => buildSettings
                        .SetProject(v)
                    )
                );
            }
        });

    Target DatabaseClear => d => d
        .DependsOn(FastCompile)
        .Executes(() =>
        {
            var combinations = GetConnectionStringsCombinations()
                .Where(IsDbContextRecord);
            foreach (var item in combinations)
            {
                var targets = Targets.Where(m => m.Name.EndsWith(item.Provider)).ToArray();
                if (targets.Length == 0) { continue; }

                EntityFrameworkTasks.EntityFrameworkDatabaseDrop(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableNoBuild()
                    .EnableForce()
                    .SetStartupProject(Startup)
                    .SetContext(item.Name)
                    .CombineWith(targets, configurator: (buildSettings, v) => buildSettings
                        .SetProject(v)
                    )
                );
            }
        });

    Target DatabaseRollback => d => d
        .DependsOn(FastCompile)
        .Executes(() =>
        {
            var combinations = GetConnectionStringsCombinations()
                .Where(IsDbContextRecord);
            foreach (var item in combinations.Select(m => m.Name))
            {
                foreach (var target in Targets)
                {
                    var migrations = EntityFrameworkTasks.EntityFrameworkMigrationsList(c => c
                            .SetProcessWorkingDirectory(RootDirectory)
                            .EnableNoBuild()
                            .SetStartupProject(Startup)
                            .SetContext(item)
                            .SetProject(target)
                        )
                        .Where(m => !m.Text.EndsWith("(Pending)")).ToList();

                    if (migrations.Count == 0)
                    {
                        continue;
                    }

                    var lastIndex = migrations.IndexOf(migrations[^1]);
                    lastIndex--;
                    if (lastIndex < 0)
                    {
                        continue;
                    }

                    var lastMigration = migrations[lastIndex].Text;
                    EntityFrameworkTasks.EntityFrameworkDatabaseUpdate(c => c
                        .SetProcessWorkingDirectory(RootDirectory)
                        .EnableNoBuild()
                        .SetStartupProject(Startup)
                        .SetContext(item)
                        .SetMigration(lastMigration)
                        .SetProject(target)
                    );
                }
            }
        });

    internal record ConnectionStringRecord
    {
        public string Name { get; set; }
        public string Tenant { get; set; }
        public string Provider { get; set; }

        public string Path => Regex.Replace(Name, "Context$", string.Empty, RegexOptions.IgnoreCase);
        public string Value { get; set; }
    }

    private static bool IsDbContextRecord(ConnectionStringRecord s) =>
        !string.IsNullOrWhiteSpace(s?.Name) &&
        s.Name.Contains("Context", StringComparison.InvariantCultureIgnoreCase);

    private static ConnectionStringRecord GetConnectionStringRecord(string key, string value)
    {
        var result = new ConnectionStringRecord();
        var mt = MultiTenantCsRegex().Match(key.Trim());
        if (mt.Success)
        {
            result.Name = mt.Groups[1].Value.Trim();
            result.Tenant = mt.Groups[2].Value.Trim();
            result.Provider = mt.Groups[3].Value.Trim();
            result.Value = value;
            return result;
        }

        var pv = ProviderCsRegex().Match(key.Trim());
        if (!pv.Success)
        {
            return new ConnectionStringRecord()
            {
                Name = key.Trim(),
                Value = value
            };
        }

        result.Name = pv.Groups[1].Value.Trim();
        result.Provider = pv.Groups[2].Value.Trim();
        result.Value = value;
        return result;
    }

    [GeneratedRegex(@"(.*)\[(.*)\]\.?(\w+)?", RegexOptions.Compiled)]
    private static partial Regex MultiTenantCsRegex();

    [GeneratedRegex(@"(.*)\.(\w+)", RegexOptions.Compiled)]
    private static partial Regex ProviderCsRegex();
}
#endif
