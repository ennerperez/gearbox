using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Gearbox.Core;
using Gearbox.Runner.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OS=System.Runtime.OperatingSystemExtensions;
#if DEBUG
using Serilog;
using ILogger=Serilog.ILogger;
#else
using Microsoft.Extensions.Logging;
#endif

namespace Gearbox.Runner
{

  public static partial class Program
  {

    #region Metadata

    [GeneratedRegex(@"v?\=?((?:[0-9]{1,}\.{0,}){1,})\-?(.*)?\+(.*)?", RegexOptions.Compiled)]
    private static partial Regex VersionRegex();

    private static void ReadMetadata()
    {
      Metadata.Name = Assembly.GetAssembly(typeof(Program)).Product();
      Metadata.Description = Assembly.GetAssembly(typeof(Program)).Description();
      Metadata.Assembly = AppContext.BaseDirectory;

      var informationalVersion = Assembly.GetAssembly(typeof(Program)).InformationalVersion();
      if (informationalVersion != null)
      {
        var versionMatch = VersionRegex().Match(informationalVersion);
        if (versionMatch.Success)
        {
          Metadata.Version = Version.Parse(versionMatch.Groups[1].Value);
          Metadata.Tag = versionMatch.Groups[2].Value;
          Metadata.Commit = versionMatch.Groups[3].Value.Substring(0, 7);
          Metadata.DisplayVersion = string.Join("-", Metadata.Version.ToString(3), Metadata.Tag);
        }
      }
      Metadata.Environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    }

    #endregion

    public static ILogger? Logger { get; private set; }

    public static IConfiguration? Configuration { get; private set; }

    public static string QueueFile => Path.Combine(OS.GetDataDir(), $"{Metadata.Name ?? "Gearbox"}.queue");
    

    [STAThread]
    public static void Main(string[] args)
    {
      ReadMetadata();
      
      var isCommand = Worker.Commands.Join(args, s => s, s => s, (s1, s2) => s1 == s2).Any(m => m);
      if (isCommand && File.Exists(QueueFile)) { File.Delete(QueueFile); }
      File.AppendAllLines(QueueFile, [string.Join(" ", args)]);

#if DEBUG
      // Initialize Logger
      var loggerConfiguration = new LoggerConfiguration()
        .WriteTo.Async(a =>
        {
          a.File(
            path: Path.Combine(OS.GetDataDir(), "Logs/.log"),
            rollingInterval: RollingInterval.Day,
            flushToDiskInterval: TimeSpan.FromSeconds(30),
            shared: true
          );
        })
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .Enrich.WithProcessName()
        .Enrich.WithThreadId()
        .Enrich.WithThreadName()
        .Enrich.WithProperty("ApplicationName", Metadata.Name);

      // Initialize Logger
      Logger = Log.Logger = loggerConfiguration
        .WriteTo.Trace()
        .CreateLogger();
#endif

      System.Runtime.Exceptions.UnhandledException += (_, e) =>
      {
        var ex = e.ExceptionObject as Exception;
#if DEBUG
        Logger.Fatal(ex, "{Message}", ex?.Message);
#else
        Logger?.LogCritical(ex, "{Message}", ex?.Message);
#endif
      };
      
      var mutex = new Mutex(true, Metadata.Name, out var result);
      if (!result && OperatingSystem.IsWindows())
      {
#if DEBUG
        Logger.Information("Running instance detected, exiting...");
#else
        Logger?.LogInformation("Running instance detected, exiting...");
#endif
        return;
      }

      try
      {
        var builder = BuildRunnerApp(args);
        IsRunning = true;

        var host = builder.Build();
        if (isCommand || !OperatingSystem.IsWindows())
        {
          host.StartAsync();
          return;
        }
        host.Run();

      }
      catch (Exception e)
      {
#if DEBUG
        Logger.Fatal(e, "{Message}", e.Message);
#else
        Logger?.LogCritical(e, "{Message}", e?.Message);
#endif
      }
      finally
      {
        IsRunning = false;
      }

      if (OperatingSystem.IsWindows())
      {
        GC.KeepAlive(mutex);
      }

    }
    private static HostApplicationBuilder BuildRunnerApp(string[] args)
    {
      var builder = Host.CreateApplicationBuilder(args);

      var assemblyPath = Path.GetDirectoryName(AppContext.BaseDirectory);
      Configuration = new ConfigurationBuilder()
        .SetBasePath(assemblyPath ?? Directory.GetCurrentDirectory())
        .AddIniFile("Config.ini")
        .AddIniFile($"Config.{OS.GetName()}.ini", true)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

      // Register all the services needed for the application to run
      builder.Services.AddSingleton(Configuration);
#if DEBUG
      builder.Services.AddLogging(c => c.AddSerilog(Logger, true));
#endif

      // Core Services
      builder.Services.AddCore()
        .AddRunner();

      builder.Services.AddHostedService<Worker>();

      return builder;
    }
    internal static bool IsRunning { get; private set; }

  }
}
