using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;
using Gearbox.Core.Types;
using Gearbox.Shell.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Shell.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IBackend _backend;
        private readonly IBrowserService _browserService;
        private readonly INotificationService _notificationService;

#if DEBUG
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        [ActivatorUtilitiesConstructor]
        public MainWindowViewModel()
        {
            Url = "https://www.google.com";
            Browsers = new List<IBrowser>([
                new Browser() { Path = "Opera", Name = "Opera", Icon = "/Assets/Images/Browsers/Opera.svg" },
                new Browser() { Path = "Edge", Name = "Edge", Icon = "/Assets/Images/Browsers/Edge.svg" },
                new Browser() { Path = "Firefox", Name = "Firefox", Icon = "/Assets/Images/Browsers/Firefox.svg" },
                new Browser() { Path = "Chrome", Name = "Chrome", Icon = "/Assets/Images/Browsers/Chrome.svg" },
                new Browser() { Path = "Brave", Name = "Brave", Icon = "/Assets/Images/Browsers/Brave.svg" },
                new Browser() { Path = "Chromium", Name = "Chromium", Icon = "/Assets/Images/Browsers/Chromium.svg" },
            ]);
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#endif
        public MainWindowViewModel(IBackend backend, IBrowserService browserService, INotificationService notificationService)
        {
            _backend = backend;
            _browserService = browserService;
            _notificationService = notificationService;
        }

        public override async Task OnAppearingAsync(object sender, EventArgs e)
        {
            if (Design.IsDesignMode) { return; }

            Browsers = _browserService.GetBrowsers<Browser>().Values.Where(m => m.IsInstalled);
            var status = _backend.GetRegisterStatus();
            if (status != RegisterStatus.Registered)
            {
                await _notificationService.ShowAsync(new Notification("Default browser", "Gearbox is not registered as default browser")
                {
                    Expiration = TimeSpan.FromMinutes(1),
                    OnClick = async void () =>
                    {
                        await _backend.RegisterAsync();
                    }
                });
            }

        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public void Exit()
        {
            Environment.Exit(0);
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public void Settings()
        {
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public void About()
        {
            var activeWindow = (Application.Current as App)?.ActiveWindow;
            if (activeWindow != null)
            {
                IsBusy = true;
                Program.Services?.GetService<AboutWindow>()?.Show(activeWindow);
            }

            IsBusy = false;
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public Task Register()
        {
            _backend.RegisterAsync();
            return Task.CompletedTask;
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public async Task InvokeBrowser(IBrowser browser)
        {
            if (Url != null)
            {
                IsBusy = true;
                Owner?.Hide();
                await _browserService.LaunchAsync(browser, new Uri(Url));
                IsBusy = false;
            }

            Environment.Exit(0);
        }

#if !DEBUG
        [ObservableProperty]
        private string? _url;
#else
        [ObservableProperty]
        private string _url = "https://www.google.com";
#endif

        [ObservableProperty]
        private IEnumerable<IBrowser> _browsers;
    }
}
