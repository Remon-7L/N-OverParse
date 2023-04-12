using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public async Task<bool> UpdateLog()
        {
            if (logReader == null) { return false; }
            string newLines = await logReader.ReadToEndAsync();
            if (string.IsNullOrEmpty(newLines)) { return false; }

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
                        AllTab.CombatantData.ItemsSource = current.players;
                        var collectionView = CollectionViewSource.GetDefaultView(AllTab.CombatantData.ItemsSource);
                        collectionView.SortDescriptions.Add(new SortDescription("SortDamage", ListSortDirection.Descending));
                    }

                    if (current.startTimestamp == 0)
                    { //initialize
                        current.startTimestamp = lineTimestamp;
                        current.nowTimestamp = lineTimestamp; 
                    }

                    if (0 < (lineTimestamp - current.nowTimestamp))
                    {
                        current.diffTime++;
                        current.nowTimestamp = lineTimestamp;
                    }

                    if (IsQuestTime) { current.ActiveTime = current.diffTime; }
                    else { current.ActiveTime = lineTimestamp - current.startTimestamp; }

                    Player p;
                    if (current.players.Any(i => i.ID == sourceID))
                    {
                        p = current.players.First(x => x.ID == sourceID);
                    }
                    else
                    {
                        current.players.Add(new Player(sourceID, sourceName));
                        p = current.players[current.players.Count - 1];
                    }

                    current.totalDamage += hitDamage; p.Damage += hitDamage; p.AttackCount++;
                    if (Cri) { p.CriCount++; }
                    if (p.Maxdmg < hitDamage) { p.Maxdmg = hitDamage; p.MaxHitID = attackID; }
                    p.Attacks.Add(new Hit(attackID, hitDamage, Cri, current.diffTime));

                    IsRunning = true;
                }
                else if (10000000 < targetID) //Enemy->Player
                {
                    if (!IsRunning) { continue; } //被ダメージからセッションが始まらないようにする

                    Player p = current.players.FirstOrDefault(x => x.ID == targetID);
                    if(p != null) { p.Damaged += hitDamage; }
                    
                    /* 
                     * Plugin_20230407では被ダメにプレイヤー名情報が入らない為
                     * Sessionに既に登録されてない場合は無視
                     */
                }
            }

            return true;
        }


        public async void UpdateForm(object sender, EventArgs e)
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

            bool IsLogContain = await UpdateLog();
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
                TimeSpan timespan = TimeSpan.FromSeconds(current.ActiveTime);
                string timer = timespan.ToString(@"h\:mm\:ss");
                EncounterStatus.Content = $"{timer}";

                double totalDPS = current.totalDamage / (double)current.ActiveTime;
                if (totalDPS > 0) { EncounterStatus.Content += $" - Total : {current.totalDamage:N0}" + $" - {totalDPS:N0} DPS"; }
                lastStatus = EncounterStatus.Content.ToString();
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
            if (current.ActiveTime == 0) { current.ActiveTime = 1; }
            string timer = TimeSpan.FromSeconds(current.ActiveTime).ToString(@"hh\:mm\:ss");
            StringBuilder log = new StringBuilder(8192);
            _ = log.Append($"{DateTime.Now:F} | {timer} | TotalDamage : {current.totalDamage:N0} | TotalDPS : {current.totalDamage / current.ActiveTime:N0}").AppendLine().AppendLine();

            foreach (Player c in current.players)
            {
                _ = log.Append($"{c.PlayerName} | {c.RatioPercent}% | 偏差値:{c.TScore} | {c.Damage:N0} dmg | {c.Writedmgd} dmgd | {c.DPS:N0} DPS | Critical: {c.WCRIPercent}% | Max: {c.WriteMaxdmg} ({c.MaxHit})").AppendLine();
            }

            _ = log.AppendLine().AppendLine();

            foreach (Player c in current.players)
            {
                List<string> attackNames = new List<string>();
                List<Tuple<string, List<int>, List<bool>>> attackData = new List<Tuple<string, List<int>, List<bool>>>();

                _ = log.AppendLine($"[ {c.PlayerName} - {c.RatioPercent}% - {c.Damage:N0} dmg ]").AppendLine().AppendLine();

                List<PAHit> temphits = new List<PAHit>();
                foreach (Hit atk in c.Attacks)
                {
                    //PAID -> PAName
                    string temp = atk.ID.ToString();
                    if (skillDict.ContainsKey(atk.ID)) { temp = skillDict[atk.ID]; } // these are getting disposed anyway, no 1 cur
                    if (!attackNames.Contains(temp)) { attackNames.Add(temp); }
                    temphits.Add(new PAHit(temp, atk.Damage, atk.Cri));
                }

                foreach (string paname in attackNames)
                {
                    //マッチングアタックからダメージを選択するだけ
                    List<int> matchingAttacks = temphits.Where(a => a.Name == paname).Select(a => a.Damage).ToList();
                    List<bool> criPercents = temphits.Where(a => a.Name == paname).Select(a => a.Cri).ToList();
                    attackData.Add(new Tuple<string, List<int>, List<bool>>(paname, matchingAttacks, criPercents));
                }

                attackData = attackData.OrderByDescending(x => x.Item2.Sum()).ToList();

                foreach (var i in attackData)
                {
                    List<int> excri = i.Item3.ConvertAll(x => Convert.ToInt32(x));

                    string min, max, avg, cri;
                    double percent = i.Item2.Sum() * 100d / c.Damage;
                    string spacer = (percent >= 9) ? "" : " ";

                    string paddedPercent = percent.ToString("00.00").Substring(0, 5);
                    string hits = i.Item2.Count().ToString("N0");
                    string sum = i.Item2.Sum().ToString("N0");

                    min = i.Item2.Min().ToString("N0");
                    max = i.Item2.Max().ToString("N0");
                    avg = i.Item2.Average().ToString("N0");
                    cri = excri.Any() ? (excri.Average() * 100).ToString("N2") : "NaN";

                    _ = log.Append($"{paddedPercent}%	| {i.Item1} - {sum} dmg - Critical : {cri}%").AppendLine();
                    _ = log.Append($"	|   {hits} hits - {min} min, {avg} avg, {max} max").AppendLine();
                }

                _ = log.AppendLine();
            }

            DateTime thisDate = DateTime.Now;
            string directory = string.Format("{0:yyyy-MM-dd}", DateTime.Now);
            Directory.CreateDirectory($"Logs/{directory}");
            string datetime = string.Format("{0:yyyy-MM-dd_HH-mm-ss}", DateTime.Now);
            string filename = $"Logs/{directory}/OverParse - {datetime}.txt";
            File.WriteAllText(filename, log.ToString());

            return filename;
        }

    }

    public struct PAHit //Use WriteLog Only
    {
        public string Name;
        public int Damage;
        public bool Cri;
        public PAHit(string paname, int dmg, bool cri) { Name = paname; Damage = dmg; Cri = cri; }
    }
}
