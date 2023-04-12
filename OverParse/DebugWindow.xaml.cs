using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace OverParse
{
    /// <summary>
    /// DebugWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class DebugWindow : Window
    {
        public DebugWindow() => InitializeComponent();

        private void ShowColorBox(object sender, RoutedEventArgs e)
        {
#if DEBUG
            MainWindow m = (MainWindow)Application.Current.MainWindow;
            var collectionView = CollectionViewSource.GetDefaultView(m.AllTab.CombatantData.ItemsSource);
            collectionView.SortDescriptions.Add(new SortDescription("SortDamage", ListSortDirection.Descending));
            //SelectColor selectwindow = new SelectColor(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            //selectwindow.Show();
#endif
        }

        private void LoadCsvFile(object sender, RoutedEventArgs e)
        {
#if DEBUG
            MainWindow m = (MainWindow)Application.Current.MainWindow;

            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "combat.csv(*.csv)|*.csv"
            };

            if (dialog.ShowDialog() == false) { return; }

            FileStream fileStream = File.Open(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileStream.Seek(0, SeekOrigin.Begin);
            MainWindow.logReader = new StreamReader(fileStream);
            MainWindow.logReader.ReadLine();
            
            string[] dataLine = File.ReadAllLines(dialog.FileName);
            foreach (string line in dataLine)
            {
                if (line == "") { continue; }
                if (line == "timestamp, instanceID, sourceID, sourceName, targetID, targetName, attackID, damage, IsJA, IsCrit, IsMultiHit, IsMisc, IsMisc2") { continue; }
                string[] parts = line.Split(',');
                if (int.Parse(parts[7]) < 0) { continue; }
                if (parts[2] == "0" || uint.Parse(parts[6]) == 0) { continue; }

                int lineTimestamp = int.Parse(parts[0]);
                int sourceID = int.Parse(parts[2]);
                string sourceName = parts[3];
                int targetID = int.Parse(parts[4]);
                string targetName = parts[5];
                uint attackID = uint.Parse(parts[6]);
                int hitDamage = int.Parse(parts[7]);
                bool Cri = (parts[9] == "1");
                if (sourceName.Contains("comma")) { sourceName = sourceName.Replace("comma", ","); }
                if (targetName.Contains("comma")) { targetName = targetName.Replace("comma", ","); }

                //処理スタート
                if (10000000 < sourceID) //Player->Enemy
                {
                    if (!MainWindow.IsRunning) { MainWindow.current = new Session(); }

                    if (MainWindow.current.startTimestamp == 0)
                    { //initialize
                        MainWindow.current.startTimestamp = lineTimestamp;
                        MainWindow.current.nowTimestamp = lineTimestamp;
                    }

                    if (0 < (lineTimestamp - MainWindow.current.nowTimestamp))
                    {
                        MainWindow.current.diffTime++;
                        MainWindow.current.nowTimestamp = lineTimestamp;
                    }

                    if (MainWindow.IsQuestTime) { MainWindow.current.ActiveTime = MainWindow.current.diffTime; }
                    else { MainWindow.current.ActiveTime = lineTimestamp - MainWindow.current.startTimestamp; }

                    Player p;
                    if (MainWindow.current.players.Any(i => i.ID == sourceID))
                    {
                        p = MainWindow.current.players.First(x => x.ID == sourceID);
                    }
                    else
                    {
                        MainWindow.current.players.Add(new Player(sourceID, sourceName));
                        p = MainWindow.current.players[MainWindow.current.players.Count - 1];
                    }

                    MainWindow.current.totalDamage += hitDamage; p.Damage += hitDamage; p.AttackCount++;
                    if (Cri) { p.CriCount++; }
                    if (p.Maxdmg < hitDamage) { p.Maxdmg = hitDamage; p.MaxHitID = attackID; }
                    p.Attacks.Add(new Hit(attackID, hitDamage, Cri, MainWindow.current.diffTime));

                    MainWindow.IsRunning = true;
                }
                else if (10000000 < targetID) //Enemy->Player
                {
                    if (!MainWindow.IsRunning) { continue; } //被ダメージからセッションが始まらないようにする

                    Player p = MainWindow.current.players.FirstOrDefault(x => x.ID == targetID);
                    if (p != null) { p.Damaged += hitDamage; }
                }
            }

            m.AllTab.CombatantData.ItemsSource = MainWindow.current.players;
            var collectionView = CollectionViewSource.GetDefaultView(m.AllTab.CombatantData.ItemsSource);
            collectionView.SortDescriptions.Add(new SortDescription("SortDamage", ListSortDirection.Descending));
#endif
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            MainWindow m = (MainWindow)Application.Current.MainWindow;

            Player p = new Player(10000001, "DebugUser")
            {
                Damage = 88888888,
                Damaged = 88888,
                Maxdmg = 888888,
                MaxHitID = 123456,
                
            };

            MainWindow.current.players.Add(p);
#endif
        }

        private void ReloadSkills(object sender, RoutedEventArgs e)
        {
#if DEBUG
            MainWindow.skillDict.Clear();

            string[] skills = File.ReadAllLines(@"prop/skills.csv");
            foreach (string pa in skills)
            {
                string[] split = pa.Split(',');
                if (split.Length > 1) { _ = MainWindow.skillDict.GetOrAdd(uint.Parse(split[1]), split[0]); }
            }
#endif
        }

        private void RunPSO2_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            if (!IsAdministrator()) { _ = MessageBox.Show("管理者権限がありません。"); return; }

            //not work
            Environment.SetEnvironmentVariable("-pso2", "+0x01e3f1e9");

            ProcessStartInfo inf = new ProcessStartInfo
            {
                Arguments = "+0x33aca2b9",
                FileName = @"C:\Program Files (x86)\SEGA\PHANTASYSTARONLINE2\pso2_bin\pso2.exe",
                UseShellExecute = true,
                WorkingDirectory = @"C:\Program Files (x86)\SEGA\PHANTASYSTARONLINE2\pso2_bin\"
            };

            Process p = new Process
            {
                StartInfo = inf
            };

            _ = p.Start();
#endif
        }

        private void EnableDarkMode_Click(object sender, RoutedEventArgs e)
        {
#if DEBUG
            var box = new System.Windows.Interop.WindowInteropHelper(this);
            NativeMethods.SetWindowTheme(box.Handle, "DarkMode_Explorer", IntPtr.Zero);
#endif
        }

        private bool IsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

    }
}