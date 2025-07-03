using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gearbox.Core.Interfaces;
using Gearbox.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Shell.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IBrowserService? _browserService;

        [ActivatorUtilitiesConstructor]
        public MainWindowViewModel()
        {
            _browsers = new List<IBrowser>([
                new Browser(){Path = "Opera", Name = "Opera", Icon = "/Assets/Icons/Browsers/Opera.svg"},
                new Browser(){Path = "Edge", Name = "Edge", Icon = "/Assets/Icons/Browsers/Edge.svg"},
                new Browser(){Path = "Firefox", Name = "Firefox", Icon = "/Assets/Icons/Browsers/Firefox.svg"},
                new Browser(){Path = "Chrome", Name = "Chrome", Icon = "/Assets/Icons/Browsers/Chrome.svg"},
                new Browser(){Path = "Brave", Name = "Brave", Icon = "/Assets/Icons/Browsers/Brave.svg"},
                new Browser(){Path = "Chromium", Name = "Chromium", Icon = "/Assets/Icons/Browsers/Chromium.svg"},
            ]);
        }
        public MainWindowViewModel(IBrowserService? browserService) : this()
        {
            _browserService = browserService;
        }

        [RelayCommand]
        public void Exit()
        {
            Environment.Exit(0);
        }

        [RelayCommand]
        public void Settings()
        {
        }

        [RelayCommand]
        public void About()
        {
        }

        [ObservableProperty]
        private string? _url;

        [ObservableProperty]
        private IEnumerable<IBrowser> _browsers;
    }
}
