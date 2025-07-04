using Avalonia.Controls;
using Gearbox.Shell.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Shell.Views
{
    public partial class AboutWindow : Window
    {
#if DEBUG
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        [ActivatorUtilitiesConstructor]
        public AboutWindow()
        {
            InitializeComponent();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#endif
        public AboutWindow(AboutWindowViewModel model)
        {
            model.Owner = this;
            DataContext = model;
            InitializeComponent();
        }
    }
}
