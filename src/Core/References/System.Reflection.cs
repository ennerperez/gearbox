// ReSharper disable CheckNamespace

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime;
using System.Text.RegularExpressions;

// ReSharper disable UnusedAutoPropertyAccessor.Global

#pragma warning disable CS8632

namespace System.Reflection
{
    [ExcludeFromCodeCoverage]
    public static class AssemblyExtensions
    {
        private static string? s_copyright;

        public static string? Copyright(this Assembly @this)
        {
            if (string.IsNullOrWhiteSpace(s_copyright))
            {
                s_copyright = @this.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true).OfType<AssemblyCopyrightAttribute>().FirstOrDefault()?.Copyright;
            }

            return s_copyright;
        }

        private static string? s_product;

        public static string? Product(this Assembly? @this)
        {
            if (string.IsNullOrWhiteSpace(s_product))
            {
                s_product = @this?.GetCustomAttributes(typeof(AssemblyProductAttribute), true).OfType<AssemblyProductAttribute>().FirstOrDefault()?.Product;
            }

            return s_product;
        }

        private static string? s_company;

        public static string? Company(this Assembly @this)
        {
            if (string.IsNullOrWhiteSpace(s_company))
            {
                s_company = @this.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true).OfType<AssemblyCompanyAttribute>().FirstOrDefault()?.Company;
            }

            return s_company;
        }

        private static string? s_title;

        public static string? Title(this Assembly @this)
        {
            if (string.IsNullOrWhiteSpace(s_title))
            {
                s_title = @this.GetCustomAttributes(typeof(AssemblyTitleAttribute), true).OfType<AssemblyTitleAttribute>().FirstOrDefault()?.Title;
            }

            return s_title;
        }

        private static string? s_description;

        public static string? Description(this Assembly? @this)
        {
            if (string.IsNullOrWhiteSpace(s_description))
            {
                s_description = @this?.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true).OfType<AssemblyDescriptionAttribute>().FirstOrDefault()?.Description;
            }

            return s_description;
        }

        private static Version? s_version;

        public static Version? Version(this Assembly @this)
        {
            if (s_version != null)
            {
                return s_version;
            }

            var versionString = @this.GetCustomAttributes(typeof(AssemblyVersionAttribute), true).OfType<AssemblyVersionAttribute>().FirstOrDefault()?.Version;
            if (!string.IsNullOrWhiteSpace(versionString))
            {
                _ = System.Version.TryParse(versionString, out s_version);
            }
            else
            {
                s_version = @this.GetName().Version;
            }

            return s_version;
        }

        private static Version? s_fileVersion;

        public static Version? FileVersion(this Assembly @this)
        {
            if (s_fileVersion != null)
            {
                return s_version;
            }

            var fileVersionString = @this.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).OfType<AssemblyFileVersionAttribute>().FirstOrDefault()?.Version;
            _ = System.Version.TryParse(fileVersionString, out s_fileVersion);

            return s_version;
        }

        private static string? s_informationalVersion;

        public static string? InformationalVersion(this Assembly? @this)
        {
            if (string.IsNullOrWhiteSpace(s_informationalVersion))
            {
                s_informationalVersion = @this?.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true).OfType<AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion;
            }

            return s_informationalVersion;
        }
    }

    [ExcludeFromCodeCoverage]
    public static partial class AssemblyMetadata
    {
        #region Metadata

        public static string? Product { get; internal set; }
        public static string? DisplayVersion { get; private set; }
        public static Version? Version { get; private set; }
        public static string? Commit { get; private set; }
        public static string? Tag { get; private set; }
        public static string? Environment { get; private set; }
        public static string? Assembly { get; private set; }
        public static string? Description { get; private set; }

        [GeneratedRegex(@"v?\=?((?:[0-9]{1,}\.{0,}){1,})\-?(.*)?\+(.*)", RegexOptions.Compiled)]
        private static partial Regex InformationalVersionRegex();

        internal static void Read(Assembly assembly, string variableName = "DOTNET_ENVIRONMENT")
        {
            Product = assembly.Product();
            Description = assembly.Description();
            Assembly = assembly.Location;

            var informationalVersion = assembly.InformationalVersion();
            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                var versionMatch = InformationalVersionRegex().Match(informationalVersion);
                if (versionMatch.Success)
                {
                    Version = Version.Parse(versionMatch.Groups[1].Value);
                    Tag = versionMatch.Groups[2].Value;
                    Commit = versionMatch.Groups[3].Value[..7];
                    DisplayVersion = string.Join("-", new[] { Version.ToString(3), Tag }.Where(m => !string.IsNullOrWhiteSpace(m)));
                }
            }

            var environmentName = System.Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                Environment = environmentName;
            }
            else
            {
                Environment =
#if DEVELOPMENT
                    Environments.Development;
#elif TEST
                    Environments.Test;
#elif STAGING
                    Environments.Staging;
#else
                    Environments.Production;
#endif
                System.Environment.SetEnvironmentVariable(variableName, Environment);
            }
        }

        public static void ReadMetadata(this Assembly assembly, string variableName = "DOTNET_ENVIRONMENT")
        {
            Read(assembly, variableName);
        }

        #endregion
    }
}
