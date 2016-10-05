using PowerSwitcher.TrayApp.Extensions;
using PowerSwitcher.TrayApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PowerSwitcher.TrayApp
{
    ////
    //  Code heavily inspired by https://github.com/File-New-Project/EarTrumpet/blob/master/EarTrumpet/MainWindow.xaml.cs
    ////

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ((App)Application.Current).TrayApp.ShowFlyout += TrayApp_ShowFlyout;
        
            createAndHideWindow();

            // Move keyboard focus to the first element. Disabled this since it is ugly but not sure invisible
            // visuals are preferrable.
            // Activated += (s,e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.First));

            SourceInitialized += (s, e) => UpdateTheme();
        }

        private void createAndHideWindow()
        {
            // Ensure the Win32 and WPF windows are created to fix first show issues with DPI Scaling
            Opacity = 0;
            Show();
            Hide();
            Opacity = 1;
        }

        #region ShowingHiding
        void TrayApp_ShowFlyout()
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.HideWithAnimation();
            }
            else
            {
                ViewModel.Refresh();
                UpdateTheme();
                UpdateWindowPosition();
                this.ShowWithAnimation();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.HideWithAnimation();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.HideWithAnimation();
            }
        }
        #endregion

        #region ServicesUpdates
        private void UpdateTheme()
        {
            // Call UpdateTheme before UpdateWindowPosition in case sizes change with the theme.
            ThemeService.UpdateThemeResources(Resources);
            if (ThemeService.IsWindowTransparencyEnabled)
            {
                this.EnableBlur();
            }
            else
            {
                this.DisableBlur();
            }
        }

        private void UpdateWindowPosition()
        {
            LayoutRoot.UpdateLayout();
            LayoutRoot.Measure(new Size(double.PositiveInfinity, MaxHeight));
            Height = LayoutRoot.DesiredSize.Height;

            var taskbarState = TaskbarService.GetWinTaskbarState();
            switch (taskbarState.TaskbarPosition)
            {
                case TaskbarPosition.Left:
                    Left = (taskbarState.TaskbarSize.right / this.DpiWidthFactor());
                    Top = (taskbarState.TaskbarSize.bottom / this.DpiHeightFactor()) - Height;
                    break;
                case TaskbarPosition.Right:
                    Left = (taskbarState.TaskbarSize.left / this.DpiWidthFactor()) - Width;
                    Top = (taskbarState.TaskbarSize.bottom / this.DpiHeightFactor()) - Height;
                    break;
                case TaskbarPosition.Top:
                    Left = (taskbarState.TaskbarSize.right / this.DpiWidthFactor()) - Width;
                    Top = (taskbarState.TaskbarSize.bottom / this.DpiHeightFactor());
                    break;
                case TaskbarPosition.Bottom:
                    Left = (taskbarState.TaskbarSize.right / this.DpiWidthFactor()) - Width;
                    Top = (taskbarState.TaskbarSize.top / this.DpiHeightFactor()) - Height;
                    break;
            }
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            Guid guid = (Guid)clickedButton.Tag;
            ViewModel.SetGuidAsActive(guid);
        }
    }
}
