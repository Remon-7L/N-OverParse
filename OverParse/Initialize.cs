using HotKeyFrame;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OverParse
{
    public partial class MainWindow : YKToolkit.Controls.Window
    {
        private HotKey hotkey1, hotkey2, hotkey3, hotkey4;

        private void SkillsLoad()
        {
            //skills.csv
            try
            {
                string[] skills = File.ReadAllLines(@"prop/skills.csv");

                foreach (string pa in skills)
                {
                    string[] split = pa.Split(',');
                    if (split.Length > 1) { _ = skillDict.GetOrAdd(uint.Parse(split[1]), split[0]); }
                }
            }
            catch
            {
                MessageBox.Show("skills.csvが存在しません。\n全ての最大ダメージはUnknownとなります。", "OverParse Setup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void HotKeyLoad()
        {
            try
            {
                hotkey1 = hotkey2 = hotkey3 = new HotKey(this);

                hotkey1.Regist(ModifierKeys.Control | ModifierKeys.Shift, Key.E, new EventHandler(EndEncounter_Key), 0x0071);
                hotkey2.Regist(ModifierKeys.Control | ModifierKeys.Shift, Key.R, new EventHandler(EndEncounterNoLog_Key), 0x0072);
                hotkey3.Regist(ModifierKeys.Control | ModifierKeys.Shift, Key.A, new EventHandler(AlwaysOnTop_Key), 0x0073);
#if DEBUG
                hotkey4 = new HotKey(this);
                hotkey4.Regist(ModifierKeys.Control | ModifierKeys.Shift, Key.F11, new EventHandler(DebugWindow_Key), 0x0077);
#endif
            }
            catch
            {
                MessageBox.Show("OverParseはホットキーを初期化出来ませんでした。　多重起動していないか確認して下さい！\nプログラムは引き続き使用できますが、ホットキーは反応しません。", "OverParse Setup", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ConfigLoad()
        {
            MainWindow m = (MainWindow)Application.Current.MainWindow;

            m.Datetime.Visibility = Properties.Settings.Default.Clock ? Visibility.Visible : Visibility.Collapsed;
            currentPlayerID = Properties.Settings.Default.RegistID;


            //フォントサイズ適用
            m.AllTab.CombatantData.FontSize = 14.0;

            //GraphSettings
            IsShowGraph = Properties.Settings.Default.IsShowGraph;
            IsHighlight = Properties.Settings.Default.IsGraphHighLight;

            //ColorBrush
            MyColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.MyColorBrush);
            OddLeft = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.OddLeftColor);
            OddRgt = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.OddRgtColor);
            EveLeft = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.EveLeftColor);
            EveRgt = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.EveRgtColor);
            Other = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.OtherBrush);

            // - - - -
            IsOnlyme = Properties.Settings.Default.Onlyme;
            IsQuestTime = Properties.Settings.Default.QuestTime;
            // - - - -

            TheWindow.Opacity = Properties.Settings.Default.WindowOpacity;

            if (Properties.Settings.Default.BackContent == "Color")
            {
                try { ContentBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.BackColor)); }
                catch { ContentBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0A0A0A")); }
            }
            else if (Properties.Settings.Default.BackContent == "Image")
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(Properties.Settings.Default.ImagePath));
                    ImageBrush brush = new ImageBrush
                    {
                        ImageSource = bitmap,
                        Stretch = Stretch.UniformToFill
                    };
                    ContentBackground = brush;
                }
                catch { ContentBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0A0A0A")); }
            }
            else { ContentBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0A0A0A")); }

            //リスト前面カラー
            System.Windows.Media.Color color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Properties.Settings.Default.FontColor);
            AllTab.CombatantData.Foreground = new SolidColorBrush(color);

            //色を適用した後、最後に設定
            m.Background.Opacity = Properties.Settings.Default.ListOpacity;
        }

        private void DamageSortDesc()
        {
            AllTab.CombatantData.ItemsSource = current.players;
            var collectionView = CollectionViewSource.GetDefaultView(AllTab.CombatantData.ItemsSource);
            collectionView.SortDescriptions.Add(new SortDescription("SortDamage", ListSortDirection.Descending));
        }

    }
}
