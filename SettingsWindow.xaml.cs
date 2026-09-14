using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;

using Wpf.Ui.Controls;

namespace FoldVision
{
    public partial class SettingsWindow : FluentWindow
    {
        private bool _isInitializing = true;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentValues();
        }

        private void LoadCurrentValues()
        {
            DepthSlider.Value           = AppSettings.CameraDepth;
            StretchSlider.Value         = AppSettings.StretchMultiplier;
            BlurSlider.Value            = AppSettings.BlurStrength;
            CornerSlider.Value          = AppSettings.CornerRadius;
            CornerStartSlider.Value     = AppSettings.CornerStartRadius;
            CornerAnimSlider.Value      = AppSettings.CornerAnimRange;
            OpacityAnimSlider.Value     = AppSettings.OpacityAnimStart;
            ClipHeightSlider.Value      = AppSettings.ClipHeight;
            AngleStartSlider.Value      = AppSettings.AngleStart;
            AngleMaxSlider.Value        = AppSettings.AngleMax;
            ShowDebugCheckBox.IsChecked = AppSettings.ShowDebugAngle;
            chkTabletMode.IsChecked     = AppSettings.DisableInTabletMode;
            AppModeCombo.SelectedIndex  = AppSettings.AppMode == "Live" ? 1 : 0;
            LanguageCombo.SelectedIndex = AppSettings.Language == "en" ? 1 : 0;

            // Verificar si el inicio automático está activo en el registro
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    AutoStartCheckBox.IsChecked = (key.GetValue("FoldVision") != null);
                }
            }

            // Ocultar botón de desinstalación si es la versión portable
            string appDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "") ?? "";
            if (!System.IO.File.Exists(System.IO.Path.Combine(appDir, "unins000.exe")))
            {
                UninstallBtn.Visibility = Visibility.Collapsed;
            }

            UpdateLabels();
            _isInitializing = false;
        }

        private void UpdateLabels()
        {
            DepthValue.Text       = $"{AppSettings.CameraDepth:F1}";
            StretchValue.Text     = $"{AppSettings.StretchMultiplier:F1}x";
            BlurValue.Text        = $"{AppSettings.BlurStrength:F2}";
            CornerValue.Text      = $"{AppSettings.CornerRadius:F0}px";
            CornerStartValue.Text = $"{AppSettings.CornerStartRadius:F0}px";
            CornerAnimValue.Text  = $"{AppSettings.CornerAnimRange:P0}";
            OpacityAnimValue.Text = $"{AppSettings.OpacityAnimStart:P0}";
            ClipHeightValue.Text  = $"{AppSettings.ClipHeight:P0}";
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

        private void CornerSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.CornerRadius = (float)e.NewValue;
            if (CornerValue != null) CornerValue.Text = $"{AppSettings.CornerRadius:F0}px";
            AppSettings.Save();
        }

        private void CornerStartSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.CornerStartRadius = (float)e.NewValue;
            if (CornerStartValue != null) CornerStartValue.Text = $"{AppSettings.CornerStartRadius:F0}px";
            AppSettings.Save();
        }

        private void CornerAnimSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.CornerAnimRange = (float)e.NewValue;
            if (CornerAnimValue != null) CornerAnimValue.Text = $"{AppSettings.CornerAnimRange:P0}";
            AppSettings.Save();
        }

        private void OpacityAnimSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.OpacityAnimStart = (float)e.NewValue;
            if (OpacityAnimValue != null) OpacityAnimValue.Text = $"{AppSettings.OpacityAnimStart:P0}";
            AppSettings.Save();
        }

        private void ClipHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            AppSettings.ClipHeight = (float)e.NewValue;
            if (ClipHeightValue != null) ClipHeightValue.Text = $"{AppSettings.ClipHeight:P0}";
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

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            string title = System.Windows.Application.Current.TryFindResource("UninstallConfirmTitle") as string ?? "Confirm Uninstall";
            string message = System.Windows.Application.Current.TryFindResource("UninstallConfirmMessage") as string ?? "Are you sure you want to uninstall the application?";

            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                string appDir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "") ?? "";
                string uninstallerPath = System.IO.Path.Combine(appDir, "unins000.exe");

                if (System.IO.File.Exists(uninstallerPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = uninstallerPath,
                        UseShellExecute = true
                    });
                    System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    System.Windows.MessageBox.Show("Uninstaller not found.", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.LoadDefaults();
            AppSettings.Save();
            
            _isInitializing = true;
            LoadCurrentValues();
            _isInitializing = false;
        }

        private void AutoStart_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
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

        private void TabletMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            AppSettings.DisableInTabletMode = chkTabletMode.IsChecked == true;
            AppSettings.Save();
        }

        private void AppMode_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            AppSettings.AppMode = AppModeCombo.SelectedIndex == 1 ? "Live" : "Static";
            AppSettings.Save();

            if (System.Windows.Application.Current is App app)
            {
                app.SwitchCaptureMode();
            }
        }

        private void Language_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                AppSettings.Language = lang;
                AppSettings.Save();
                App.ChangeLanguage(lang);
            }
        }
    }
}
