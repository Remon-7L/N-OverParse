using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace OverParse
{
    /// <summary>MainWindow.xaml の相互作用ロジック</summary>
    public partial class MainWindow : YKToolkit.Controls.Window
    {
        public static bool IsRunning, IsConnect;
        public static bool IsOnlyme, IsQuestTime, IsShowGraph, IsHighlight; // Properties.Settings.Default... Read is high cost, My BAD IDEA is this;
        public static StreamReader logReader = null;
        public static int currentPlayerID = 10000000;
        public static string currentPlayerName = null;
        public static SortedList<uint, ValueTuple<string, WpType, WpType>> skillDict = new SortedList<uint, ValueTuple<string, WpType, WpType>>();
        public static DirectoryInfo damagelogs;
        public static FileInfo damagelogcsv;
        public static Session current = new Session();
        public static Session backup = new Session();
        public static ObservableCollection<Hit> userattacks = new ObservableCollection<Hit>();
        public static DispatcherTimer damageTimer, logCheckTimer, inactiveTimer;
        public static Color MyColor, OddLeft, OddRgt, EveLeft, EveRgt, Other;
        public static uint[] ignoreskill = new uint[] { 1281205888, 2505928570, 2692870042 };

        private List<string> LogFilenames = new List<string>();
        private IntPtr hwndcontainer;

        public MainWindow()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
            try { Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\N-OverParse"); Directory.CreateDirectory("Logs"); }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"OverParseに必要なアクセス権限がありません！\n管理者としてOverParseを実行してみるか、システムのアクセス権を確認して下さい！\nOverParseを別のフォルダーに移動してみるのも良いかも知れません。\n\n{ex}");
                Application.Current.Shutdown();
            }
            InitializeComponent();
            Dispatcher.UnhandledException += ErrorToLog;
            if (!Properties.Settings.Default.Initialized)
            {
                Launcher launcher = new Launcher(); launcher.ShowDialog();
                if (launcher.DialogResult != true && Application.Current != null) { Application.Current.Shutdown(); return; }
            }

            AlwaysOnTop.IsChecked = Properties.Settings.Default.AlwaysOnTop;
            ConfigLoad();
        }

        private void ErrorToLog(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Directory.CreateDirectory("ErrorLogs");
                string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
                string filename = $"ErrorLogs/ErrorLogs - {datetime}.txt";
                File.WriteAllText(filename, e.Exception.ToString());
            }
            catch
            {
                MessageBox.Show("OverParseはDirectory<ErrorLogs>の作成に失敗しました。" + Environment.NewLine + "OverParse内のディレクトリにErrorLogを保存しました。");
                string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
                File.WriteAllText($"ErrorLogs - {datetime}.txt", e.Exception.ToString());
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            damagelogs = new DirectoryInfo(Properties.Settings.Default.Path + "\\damagelogs");
            if (Directory.Exists(Properties.Settings.Default.Path + "\\damagelogs") && damagelogs.GetFiles().Any())
            {
                damagelogcsv = damagelogs.GetFiles().Where(f => Regex.IsMatch(f.Name, @"\d+\.")).OrderByDescending(f => f.Name).FirstOrDefault();
                FileStream fileStream = File.Open(damagelogcsv.DirectoryName + "\\" + damagelogcsv.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                _ = fileStream.Seek(0, SeekOrigin.End);
                logReader = new StreamReader(fileStream);
            }

            SkillsLoad();
            HotKeyLoad();
            DamageSortDesc();

            damageTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 200) };
            logCheckTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 20) };
            inactiveTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };
            damageTimer.Tick += new EventHandler(UpdateForm);
            logCheckTimer.Tick += new EventHandler(CheckNewCsv);
            inactiveTimer.Tick += new EventHandler(HideIfInactive);
            damageTimer.Start();
            logCheckTimer.Start();
            inactiveTimer.Start();
        }

        private void HideIfInactive(object sender, EventArgs e)
        {
            if (!Properties.Settings.Default.AutoHideWindow) { return; }
            string title = NativeMethods.GetActiveWindowTitle();
            string[] relevant = { "OverParse", "OverParse Setup", "OverParse Error", "Encounter Timeout", "Phantasy Star Online 2", "Settings", "AtkLog", "Detalis", "Color", "OverParse Install" };
            if (!relevant.Contains(title))
            {
                Opacity = 0;
            }
            else
            {
                TheWindow.Opacity = Properties.Settings.Default.WindowOpacity;
            }
        }

        private void CheckNewCsv(object sender, EventArgs e)
        {
            if (!damagelogs.Exists || !damagelogs.GetFiles().Any()) { return; }
            FileInfo curornewcsv = damagelogs.GetFiles().Where(f => Regex.IsMatch(f.Name, @"\d+\.")).OrderByDescending(f => f.Name).FirstOrDefault();
            if (damagelogcsv != null && curornewcsv.LastWriteTimeUtc <= damagelogcsv.LastWriteTimeUtc) { return; }
            damagelogcsv = curornewcsv;
            FileStream fileStream = File.Open(damagelogcsv.DirectoryName + "\\" + damagelogcsv.Name, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            _ = fileStream.Seek(0, SeekOrigin.End);
            logReader = new StreamReader(fileStream);
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Get this window's handle
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            hwndcontainer = hwnd;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            TheWindow.Opacity = Properties.Settings.Default.WindowOpacity;
            Window window = (Window)sender;
            window.Topmost = AlwaysOnTop.IsChecked;

            if (Properties.Settings.Default.ClickthroughEnabled)
            {
                int extendedStyle = NativeMethods.GetWindowLong(hwndcontainer, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLong(hwndcontainer, NativeMethods.GWL_EXSTYLE, extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = AlwaysOnTop.IsChecked;
            if (Properties.Settings.Default.ClickthroughEnabled)
            {
                int extendedStyle = NativeMethods.GetWindowLong(hwndcontainer, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLong(hwndcontainer, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e) { if (WindowState == WindowState.Maximized) { WindowState = WindowState.Normal; } }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Windowを移動可能にする
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                damageTimer?.Stop();

                DragMove();
            }
            if (e.LeftButton == MouseButtonState.Released && damageTimer != null) { damageTimer.Start(); }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);
        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Closing...
            if (WindowState == WindowState.Maximized)
            {
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                Properties.Settings.Default.Top = Top;
                Properties.Settings.Default.Left = Left;
                Properties.Settings.Default.Height = Height;
                Properties.Settings.Default.Width = Width;
                Properties.Settings.Default.Maximized = false;
            }

            if (IsRunning) { WriteLog(); }
            Properties.Settings.Default.Save();
        }


    }
}
