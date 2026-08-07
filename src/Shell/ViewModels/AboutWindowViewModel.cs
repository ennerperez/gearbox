using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using OS = System.Runtime.OperatingSystemExtensions;

namespace Gearbox.Shell.ViewModels
{
    public partial class AboutWindowViewModel : ViewModelBase
    {
        private const string FallbackWebsiteUrl = "https://github.com/ennerperez/gearbox";

#if DEBUG
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        [ActivatorUtilitiesConstructor]
        public AboutWindowViewModel()
        {
            InitializeComponent();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#endif
        private void InitializeComponent()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var metadata = assembly.ReadMetadata();

            Title = metadata.Product ?? assembly.GetName().Name ?? "Gearbox";
            Description = metadata.Description ?? string.Empty;
            Version = metadata.DisplayVersion ?? assembly.InformationalVersion() ?? assembly.GetName().Version?.ToString() ?? string.Empty;
            Copyright = assembly.Copyright() ?? GetMetadataValue(assembly, "Copyright") ?? $"Copyright (c) {DateTime.Now.Year}";
            License = GetMetadataValue(assembly, "PackageLicenseExpression") ?? GetMetadataValue(assembly, "License") ?? "MIT";
            WebsiteUrl = NormalizeWebsiteUrl(
                GetMetadataValue(assembly, "PackageProjectUrl") ??
                GetMetadataValue(assembly, "RepositoryUrl") ??
                FallbackWebsiteUrl);
            Tagline = "Cross-platform rule-based browser launcher";
            SystemPurpose = "Gearbox routes links to the right browser using URL, file type, and source-window rules.";
            OperatingSystem = $"{OS.GetName()} {RuntimeInformation.OSDescription}".Trim();
            Runtime = RuntimeInformation.FrameworkDescription;
            Architecture = $"{RuntimeInformation.OSArchitecture} OS / {RuntimeInformation.ProcessArchitecture} process";
            DataDirectory = OS.GetDataDir();
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public void Exit()
        {
            Owner?.Close();
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public void OpenWebsite()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = WebsiteUrl,
                UseShellExecute = true
            });
        }

        private static string GetMetadataValue(Assembly assembly, string key)
        {
            foreach (var attribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
            {
                if (string.Equals(attribute.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return string.IsNullOrWhiteSpace(attribute.Value) ? null : attribute.Value;
                }
            }

            return null;
        }

        private static string NormalizeWebsiteUrl(string websiteUrl)
        {
            if (string.IsNullOrWhiteSpace(websiteUrl))
            {
                return FallbackWebsiteUrl;
            }

            return websiteUrl.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? websiteUrl[..^4] : websiteUrl;
        }

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private string _version;

        [ObservableProperty]
        private string _copyright;

        [ObservableProperty]
        private string _license;

        [ObservableProperty]
        private string _websiteUrl = FallbackWebsiteUrl;

        [ObservableProperty]
        private string _tagline;

        [ObservableProperty]
        private string _systemPurpose;

        [ObservableProperty]
        private string _operatingSystem;

        [ObservableProperty]
        private string _runtime;

        [ObservableProperty]
        private string _architecture;

        [ObservableProperty]
        private string _dataDirectory;
    }
}
