using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace OverParse
{
    public struct Hit
    {
        public uint ID;
        public int Damage, DiffTime; //Graph?
        public bool Cri;
        public Hit(uint paid, int dmg, bool cri, int time) { ID = paid; Damage = dmg; Cri = cri; DiffTime = time; }

        public string ReadID => ID.ToString();
        public string IDName
        {
            get
            {
                string paname = "Unknown";
                if (MainWindow.skillDict.ContainsKey(ID)) { paname = MainWindow.skillDict[ID]; }
                return paname;
            }
        }

        public string ReadDamage => Damage.ToString("N0");
        public string IsCri => Cri ? "True" : "False";
        public string UserTime => DiffTime.ToString();
    }

    /// <summary>
    /// Binding Property(MainWindow.xaml)
    /// </summary>
    public class Player
    {
        internal int ID;
        public double PercentDPS, PercentReadDPS, TScore;
        public List<Hit> Attacks;
        public ulong Damage, Damaged, CriCount, AttackCount;
        public uint MaxHitID;
        public int Maxdmg;

        public string DisplayName
        {
            get
            {
                if (MainWindow.nameDict.ContainsKey(ID))
                {
                    return MainWindow.nameDict[ID];
                }
                return ID.ToString();
            }
        }

        public string RatioPercent => $"{PercentReadDPS:00.00}";

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

        public string ReadTScore => TScore.ToString("N2");

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
                    ulong tempCtl = CriCount, tempCount = AttackCount;
                    if (Properties.Settings.Default.Nodecimal) { return ((double)tempCtl / tempCount * 100).ToString("N0"); }
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
                        return FormatDmg((ulong)Maxdmg);
                    }
                    else { return Maxdmg.ToString("N0"); }
                }
                catch { return "Error"; }
            }
        }

        public string MaxHit
        {
            get
            {
                if (MainWindow.skillDict.ContainsKey(MaxHitID))
                {
                    return MainWindow.skillDict[MaxHitID];
                }
                return "Unknown";
            }
        }

        public string Writedmgd => Damaged.ToString("N0");
        public string WriteMaxdmg
        {
            get
            {
                try { return Maxdmg.ToString("N0"); } catch { return "Error"; }
            }
        }

        private string FormatDmg(ulong value)
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
                if (MainWindow.workingList.IndexOf(this) % 2 == 0)
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
            lgb.GradientStops.Add(new GradientStop(c, Damage / MainWindow.current.firstDamage));
            lgb.GradientStops.Add(new GradientStop(c2, Damage / MainWindow.current.firstDamage));
            lgb.GradientStops.Add(new GradientStop(c2, 1));
            lgb.SpreadMethod = GradientSpreadMethod.Repeat;
            return lgb;
        }

        public Player(int id)
        {
            ID = id;
            PercentDPS = -1;
            Attacks = new List<Hit>();
            PercentReadDPS = 0;
            Damaged = 0;
        }
    }

    public class Session
    {
        public int startTimestamp, nowTimestamp, diffTime, ActiveTime;
        public List<int> instances = new List<int>();
        public ulong totalDamage;
        public double totalSD, firstDamage, totalDPS;
        public List<Player> players = new List<Player>();
    }

}
