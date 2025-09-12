using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gearbox.Shell.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Gearbox.Shell.Views
{
    public partial class MainWindow : Window
    {
#if DEBUG
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        [ActivatorUtilitiesConstructor]
        public MainWindow()
        {
            InitializeComponent();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#endif
        public MainWindow(MainWindowViewModel model)
        {
            model.Owner = this;
            DataContext = model;

            PointerMoved += InputElement_OnPointerMoved;
            PointerPressed += InputElement_OnPointerPressed;
            PointerReleased += InputElement_OnPointerReleased;

            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            if (DataContext != null && DataContext is ViewModelBase vm)
            {
                vm.OnAppearing(this, EventArgs.Empty);
                vm.OnAppearingAsync(this, EventArgs.Empty).ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            }

            base.Show();
        }

        private void InputElement_OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
            {
                return;
            }

            var currentOffset = scrollViewer.Offset;
            const double scrollSpeedMultiplier = 50.0;
            var newOffsetX = currentOffset.X - (e.Delta.Y * scrollSpeedMultiplier);
            newOffsetX = Math.Max(0, Math.Min(newOffsetX, scrollViewer.ScrollBarMaximum.X));
            scrollViewer.Offset = new Vector(newOffsetX, currentOffset.Y);
            e.Handled = true;
        }

        private bool _mouseDownForWindowMoving;
        private PointerPoint _originalPoint;

        private void InputElement_OnPointerMoved(object sender, PointerEventArgs e)
        {
            if (!_mouseDownForWindowMoving) { return; }

            var currentPoint = e.GetCurrentPoint(this);
            Position = new PixelPoint(Position.X + (int)(currentPoint.Position.X - _originalPoint.Position.X),
                Position.Y + (int)(currentPoint.Position.Y - _originalPoint.Position.Y));
        }

        private void InputElement_OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen) { return; }

            _mouseDownForWindowMoving = true;
            _originalPoint = e.GetCurrentPoint(this);
        }

        private void InputElement_OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _mouseDownForWindowMoving = false;
        }
        public override void Show()
        {
            if (Screens.Primary != null)
            {

                //Width = Screens.Primary.Bounds.Width / 2.0;
                //Height = Screens.Primary.Bounds.Height / 2.0;
                var x = (int)(Screens.Primary.Bounds.Width / 2.0 - Width / 2);
                var y = (int)(Screens.Primary.Bounds.Height / 2.0 - Height / 2);
                Position = new PixelPoint(x, y);
            }
            base.Show();
        }

    }
}
