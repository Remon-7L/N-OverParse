using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace OverParse
{
    public struct Hit
    {
        public uint ID;
        public int Damage, DiffTime;
        public bool Cri;
        public Hit(uint paid, int dmg, bool cri, int time) { ID = paid; Damage = dmg; Cri = cri; DiffTime = time; }

        public string ReadID => ID.ToString();
        public string IDName => MainWindow.skillDict.ContainsKey(ID) ? MainWindow.skillDict[ID] : ID.ToString();

        public string ReadDamage => Damage.ToString();
        public string IsCri => Cri ? "True" : "False";
        public string UserTime => DateTimeOffset.FromUnixTimeSeconds(DiffTime).LocalDateTime.ToString("HHmmss");
    }

    /// <summary>
    /// Binding Property(MainWindow.xaml)
    /// </summary>
    public class Player
    {
        public int ID, Maxdmg;
        public ObservableCollection<Hit> Attacks;
        public long Damage, Damaged, CriCount, AttackCount;
        public uint MaxHitID;
        public string PlayerName;

        public string DisplayName => PlayerName;

        public string RatioPercent
        {
            get
            {
                return Damage == MainWindow.current.totalDamage ? "100" : $"{Damage / (double)MainWindow.current.totalDamage * 100:00.00}";
            }
        }

        public string BindDamage
        {
            get
            {
                if (Properties.Settings.Default.DamageSI)
                {
                    return FormatDmg(Damage);
                }
                else { return Damage.ToString("N0"); }
            }
        }

        public long SortDamage => Damage;

        public string BindDamaged
        {
            get
            {
                if (Properties.Settings.Default.DamagedSI)
                {
                    return FormatDmg(Damaged);
                }
                else { return Damaged.ToString("N0"); }
            }
        }

        public double DPS => Damage / (double)MainWindow.current.ActiveTime;
        public double ReadDPS => Math.Round(Damage / (double)MainWindow.current.ActiveTime);
        public string StringDPS => ReadDPS.ToString("N0");
        public string FDPSReadout
        {
            get
            {
                if (Properties.Settings.Default.DPSSI)
                {
                    return FormatNumber(ReadDPS);
                }
                else
                {
                    return StringDPS;
                }
            }
        }

        public string CRIPercent
        {
            get
            {
                try
                {
                    long tempCtl = CriCount, tempCount = AttackCount;
                    if (Properties.Settings.Default.Nodecimal) 
                    { return ((double)tempCtl / tempCount * 100).ToString("N0"); }
                    else if (tempCtl == tempCount)
                    {
                        return "100";
                    }
                    else { return ((double)tempCtl / tempCount * 100).ToString("N2"); }
                }
                catch { return "Error"; }
            }
        }

        public string WCRIPercent
        {
            get
            {
                try { return ((double)CriCount / AttackCount * 100).ToString("N2"); }
                catch { return "Error"; }
            }
        }

        public string MaxHitdmg
        {
            get
            {
                try
                {
                    if (Properties.Settings.Default.MaxSI)
                    {
                        return FormatDmg(Maxdmg);
                    }
                    else { return Maxdmg.ToString("N0"); }
                }
                catch { return "Error"; }
            }
        }

        public string MaxHit => MainWindow.skillDict.ContainsKey(MaxHitID) ? MainWindow.skillDict[MaxHitID] : "Unknown";

        public string Writedmgd => Damaged.ToString("N0");
        public string WriteMaxdmg
        {
            get
            {
                try { return Maxdmg.ToString("N0"); } catch { return "Error"; }
            }
        }

        private string FormatDmg(long value)
        {
            if (value >= 1000000000) { return (value / 1000000000D).ToString("0.00") + "G"; }
            else if (value >= 100000000) { return (value / 1000000D).ToString("0.0") + "M"; }
            else if (value >= 1000000) { return (value / 1000000D).ToString("0.00") + "M"; }
            else if (value >= 100000) { return (value / 1000).ToString("#,0") + "K"; }
            else if (value >= 10000) { return (value / 1000D).ToString("0.0") + "K"; }
            return value.ToString("#,0");
        }

        private string FormatNumber(double value)
        {
            if (value >= 100000000) { return (value / 1000000).ToString("#,0") + "M"; }
            if (value >= 1000000) { return (value / 1000000D).ToString("0.0") + "M"; }
            if (value >= 100000) { return (value / 1000).ToString("#,0") + "K"; }
            if (value >= 1000) { return (value / 1000D).ToString("0.0") + "K"; }
            return value.ToString("#,0");
        }

        public Brush Brush
        {
            get
            {
                if (MainWindow.current.players.IndexOf(this) % 2 == 0)
                {
                    if (MainWindow.IsShowGraph)
                    {
                        return GenerateBarBrush(MainWindow.OddLeft, MainWindow.OddRgt); //奇数行OnGraph
                    }
                    else { return GenerateBarBrush(MainWindow.OddRgt, MainWindow.OddRgt); } //奇数行OffGraph
                }
                else
                {
                    if (MainWindow.IsShowGraph)
                    {
                        return GenerateBarBrush(MainWindow.EveLeft, MainWindow.EveRgt); //偶数行OnGraph
                    }
                    else { return GenerateBarBrush(MainWindow.EveRgt, MainWindow.EveRgt); } //偶数行OffGraph
                }
            }
        }


        LinearGradientBrush GenerateBarBrush(Color c, Color c2)
        {
            if (MainWindow.currentPlayerID == ID) { c = MainWindow.MyColor; } //前面自分

            LinearGradientBrush lgb = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 0) };
            lgb.GradientStops.Add(new GradientStop(c, 0));
            lgb.GradientStops.Add(new GradientStop(c, Damage / (double)MainWindow.current.TopPlayerDamage));
            lgb.GradientStops.Add(new GradientStop(c2, Damage / (double)MainWindow.current.TopPlayerDamage));
            lgb.GradientStops.Add(new GradientStop(c2, 1));
            lgb.SpreadMethod = GradientSpreadMethod.Repeat;
            return lgb;
        }

        #region 偏差値
        /// <summary>全体平均dmg</summary>
        public double TotalAVG => 
            MainWindow.IsRunning ? 
            MainWindow.current.totalDamage / MainWindow.current.players.Count
            : MainWindow.backup.totalDamage / MainWindow.backup.players.Count;

        /// <summary>標準偏差</summary>
        public double TotalSD =>
            MainWindow.IsRunning ?
            Math.Sqrt(MainWindow.current.players.Select(x => x.Damage * x.Damage).Sum() / MainWindow.current.players.Count - TotalAVG * TotalAVG)
            : Math.Sqrt(MainWindow.backup.players.Select(x => x.Damage * x.Damage).Sum() / MainWindow.backup.players.Count - TotalAVG * TotalAVG);

        /// <summary>偏差値</summary>
        public string TScore
        {
            get
            {
                if (Damage - TotalAVG == 0) { return "50.00"; }
                return ((Damage - TotalAVG) / TotalSD * 10 + 50).ToString("N2");
            }
        }

        #endregion 偏差値




        public Player(int id, string name)
        {
            ID = id;
            PlayerName = name;
            Attacks = new ObservableCollection<Hit>();
            Damaged = 0;
        }


    }

    public class Session
    {
        public int startTimestamp, nowTimestamp, diffTime, ActiveTime;
        public long totalDamage;
        public ObservableCollection<Player> players = new ObservableCollection<Player>();

        public long TopPlayerDamage => players.Any() ? players.Max(x => x.Damage) : 0;
    }

}
