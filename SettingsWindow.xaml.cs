using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FoldVision
{
    public partial class SettingsWindow : Window
    {
        private bool _isInitializing = true;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentValues();
        }

        private void LoadCurrentValues()
        {
            DepthSlider.Value      = AppSettings.CameraDepth;
            StretchSlider.Value    = AppSettings.StretchMultiplier;
            BlurSlider.Value       = AppSettings.BlurStrength;
            AngleStartSlider.Value = AppSettings.AngleStart;
            AngleMaxSlider.Value   = AppSettings.AngleMax;
            ShowDebugCheckBox.IsChecked = AppSettings.ShowDebugAngle;

            // Verificar si el inicio automático está activo en el registro
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    AutoStartCheckBox.IsChecked = (key.GetValue("FoldVision") != null);
                }
            }

            UpdateLabels();
            _isInitializing = false;
        }

        private void UpdateLabels()
        {
            DepthValue.Text       = $"{AppSettings.CameraDepth:F1}";
            StretchValue.Text     = $"{AppSettings.StretchMultiplier:F1}x";
            BlurValue.Text        = $"{AppSettings.BlurStrength:F2}";
            AngleStartValue.Text  = $"{AppSettings.AngleStart:F0}°";
            AngleMaxValue.Text    = $"{AppSettings.AngleMax:F0}°";
        }

        private void DepthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.CameraDepth = (float)e.NewValue;
            if (DepthValue != null) DepthValue.Text = $"{AppSettings.CameraDepth:F1}";
            AppSettings.Save();
        }

        private void StretchSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.StretchMultiplier = (float)e.NewValue;
            if (StretchValue != null) StretchValue.Text = $"{AppSettings.StretchMultiplier:F1}x";
            AppSettings.Save();
        }

        private void BlurSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.BlurStrength = (float)e.NewValue;
            if (BlurValue != null) BlurValue.Text = $"{AppSettings.BlurStrength:F2}";
            AppSettings.Save();
        }

        private void AngleStart_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.AngleStart = (float)e.NewValue;
            // Garantizar que AngleStart < AngleMax
            if (AppSettings.AngleStart >= AppSettings.AngleMax)
            {
                AppSettings.AngleMax = AppSettings.AngleStart + 5f;
                AngleMaxSlider.Value = AppSettings.AngleMax;
            }
            if (AngleStartValue != null) AngleStartValue.Text = $"{AppSettings.AngleStart:F0}°";
            AppSettings.Save();
        }

        private void AngleMax_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.AngleMax = (float)e.NewValue;
            // Garantizar que AngleMax > AngleStart
            if (AppSettings.AngleMax <= AppSettings.AngleStart)
            {
                AppSettings.AngleStart = AppSettings.AngleMax - 5f;
                AngleStartSlider.Value = AppSettings.AngleStart;
            }
            if (AngleMaxValue != null) AngleMaxValue.Text = $"{AppSettings.AngleMax:F0}°";
            AppSettings.Save();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            AppSettings.CameraDepth       = 3.2f;
            AppSettings.StretchMultiplier = 0.6f;
            AppSettings.BlurStrength      = 0.50f;
            AppSettings.AngleStart        = 110f;
            AppSettings.AngleMax          = 140f;
            LoadCurrentValues();
            AppSettings.Save();
        }

        private void AutoStart_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    if (AutoStartCheckBox.IsChecked == true)
                    {
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                        if (!string.IsNullOrEmpty(exePath))
                            key.SetValue("FoldVision", $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue("FoldVision", false);
                    }
                }
            }
        }

        private void ShowDebug_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            AppSettings.ShowDebugAngle = ShowDebugCheckBox.IsChecked == true;
            AppSettings.Save();
        }
    }
}
