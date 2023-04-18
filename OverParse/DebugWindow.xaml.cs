using System;
using System.ComponentModel;
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
                if (line == "end_encounter") { continue; }
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
                bool Cri = parts[9] == "1";
                if (sourceName.Contains("comma")) { sourceName = sourceName.Replace("comma", ","); }
                if (targetName.Contains("comma")) { targetName = targetName.Replace("comma", ","); }

                //処理スタート
                if (10000000 < sourceID) //Player->Enemy
                {
                    if (!MainWindow.IsRunning) { MainWindow.current = new Session(); }

                    if (0 < (lineTimestamp - MainWindow.current.lastTimestamp))
                    {
                        MainWindow.current.AllActiveTime++;
                        MainWindow.current.lastTimestamp = lineTimestamp;
                    }

                    Player p = MainWindow.current.players.FirstOrDefault(x => x.ID == sourceID);

                    if (p == null)
                    {
                        MainWindow.current.players.Add(new Player(sourceID, sourceName));
                        p = MainWindow.current.players[MainWindow.current.players.Count - 1];
                    }

                    if (0 < (lineTimestamp - p.LastSeenTime))
                    {
                        p.ActiveTime++;
                        p.LastSeenTime = lineTimestamp;
                    }

                    MainWindow.current.totalDamage += hitDamage; p.Damage += hitDamage; p.AttackCount++;
                    if (Cri) { p.CriCount++; }
                    if (p.Maxdmg < hitDamage) { p.Maxdmg = hitDamage; p.MaxHitID = attackID; }

                    Attack atk = p.Attacks.FirstOrDefault(x => x.ID.Contains(attackID));
                    if (atk == null)
                    {
                        p.Attacks.Add(new Attack(attackID, hitDamage, Cri));
                    }
                    else
                    {
                        atk.Damage += hitDamage; atk.AtkCount++;
                        if (Cri) { atk.Cri++; }
                        if (hitDamage < atk.MinDamage) { atk.MinDamage = hitDamage; }
                        if (atk.MaxDamage < hitDamage) { atk.MaxDamage = hitDamage; }
                    }

                    MainWindow.IsRunning = true;
                }
                else if (10000000 < targetID) //Enemy->Player
                {
                    if (!MainWindow.IsRunning) { continue; } //被ダメージからセッションが始まらないようにする

                    Player p = MainWindow.current.players.FirstOrDefault(x => x.ID == targetID);
                    if (p != null && !MainWindow.ignoreskill.Contains(attackID)) { p.Damaged += hitDamage; }
                }


            }

            MainWindow.current.players.Sort((x, y) => y.Damage.CompareTo(x.Damage));

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

            //skills.csv
            foreach (string pa in File.ReadAllLines(@"prop/skills.csv"))
            {
                string[] split = pa.Split(',');
                if (split.Length > 1)
                {
                    Enum.TryParse(split[0], out WpType classType);
                    Enum.TryParse(split[1], out WpType wpType);
                    MainWindow.skillDict.Add(uint.Parse(split[3]), (split[2], classType, wpType));
                }
            }
#endif
        }

    }
}