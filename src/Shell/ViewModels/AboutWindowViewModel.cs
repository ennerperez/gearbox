using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Shell.ViewModels
{
    public partial class AboutWindowViewModel : ViewModelBase
    {
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
            Title = Metadata.Product ?? "Gearbox";
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public void Exit()
        {
            Owner?.Close();
        }

        [RelayCommand(CanExecute = nameof(IsNotBusy))]
        public void OpenWebsite()
        {
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
    }
}
