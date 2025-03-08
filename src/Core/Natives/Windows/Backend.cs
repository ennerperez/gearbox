using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Natives.Windows.Interop;
using Gearbox.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Notification=Gearbox.Core.Models.Notification;

namespace Gearbox.Core.Natives.Windows
{

  [SupportedOSPlatform("windows")]
  public class Backend : IBackend
  {

    private readonly INotificationService _notificationService;
    private readonly ILogger<Backend> _logger;

    public Backend(INotificationService notificationService, ILogger<Backend> logger)
    {
      _notificationService = notificationService;
      _logger = logger;
    }

    public string GetActiveWindowTitle()
    {
      var result = string.Empty;
      const int NChars = 256;
      var buff = new StringBuilder(NChars);
      var handle = User32.GetForegroundWindow();

      if (User32.GetWindowText(handle, buff, NChars) > 0)
      {
        result = buff.ToString();
      }
      return result;
    }
    
    public RegisterStatus GetRegisterStatus()
    {
      throw new NotImplementedException();
    }
    
    public Task RegisterAsync()
    {
      _logger.LogInformation("Registering...");

      var appReg = Registry.CurrentUser.CreateSubKey(AppKey);
      RegisterCapabilities(appReg);

      _registerKey?.SetValue(Metadata.Name, CapabilityKey);

      HandleUrls();
      OpenSettings();

      _logger.LogInformation($"Please set {Metadata.Name} as the default browser in Settings.");
      _notificationService.Show(new Notification("Registered as a browser.", $"Please set {Metadata.Name} as the default browser in Settings."));
      return Task.CompletedTask;
    }
    
    private void HandleUrls()
    {
      var handlerReg = Registry.CurrentUser.CreateSubKey(UrlKey);
      // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
      if (handlerReg == null)
      {
        return;
      }
      handlerReg.SetValue(string.Empty, Metadata.Name ?? string.Empty);
      handlerReg.SetValue("FriendlyTypeName", Metadata.Name ?? string.Empty);
      handlerReg.CreateSubKey("shell\\open\\command").SetValue("", AppOpenUrlCommand);
    }
    public Task UnregisterAsync()
    {
      throw new NotImplementedException();
    }
    public async Task RegisterOrUnregisterAsync()
    {
      var status = GetRegisterStatus();

      switch (status)
      {
        case RegisterStatus.Unregistered:
          await RegisterAsync();
          return;
        case RegisterStatus.Registered:
          await UnregisterAsync();
          return;
        case RegisterStatus.Updated:
          await UnregisterAsync();// Unregister the old path
          await RegisterAsync();// Register with the new path
          _notificationService.Show(new Notification("Updated location", $"{Metadata.Name} has been re-registered with a new path."));
          break;
      }

    }
    
    public void OpenSettings() => Process.Start(new ProcessStartInfo { FileName = $"ms-settings:defaultapps?registeredAppUser={Metadata.Name}", UseShellExecute = true });

    private string AppOpenUrlCommand => Metadata.Assembly?.Replace(".dll", ".exe") + " %1";
    private string AppKey => $"SOFTWARE\\{Metadata.Name}";
    private string UrlKey => $"SOFTWARE\\Classes\\{Metadata.Name}URL";
    private string CapabilityKey => $"SOFTWARE\\{Metadata.Name}\\Capabilities";

    private readonly RegistryKey? _registerKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\RegisteredApplications", true);
    // private RegistryKey? AppRegKey => Registry.CurrentUser.OpenSubKey(AppKey);
    // private RegistryKey? UrlRegKey => Registry.CurrentUser.OpenSubKey(UrlKey);
    private void RegisterCapabilities(RegistryKey appReg)
    {
      // Register capabilities.
      var capabilityReg = appReg.CreateSubKey("Capabilities");
      // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
      if (capabilityReg == null)
      {
        return;
      }
      capabilityReg.SetValue("ApplicationName", Metadata.Name ?? string.Empty);
      capabilityReg.SetValue("ApplicationIcon", $"{Metadata.Assembly?.Replace(".dll", ".exe")},0");
      capabilityReg.SetValue("ApplicationDescription", Metadata.Description ?? string.Empty);

      // Set up protocols we want to handle.
      var urlAssocReg = capabilityReg.CreateSubKey("URLAssociations");
      // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
      if (urlAssocReg == null)
      {
        return;
      }
      urlAssocReg.SetValue("http", Metadata.Name + "URL");
      urlAssocReg.SetValue("https", Metadata.Name + "URL");
      urlAssocReg.SetValue("ftp", Metadata.Name + "URL");
      urlAssocReg.SetValue("ftps", Metadata.Name + "URL");
    }
    
  }
}
