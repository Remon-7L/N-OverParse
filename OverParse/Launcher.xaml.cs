using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OverParse
{
    /// <summary>
    /// Launcher.xaml の相互作用ロジック
    /// </summary>
    public partial class Launcher : Window
    {
        public Launcher()
        {
            InitializeComponent();
            BinPath.Content = "pso2_bin : " + Properties.Settings.Default.Path;
            PathCheck();
        }

        private void PathCheck()
        {
            if (File.Exists(Properties.Settings.Default.Path + "\\pso2.exe"))
            {
                PathResult.Content = "OK"; PathResult.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                Status.Content = "Status : OK";
                Continue_Button.IsEnabled = true;
            }
            else
            {
                PathResult.Content = "Error"; PathResult.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                Status.Content = "Status : pso2_bin check Failed";
                Continue_Button.IsEnabled = false;
            }
        }

        private void SetBin_Click(object sender, RoutedEventArgs e)
        {
            FolderDialog.FolderBrowserDialog dialog = new FolderDialog.FolderBrowserDialog
            {
                Description = "Description",
                UseDescriptionForTitle = false
            };
            if ((bool)dialog.ShowDialog())
            {
                Properties.Settings.Default.Path = dialog.SelectedPath;
                BinPath.Content = "pso2_bin : " + Properties.Settings.Default.Path;
                PathCheck();
            }
            else
            {
                PathCheck();
            }

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }
        }
        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Initialized = true;
            DialogResult = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Properties.Settings.Default.Path + "\\pso2.exe")) { Application.Current.Shutdown(); }
            SystemCommands.CloseWindow(this);
        }

    }
}
