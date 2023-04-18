using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace OverParse
{
    public partial class MainWindow : YKToolkit.Controls.Window
    {
        public static string lastStatus = "";
        public static byte noUpdateCount;

        /// <summary>ログ更新</summary>
        /// <returns>IsLogUpdated</returns>
        public bool UpdateLog()
        {
            if (logReader == null || logReader.EndOfStream) { return false; }
            string newLines = logReader.ReadToEnd();

            string[] dataLine = newLines.Split('\n');
            foreach (string line in dataLine)
            {
                if (string.IsNullOrEmpty(line)) { continue; }
                if (line == "end_encounter\r") { continue; }
                string[] parts = line.Split(',');
                if (int.Parse(parts[7]) < 0) { continue; }
                if (parts[2] == "0" || uint.Parse(parts[6]) == 0) { continue; }

                int lineTimestamp = int.Parse(parts[0]);
                int sourceID = int.Parse(parts[2]);
                string sourceName = parts[3];
                int targetID = int.Parse(parts[4]);
                uint attackID = uint.Parse(parts[6]);
                int hitDamage = int.Parse(parts[7]);
                bool Cri = parts[9] == "1";

                if (currentPlayerID == sourceID) { userattacks.Add(new Hit(attackID, hitDamage, Cri, lineTimestamp)); }

                //処理スタート
                if (10000000 < sourceID) //Player->Enemy
                {
                    if (!IsRunning)
                    {
                        current = new Session();
                    }

                    if (0 < (lineTimestamp - current.lastTimestamp))
                    {
                        current.AllActiveTime++;
                        current.lastTimestamp = lineTimestamp;
                    }

                    Player p = current.players.FirstOrDefault(x => x.ID == sourceID);

                    if (p == null)
                    {
                        current.players.Add(new Player(sourceID, sourceName));
                        p = current.players[current.players.Count - 1];
                    }

                    if(0 < (lineTimestamp - p.LastSeenTime))
                    {
                        p.ActiveTime++;
                        p.LastSeenTime = lineTimestamp;
                    }

                    current.totalDamage += hitDamage; p.Damage += hitDamage; p.AttackCount++;
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

                    IsRunning = true;
                }
                else if (10000000 < targetID) //Enemy->Player
                {
                    if (!IsRunning) { continue; } //被ダメージからセッションが始まらないようにする

                    Player p = current.players.FirstOrDefault(x => x.ID == targetID);
                    if(p != null && !ignoreskill.Contains(attackID)) { p.Damaged += hitDamage; }

                }
            }
            current.players.Sort((x, y) => y.Damage.CompareTo(x.Damage));

            return true;
        }


        public void UpdateForm(object sender, EventArgs e)
        {
            if (current.players == null)
            {
                return;
            }

            damageTimer.Stop();
            if (Properties.Settings.Default.Clock)
            {
                Datetime.Content = DateTime.Now.ToString("HH:mm:ss.ff");
            }

            bool IsLogContain = UpdateLog();
            if (IsLogContain == false && noUpdateCount < 5)
            {
                noUpdateCount++;
                StatusUpdate();
                damageTimer.Start();
                return;
            }

            noUpdateCount = 0;
            StatusUpdate();

            AllTab.CombatantData.ItemsSource = current.players;
            var collectionView = CollectionViewSource.GetDefaultView(AllTab.CombatantData.ItemsSource);
            collectionView.SortDescriptions.Add(new SortDescription("SortDamage", ListSortDirection.Descending));

            damageTimer.Start();
        }

        private void StatusUpdate()
        {
            if (IsRunning)
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 192, 255));
                EncounterStatus.Content = $"{TimeSpan.FromSeconds(current.AllActiveTime):h\\:mm\\:ss}";
                if (0 < current.AllActiveTime) { EncounterStatus.Content += $" - Total : {current.totalDamage:N0}" + $" - {current.totalDamage / current.AllActiveTime:N0} DPS"; }
                lastStatus = (string)EncounterStatus.Content;
            }
            else if (!damagelogs.Exists || !damagelogs.GetFiles().Any())
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 128, 128));
                EncounterStatus.Content = "Directory No Logs : (Re)Start PSO2 or Failed dll plugin Install";
            }
            else if (!IsRunning)
            {
                EncounterIndicator.Fill = new SolidColorBrush(Color.FromArgb(255, 64, 192, 64));
                EncounterStatus.Content = $"Waiting - {lastStatus}";
                if (string.IsNullOrEmpty(lastStatus)) { EncounterStatus.Content = "Waiting... - " + damagelogcsv.Name; }
            }
        }

        /// <summary>Output Log</summary>
        /// <returns>filename</returns>
        public string WriteLog()
        {
            if (current.players.Count == 0) { return null; }
            if (current.AllActiveTime == 0) { current.AllActiveTime = 1; }
            string timer = TimeSpan.FromSeconds(current.AllActiveTime).ToString(@"hh\:mm\:ss");
            StringBuilder log = new StringBuilder(8192);
            _ = log.Append($"{DateTime.Now:F} | {timer} | TotalDamage : {current.totalDamage:N0} | TotalDPS : {current.totalDamage / current.AllActiveTime:N0}").AppendLine().AppendLine();

            foreach (Player c in current.players)
            {
                _ = log.Append($"{c.PlayerName} | {c.RatioPercent}% | 偏差値:{c.TScore} | {c.Damage:N0} dmg | {c.Writedmgd} dmgd | {c.DPS:N0} DPS | Critical: {c.WCRIPercent}% | Max: {c.WriteMaxdmg} ({c.MaxHit})").AppendLine();
            }

            _ = log.AppendLine().AppendLine();

            foreach (Player c in current.players)
            {
                _ = log.AppendLine($"[ {c.PlayerName} - {c.RatioPercent}% - {c.Damage:N0} dmg ]").AppendLine().AppendLine();

                foreach(Attack atk in c.Attacks)
                {
                    _ = log.Append($"{(atk.Damage / c.Damage).ToString("00.00").Substring(0, 5)}%	| {atk.PAName} - {atk.Damage} dmg - Critical : {atk.Cri / atk.AtkCount}%").AppendLine();
                    _ = log.Append($"	|   {atk.AtkCount} hits - {atk.MinDamage} min, {atk.Damage/atk.AtkCount} avg, {atk.MaxDamage} max").AppendLine();
                }

                _ = log.AppendLine();
            }

            string directory = string.Format("{0:yyyy-MM-dd}", DateTime.Now);
            Directory.CreateDirectory($"Logs/{directory}");
            string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
            string filename = $"Logs/{directory}/OverParse - {datetime}.txt";
            File.WriteAllText(filename, log.ToString());

            return filename;
        }

    }

}
