#if USING_DATABASE_PROVIDER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ConfigurationSubstitution;
using Microsoft.Extensions.Configuration;
using Nuke.Common;
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
    public readonly string TargetProject;

    [Parameter("The startup project is the one that the tools build and run.")]
    public readonly string StartupProject;

    Project Target => Solution.AllProjects.FirstOrDefault(m => m.Name == TargetProject);
    Project Startup => Solution.AllProjects.FirstOrDefault(m => m.Name == StartupProject);

    static string MigrationsPath => "Migrations";

    static string ScriptsPath => "Scripts";

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
                .Where(m => new[]
                {
                    Target, Startup
                }.Contains(m))
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
            foreach (var item in combinations)
            {
                EntityFrameworkTasks.EntityFrameworkMigrationsAdd(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableNoBuild()
                    .SetProject(Target)
                    .SetStartupProject(Startup)
                    .SetName($"M{DateTime.Now.Ticks}")
                    .SetContext(item.Name)
                    .SetOutputDirectory(Path.Combine(MigrationsPath, string.IsNullOrWhiteSpace(item.Provider) ? string.Empty : item.Provider, item.Path))
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
                EntityFrameworkTasks.EntityFrameworkMigrationsRemove(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableNoBuild()
                    .SetProject(Target)
                    .SetStartupProject(Startup)
                    .SetContext(item.Name)
                );
            }
        });

    Target MigrationOutput => d => d
        .DependsOn(FastCompile)
        .Executes(() =>
        {
            var combinations = GetConnectionStringsCombinations()
                                .Where(IsDbContextRecord)
                                .Where(i => !string.IsNullOrWhiteSpace(i.Provider))
                                .Where(i => !string.Equals(i.Provider, "Sqlite", StringComparison.OrdinalIgnoreCase));
            foreach (var item in combinations)
            {
                var provider = string.IsNullOrWhiteSpace(item.Provider) ? "Unknown" : item.Provider;
                var fileName = Path.Combine(Target?.Directory ?? string.Empty, ScriptsPath, provider, item.Path, $"{DateTime.Now:yyyyMMdd}.sql");
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                EntityFrameworkTasks.EntityFrameworkMigrationsScript(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableIdempotent()
                    .EnableNoBuild()
                    .SetProject(Target)
                    .SetStartupProject(Startup)
                    .SetContext(item.Name)
                    .SetOutput(fileName)
                );
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
                EntityFrameworkTasks.EntityFrameworkDatabaseUpdate(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableNoBuild()
                    .SetProject(Target)
                    .SetStartupProject(Startup)
                    .SetContext(item.Name)
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
                EntityFrameworkTasks.EntityFrameworkDatabaseDrop(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableNoBuild()
                    .EnableForce()
                    .SetProject(Target)
                    .SetStartupProject(Startup)
                    .SetContext(item.Name)
                );
            }
        });

    Target DatabaseRollback => d => d
        .DependsOn(FastCompile)
        .Executes(() =>
        {
            var combinations = GetConnectionStringsCombinations()
                .Where(IsDbContextRecord);
            foreach (var item in combinations)
            {
                var migrations = EntityFrameworkTasks.EntityFrameworkMigrationsList(c => c
                    .SetProcessWorkingDirectory(RootDirectory)
                    .EnableNoBuild()
                    .SetProject(Target)
                    .SetStartupProject(Startup)
                    .SetContext(item.Name)
                ).Where(m => !m.Text.EndsWith("(Pending)")).ToList();

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
                    .SetProject(Target)
                    .SetStartupProject(Startup)
                    .SetContext(item.Name)
                    .SetMigration(lastMigration)
                );
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
