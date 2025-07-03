using CommunityToolkit.Mvvm.ComponentModel;

namespace Gearbox.Shell.ViewModels
{
    public partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;
        private bool IsNotBusy => !IsBusy;
    }
}
