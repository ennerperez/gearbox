using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using OS = System.Runtime.OperatingSystemExtensions;

namespace Gearbox.UnitTest.Core.Services
{
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    public class BrowserServiceTest
    {
        private readonly IBrowserService _browserService;

        public BrowserServiceTest()
        {
            Assembly.GetEntryAssembly()?.ReadMetadata();

            var assemblyPath = Path.GetDirectoryName(AppContext.BaseDirectory);
            var configuration = new ConfigurationBuilder()
                .SetBasePath(assemblyPath ?? Directory.GetCurrentDirectory())
                .AddIniFile("Config.ini")
                .AddIniFile($"Config.{OS.GetName()}.ini", true)
                .AddEnvironmentVariables()
                .Build();

            var notificationService = Substitute.For<INotificationService>();
            var backend = Substitute.For<IBackend>();
            var logger = Substitute.For<ILogger<IBrowserService>>();
            _browserService = new BrowserService(notificationService, configuration, backend, logger);
        }

        [Theory]
        [InlineData("https://google.com")]
        [InlineData("https://youtube.com")]
        public async Task LaunchUrlByBrowserAsync(string url)
        {
            var processId = await _browserService.LaunchAsync(url);
            processId.ShouldBeGreaterThan(0);
        }

        [Theory]
        [InlineData("https://google.com", "Discord")]
        [InlineData("https://youtube.com", "Microsoft Teams")]
        public async Task LaunchUrlBySourceAsync(string url, string window)
        {
            var processId = await _browserService.LaunchAsync(url, window);
            processId.ShouldBeGreaterThan(0);
        }

        [Theory]
        [InlineData("https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf")]
        public async Task LaunchUrlByTypeAsync(string url)
        {
            var processId = await _browserService.LaunchAsync(url);
            processId.ShouldBeGreaterThan(0);
        }
    }
}
