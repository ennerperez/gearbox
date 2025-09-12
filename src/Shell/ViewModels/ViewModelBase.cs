using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Gearbox.Shell.ViewModels
{
    public abstract partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        public bool IsNotBusy => !IsBusy;

        public bool IsDesignMode => Design.IsDesignMode;
        public bool IsNotDesignMode => !Design.IsDesignMode;

        public virtual void OnAppearing(object sender, EventArgs e)
        {
        }

        public virtual Task OnAppearingAsync(object sender, EventArgs e)
        {
            return Task.CompletedTask;
        }

        [ObservableProperty]
        private bool _isVisible = true;

        public Window Owner { get; set; }

    }
}
