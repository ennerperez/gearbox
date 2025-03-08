using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Types;
using Microsoft.Extensions.Logging;
using Notification = Gearbox.Core.Models.Notification;

namespace Gearbox.Core.Natives.MacOS
{
  // ReSharper disable once InconsistentNaming
  [SupportedOSPlatform("macOS")]
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
#if MACOS
      var windowInfo = QuartzCore.CGWindowListCopyWindowInfo(CGWindowListOption.OnScreenOnly, 0);
      var values = (NSArray)Runtime.GetNSObject<NSArray>(windowInfo);

      var windowList = new List<QuartzCore.kCGWindow>();
      for (ulong i = 0, len = values.Count; i < len; i++)
      {
        var window = Runtime.GetNSObject(values.ValueAt(i));
        var item = new QuartzCore.kCGWindow();
        item.Read(window);
        windowList.Add(item);
      }
#endif
      throw new System.NotImplementedException();
    }

    public RegisterStatus GetRegisterStatus()
    {
      throw new System.NotImplementedException();
    }
    public Task RegisterAsync()
    {
      _logger.LogInformation("Registering...");
      
      OpenSettings();

      _logger.LogInformation($"Please set {Metadata.Name} as the default browser in Settings.");
      _notificationService.Show(new Notification("Registered as a browser.", $"Please set {Metadata.Name} as the default browser in Settings."));
      return Task.CompletedTask;
    }
    
    public Task UnregisterAsync()
    {
      throw new System.NotImplementedException();
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
    
    public void OpenSettings() => Process.Start(new ProcessStartInfo { FileName = "x-apple.systempreferences:com.apple.Desktop-Settings.extension", UseShellExecute = true });


  }
}
