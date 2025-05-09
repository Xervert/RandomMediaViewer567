// =================================
//  SettingsWindow.xaml.cs – full file
// =================================
using System;
using System.Windows;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace RandomMediaViewer
{
    public partial class SettingsWindow : Window
    {
        // single, authoritative static paths (duplicates removed)
        public static string Folder1Global { get; private set; } = "";
        public static string Folder2Global { get; private set; } = "";
        public static string AudioPathGlobal { get; private set; } = "";

        /* ───────────── values returned to MainWindow ───────────── */
        public int CumTimer { get; private set; }
        public int ImageInterval { get; private set; }
        public int CumTimeLimit { get; private set; }
        public bool EnableCumLimit { get; private set; }
        public bool EnableAudioTrigger { get; private set; }
        public bool AlignAudioToCountdown { get; private set; }
        public bool ChooseWhen { get; private set; }
        public bool UseBar { get; private set; }

        public SettingsWindow(int curCum, int curImg, int curLimit,
                              bool curEnableLimit, bool curChooseWhen,
                              bool curAudioTrigger, bool curAlign,
                              bool curUseBar)
        {
            InitializeComponent();

            folder1TextBox.Text = Folder1Global;
            folder2TextBox.Text = Folder2Global;
            audioPathTextBox.Text = AudioPathGlobal;
            cumTimerTextBox.Text = curCum.ToString();
            imageIntervalTextBox.Text = curImg.ToString();
            cumLimitTextBox.Text = curLimit.ToString();

            enableLimitCheckbox.IsChecked = curEnableLimit;
            chooseWhenCheckbox.IsChecked = curChooseWhen;
            audioTriggerCheckbox.IsChecked = curAudioTrigger;
            alignAudioCheckbox.IsChecked = curAlign;
            useBarCheckbox.IsChecked = curUseBar;
        }

        /* ───────────── browse helpers ───────────── */
        private static string PickFolder(string initial)
        {
            using var dlg = new Forms.FolderBrowserDialog { SelectedPath = initial };
            return dlg.ShowDialog() == Forms.DialogResult.OK ? dlg.SelectedPath : initial;
        }

        private void BrowseFolder1_Click(object sender, RoutedEventArgs e) =>
            folder1TextBox.Text = PickFolder(folder1TextBox.Text);

        private void BrowseFolder2_Click(object sender, RoutedEventArgs e) =>
            folder2TextBox.Text = PickFolder(folder2TextBox.Text);

        private void BrowseAudio_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "MP3 files (*.mp3)|*.mp3" };
            if (dlg.ShowDialog() == true) audioPathTextBox.Text = dlg.FileName;
        }

        /* ───────────── Save / Cancel ───────────── */
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(cumTimerTextBox.Text, out var ct)) ct = 0;
            if (!int.TryParse(imageIntervalTextBox.Text, out var ii)) ii = 0;
            if (!int.TryParse(cumLimitTextBox.Text, out var cl)) cl = 0;

            CumTimer = ct;
            ImageInterval = ii;
            CumTimeLimit = cl;
            EnableCumLimit = enableLimitCheckbox.IsChecked == true;
            ChooseWhen = chooseWhenCheckbox.IsChecked == true;
            EnableAudioTrigger = audioTriggerCheckbox.IsChecked == true;
            AlignAudioToCountdown = alignAudioCheckbox.IsChecked == true;
            UseBar = useBarCheckbox.IsChecked == true;

            Folder1Global = folder1TextBox.Text;
            Folder2Global = folder2TextBox.Text;
            AudioPathGlobal = audioPathTextBox.Text;

            DialogResult = true;        // signals “Save” – slideshow still starts only from Start button
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
