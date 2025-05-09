using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TagFile = TagLib.File;
using IO = System.IO;

namespace RandomMediaViewer
{
    public partial class MainWindow : Window
    {
        // settings pulled from SettingsWindow
        private bool useBar;
        private int cumTimer;
        private int imageInterval;
        private int cumTimeLimit;
        private bool enableCumLimit;
        private bool enableAudioTrigger;
        private bool alignAudioToCountdown;
        private bool enableChooseWhen;

        // runtime state
        private readonly List<string> files = new();
        private readonly Random rand = new();
        private int fileIndex;
        private int countdown;
        private bool secondPhase;
        private double audioOffsetSec;

        // timers / animations
        private readonly DispatcherTimer countdownTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly DispatcherTimer timesUpTimer = new() { Interval = TimeSpan.FromSeconds(1) };
        private DispatcherTimer? audioStartTimer;
        private Storyboard? breathing;

        public MainWindow()
        {
            InitializeComponent();

            startButtonImg.Visibility = Visibility.Visible;
            timerText.Visibility = Visibility.Hidden;

            ShowStartButton(true);

            countdownTimer.Tick += CountdownTimer_Tick;
            timesUpTimer.Tick += TimesUpTimer_Tick;
        }

        private void ShowStartButton(bool show) =>
            startButtonImg.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        private void UpdateTimerText() =>
            timerText.Text = TimeSpan.FromSeconds(countdown).ToString(@"mm\:ss");

        private static readonly string[] MediaExt =
            [".png", ".jpg", ".jpeg", ".bmp", ".mp4", ".avi", ".mov", ".wmv"];

        private List<string> LoadFolder(string folder) =>
            string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)
                ? []
                : Directory.GetFiles(folder)
                           .Where(f => MediaExt.Contains(Path.GetExtension(f).ToLowerInvariant()))
                           .OrderBy(_ => rand.Next())
                           .ToList();

        private void StopAllMediaAndShowDefault()
        {
            mediaElement.Stop();
            mediaElement.Visibility = Visibility.Collapsed;
            imageControl.Visibility = Visibility.Collapsed;

            bgVideo.Stop();
            bgVideo.Visibility = Visibility.Collapsed;
            bgImage.Visibility = Visibility.Collapsed;

            audioElement.Stop();
            defaultBg.Visibility = Visibility.Visible;
        }

        private void StartButton_Click(object sender, MouseButtonEventArgs e)
        {
            ShowStartButton(false);
            StartShow();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            StopAllMediaAndShowDefault();
            countdownTimer.Stop();
            audioStartTimer?.Stop();
            timesUpTimer.Stop();
            backButton.Visibility = Visibility.Collapsed;
            ShowStartButton(true);
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsWindow(cumTimer, imageInterval, cumTimeLimit,
                                         enableCumLimit, enableChooseWhen,
                                         enableAudioTrigger, alignAudioToCountdown,
                                         useBar)
            { Owner = this };

            if (dlg.ShowDialog() != true) return;

            cumTimer = dlg.CumTimer;
            imageInterval = dlg.ImageInterval;
            cumTimeLimit = dlg.CumTimeLimit;
            enableCumLimit = dlg.EnableCumLimit;
            enableAudioTrigger = dlg.EnableAudioTrigger;
            alignAudioToCountdown = dlg.AlignAudioToCountdown;
            enableChooseWhen = dlg.ChooseWhen;
            useBar = dlg.UseBar;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (enableChooseWhen && !secondPhase && e.Key == Key.Space)
            {
                e.Handled = true;
                enableChooseWhen = false;
                audioStartTimer?.Stop();
                TriggerCumPhase();
            }
        }

        private void StartShow()
        {
            files.Clear();
            files.AddRange(LoadFolder(SettingsWindow.Folder1Global));
            fileIndex = 0;
            secondPhase = false;
            countdown = cumTimer;

            ResetUIForRun();
            ShowNext();
        }

        private void ResetUIForRun()
        {
            defaultBg.Visibility = Visibility.Collapsed;
            UpdateTimerText();

            timerText.Visibility = useBar || enableChooseWhen ? Visibility.Collapsed : Visibility.Visible;
            countdownBar.Visibility = Visibility.Collapsed;
            timesUpText.Visibility = Visibility.Collapsed;
            flashText.Visibility = Visibility.Collapsed;

            if (useBar)
            {
                if (enableAudioTrigger && alignAudioToCountdown)
                    ScheduleAudioAlign();

                var barAnim = new DoubleAnimation(0, ActualWidth,
                                TimeSpan.FromSeconds(cumTimer))
                { FillBehavior = FillBehavior.Stop };

                barAnim.Completed += (_, _) =>
                {
                    countdownBar.Visibility = Visibility.Collapsed;
                    TriggerCumPhase();
                };

                countdownBar.Visibility = Visibility.Visible;
                countdownBar.BeginAnimation(WidthProperty, barAnim);
            }
            else if (!enableChooseWhen)
            {
                countdownTimer.Start();

                if (enableAudioTrigger && alignAudioToCountdown)
                    ScheduleAudioAlign();

                StartBreathingAnimation();
            }
        }

        private void CountdownTimer_Tick(object? s, EventArgs e)
        {
            countdown--;
            UpdateTimerText();

            if (countdown > 0) return;

            countdownTimer.Stop();
            audioStartTimer?.Stop();

            if (!secondPhase)
                TriggerCumPhase();
            else
                BeginTimesUp();
        }

        private void TimesUpTimer_Tick(object? s, EventArgs e)
        {
            timesUpTimer.Stop();
            timesUpText.Visibility = Visibility.Collapsed;   // hide banner
            ShowStartButton(true);
        }

        private void StartBreathingAnimation()
        {
            breathing?.Stop();
            breathing = new Storyboard();

            var dur = TimeSpan.FromSeconds(cumTimer);
            var scale = new DoubleAnimation(1.0, 1.05, new Duration(dur))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(scale, timerText);
            Storyboard.SetTargetProperty(scale, new PropertyPath("RenderTransform.ScaleX"));
            breathing.Children.Add(scale);

            var sy = scale.Clone();
            Storyboard.SetTargetProperty(sy, new PropertyPath("RenderTransform.ScaleY"));
            breathing.Children.Add(sy);

            breathing.Begin();
        }

        private void TriggerCumPhase()
        {
            timerText.Visibility = Visibility.Collapsed;
            flashText.Visibility = Visibility.Visible;
            ((Storyboard)FindResource("FlashStoryboard")).Begin();

            if (enableAudioTrigger && !alignAudioToCountdown)
                StartAudio();

            files.Clear();
            files.AddRange(LoadFolder(SettingsWindow.Folder2Global));
            fileIndex = 0;
            ShowNext();

            if (enableCumLimit)
            {
                secondPhase = true;
                countdown = cumTimeLimit;
                timerText.Foreground = Brushes.Purple;
                timerText.Visibility = Visibility.Visible;
                countdownTimer.Start();
            }
        }

        private void BeginTimesUp()
        {
            StopAllMediaAndShowDefault();

            flashText.Visibility = Visibility.Collapsed;
            timerText.Visibility = Visibility.Collapsed;
            timesUpText.Visibility = Visibility.Visible;

            timesUpTimer.Start();
        }

        private void ScheduleAudioAlign()
        {
            if (!IO.File.Exists(SettingsWindow.AudioPathGlobal)) return;

            using var tf = TagFile.Create(SettingsWindow.AudioPathGlobal);
            var total = tf.Properties.Duration.TotalSeconds;

            audioOffsetSec = total >= countdown ? total - countdown : 0;

            if (audioOffsetSec > 0)
                StartAudio();
            else
            {
                var delay = countdown - total;
                audioStartTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(delay) };
                audioStartTimer.Tick += (_, _) =>
                {
                    audioStartTimer.Stop();
                    StartAudio();
                };
                audioStartTimer.Start();
            }
        }

        private void StartAudio()
        {
            audioElement.Source = new Uri(SettingsWindow.AudioPathGlobal);
            audioElement.Position = TimeSpan.FromSeconds(audioOffsetSec);
            audioElement.Play();
        }

        private void ShowNext()
        {
            if (files.Count == 0) return;

            var file = files[fileIndex++];
            if (fileIndex >= files.Count) fileIndex = 0;

            var ext = Path.GetExtension(file).ToLowerInvariant();
            var isVid = ext is ".mp4" or ".avi" or ".mov" or ".wmv";

            // blurred background
            if (isVid)
            {
                bgImage.Visibility = Visibility.Collapsed;

                bgVideo.Source = new Uri(file);
                bgVideo.Position = TimeSpan.Zero;
                bgVideo.Visibility = Visibility.Visible;
                bgVideo.Play();
            }
            else
            {
                bgVideo.Stop();
                bgVideo.Visibility = Visibility.Collapsed;

                bgImage.Source = new BitmapImage(new Uri(file));
                bgImage.Visibility = Visibility.Visible;
            }

            // foreground
            if (isVid)
            {
                mediaElement.Source = new Uri(file);
                mediaElement.Visibility = Visibility.Visible;
                imageControl.Visibility = Visibility.Collapsed;
                mediaElement.Play();
            }
            else
            {
                mediaElement.Stop();
                mediaElement.Visibility = Visibility.Collapsed;

                imageControl.Source = new BitmapImage(new Uri(file));
                imageControl.Visibility = Visibility.Visible;

                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(imageInterval) };
                t.Tick += (_, _) => { t.Stop(); ShowNext(); };
                t.Start();
            }
        }
    }
}