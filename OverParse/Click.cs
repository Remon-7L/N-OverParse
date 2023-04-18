using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OverParse
{
    public partial class MainWindow : YKToolkit.Controls.Window
    {
        private void EndEncounter_Click(object sender, RoutedEventArgs e)
        {
            bool temp = Properties.Settings.Default.AutoEndEncounters;
            Properties.Settings.Default.AutoEndEncounters = false;
            Properties.Settings.Default.AutoEndEncounters = temp;
            backup = current;
            backup.players = new List<Player>(current.players);

            string filename = WriteLog();
            if (filename != null)
            {
                if ((SessionLogs.Items[0] as MenuItem).Name == "SessionLogPlaceholder") { SessionLogs.Items.Clear(); }
                int items = SessionLogs.Items.Count;
                string prettyName = filename.Split('/').LastOrDefault();
                LogFilenames.Add(filename);
                var menuItem = new MenuItem() { Name = "SessionLog_" + items.ToString(), Header = prettyName };
                menuItem.Click += OpenRecentLog_Click;
                SessionLogs.Items.Add(menuItem);
            }
            IsRunning = false;
            UpdateForm(this, null);
        }

        public void EndEncounter_Key(object sender, EventArgs e) => EndEncounter_Click(null, null);

        private void EndEncounterNoLog_Click(object sender, RoutedEventArgs e)
        {
            current = backup;
            current.players = new List<Player>(backup.players);
            IsRunning = false;
            UpdateForm(this, null);
        }

        private void EndEncounterNoLog_Key(object sender, EventArgs e) => EndEncounterNoLog_Click(sender, null);

        private void RegistUserID_Click(object sender, RoutedEventArgs e)
        {
            AlwaysOnTop.IsChecked = false;
            Inputbox input = new Inputbox("UserID Regist", "ユーザーIDを入力", Properties.Settings.Default.RegistID.ToString()) { Owner = this };
            input.ShowDialog();
            if (int.TryParse(input.ResultText, out int x))
            {
                if (10000000 < x)
                {
                    Properties.Settings.Default.RegistID = x;
                    currentPlayerID = x;
                }
                else { MessageBox.Show("範囲が無効です。"); }
            }
            else
            {
                if (input.ResultText.Length > 0) { MessageBox.Show("Couldn't parse your input. Enter only a number."); }
            }
            AlwaysOnTop.IsChecked = Properties.Settings.Default.AlwaysOnTop;
        }

        private void OpenLogsFolder_Click(object sender, RoutedEventArgs e) => Process.Start(Directory.GetCurrentDirectory() + "\\Logs");

        private void OpenRecentLog_Click(object sender, RoutedEventArgs e) => Process.Start(Directory.GetCurrentDirectory() + "\\" + LogFilenames[SessionLogs.Items.IndexOf(e.OriginalSource as MenuItem)]);

        private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AlwaysOnTop = AlwaysOnTop.IsChecked;
            OnActivated(e);
        }

        public void AlwaysOnTop_Key(object sender, EventArgs e)
        {
            AlwaysOnTop.IsChecked = !AlwaysOnTop.IsChecked;
            IntPtr wasActive = NativeMethods.GetForegroundWindow();

            // hack for activating overparse window
            WindowState = WindowState.Minimized;
            Show();
            WindowState = WindowState.Normal;

            Topmost = AlwaysOnTop.IsChecked;
            AlwaysOnTop_Click(null, null);
            NativeMethods.SetForegroundWindow(wasActive);
        }

        private void OpenInstall_Click(object sender, RoutedEventArgs e)
        {
            Launcher launcher = new Launcher() { Owner = this }; launcher.ShowDialog();
        }

#if DEBUG
        private void DebugWindow_Key(object sender, EventArgs e)
        {
            DebugWindow debugWindow = new DebugWindow();
            debugWindow.Show();
        }
#endif

        private void AtkLog_Click(object sender, RoutedEventArgs e)
        {
            AtkLogWindow window = new AtkLogWindow() { Owner = this };
            window.Show();
        }

        private void ReloadSkills(object sender, RoutedEventArgs e)
        {
#if DEBUG
#endif
        }

    }
}
