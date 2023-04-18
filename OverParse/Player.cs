using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OverParse
{
    public struct Hit
    {
        public uint ID;
        public int Damage, DiffTime;
        public bool Cri;
        public Hit(uint paid, int dmg, bool cri, int time) { ID = paid; Damage = dmg; Cri = cri; DiffTime = time; }

        public string ReadID => ID.ToString();
        public string IDName => MainWindow.skillDict.ContainsKey(ID) ? MainWindow.skillDict[ID].Item1 : ID.ToString();

        public string ReadDamage => Damage.ToString();
        public string IsCri => Cri ? "True" : "False";
        public string UserTime => DateTimeOffset.FromUnixTimeSeconds(DiffTime).LocalDateTime.ToString("HHmmss");

        public ImageSource WpImage => MainWindow.skillDict.ContainsKey(ID) ? Icon.GetWpImage(MainWindow.skillDict[ID].Item3) : Icon.GetWpImage(WpType.NA);
    }

    public class Attack
    {
        public string PAName;
        public List<uint> ID = new List<uint>();
        public long Damage;
        public int Cri, AtkCount, MinDamage, MaxDamage;
        public WpType classType, wpType;

        public Attack(uint paid, int dmg, bool cri) 
        {
            (PAName, classType, wpType) = MainWindow.skillDict.ContainsKey(paid) ? MainWindow.skillDict[paid] : (paid.ToString(), WpType.NA, WpType.NA);
            ID.Add(paid); Cri = cri ? 1 : 0; AtkCount = 1;
            Damage = dmg; MinDamage = dmg; MaxDamage = dmg;
        }
    }

    /// <summary>
    /// Binding Property(MainWindow.xaml)
    /// </summary>
    public class Player
    {
        public int ID, Maxdmg, CriCount, AttackCount, ActiveTime, LastSeenTime;
        public long Damage, Damaged;
        public uint MaxHitID;
        public string PlayerName;
        public List<Attack> Attacks;

        public string ActiveTimeStr => TimeSpan.FromSeconds(ActiveTime).ToString(@"mm\:ss");
        public string DisplayName => PlayerName;
        public string RatioPercent => Damage == MainWindow.current.totalDamage ? "100" : $"{Damage / (double)MainWindow.current.totalDamage * 100:00.00}";

        public string BindDamage => Properties.Settings.Default.DamageSI ? FormatDmg(Damage) : Damage.ToString("N0");

        public long SortDamage => Damage;

        public string BindDamaged => Properties.Settings.Default.DamagedSI ? FormatDmg(Damaged) : Damaged.ToString("N0");

        public double DPS => Damage / (double)ActiveTime;
        public double ReadDPS => Math.Round(Damage / (double)ActiveTime);
        public string FDPSReadout => Properties.Settings.Default.DPSSI ? FormatNumber(ReadDPS) : ReadDPS.ToString("N0");

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

        public string MaxHit => MainWindow.skillDict.ContainsKey(MaxHitID) ? MainWindow.skillDict[MaxHitID].Item1 : MaxHitID.ToString();

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

        public ImageSource ClassImage
        {
            get
            {
                WpType wptype = MainWindow.skillDict.ContainsKey(MaxHitID) ? MainWindow.skillDict[MaxHitID].Item2 : WpType.NA;
                if (wptype == WpType.NA && Attacks.Any(x => x.classType != WpType.NA))
                {
                    wptype = Attacks.OrderByDescending(x => x.Damage).First(x => x.classType != WpType.NA).classType;
                }

                return Icon.GetWpImage(wptype);
            }
        }

        public ImageSource WpImage
        {
            get
            {
                WpType wptype = MainWindow.skillDict.ContainsKey(MaxHitID) ? MainWindow.skillDict[MaxHitID].Item3 : WpType.NA;
                return Icon.GetWpImage(wptype);
            }
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
            Attacks = new List<Attack>();
        }

    }

    public class Session
    {
        public int lastTimestamp, AllActiveTime;
        public long totalDamage;
        public List<Player> players = new List<Player>();

        public long TopPlayerDamage => players.Any() ? players.Max(x => x.Damage) : 0;
    }

    public static class Icon
    {
        public static ImageSource Hu = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Hu.png"));
        public static ImageSource Fi = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Fi.png"));
        public static ImageSource Ra = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Ra.png"));
        public static ImageSource Gu = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Gu.png"));
        public static ImageSource Fo = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Fo.png"));
        public static ImageSource Te = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Te.png"));
        public static ImageSource Br = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Br.png"));
        public static ImageSource Bo = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Bo.png"));
        public static ImageSource Wa = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Wa.png"));
        public static ImageSource Sl = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Sl.png"));
        public static ImageSource Sw = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Sword.png"));
        public static ImageSource Wi = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Wired_Lance.png"));
        public static ImageSource Pa = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Partizan.png"));
        public static ImageSource TD = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Twin_Dagger.png"));
        public static ImageSource DS = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Double_Saber.png"));
        public static ImageSource Kn = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Knuckle.png"));
        public static ImageSource AR = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Assault_Rifle.png"));
        public static ImageSource La = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Launcher.png"));
        public static ImageSource TMG = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/TMG.png"));
        public static ImageSource Ro = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Rod.png"));
        public static ImageSource Ta = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Talis.png"));
        public static ImageSource Wand = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Wand.png"));
        public static ImageSource Ka = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Katana.png"));
        public static ImageSource Bow = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Bow.png"));
        public static ImageSource DB = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Dual_Blade.png"));
        public static ImageSource JB = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Jet_Boots.png"));
        public static ImageSource Tk = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Takt.png"));
        public static ImageSource GS = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/Gunslash.png"));
        public static ImageSource NA = new BitmapImage(new Uri(@"pack://application:,,,/prop/icon_material/NA.png")); // N/A

        public static ImageSource GetWpImage(WpType wpType)
        {
            switch (wpType)
            {
                case WpType.Hu: return Hu;
                case WpType.Fi: return Fi;
                case WpType.Ra: return Ra;
                case WpType.Gu: return Gu;
                case WpType.Fo: return Fo;
                case WpType.Te: return Te;
                case WpType.Br: return Br;
                case WpType.Bo: return Bo;
                case WpType.Wa: return Wa;
                case WpType.Sl: return Sl;
                case WpType.Sw: return Sw;
                case WpType.Wi: return Wi;
                case WpType.Pa: return Pa;
                case WpType.TD: return TD;
                case WpType.DS: return DS;
                case WpType.Kn: return Kn;
                case WpType.AR: return AR;
                case WpType.La: return La;
                case WpType.TMG: return TMG;
                case WpType.Ro: return Ro;
                case WpType.Ta: return Ta;
                case WpType.Wand: return Wand;
                case WpType.Ka: return Ka;
                case WpType.Bow: return Bow;
                case WpType.DB: return DB;
                case WpType.JB: return JB;
                case WpType.Tk: return Tk;
                case WpType.GS: return GS;
            }
            return NA;
        }
    }

    public enum WpType : byte
    {
        Hu, Fi, Ra, Gu, Fo, Te, Br, Bo, Wa, Sl, NA,
        Sw, Wi, Pa, TD, DS, Kn, AR, La, TMG, Ro, Ta, Wand, Ka, Bow, DB, JB, Tk, GS
    }

}
