using System;
using System.Collections.Generic;
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
        private class AppUIState
        {
            public float CameraDepth { get; set; }
            public float StretchMultiplier { get; set; }
            public float BlurStrength { get; set; }
            public float CornerRadius { get; set; }
            public float CornerStartRadius { get; set; }
            public float CornerAnimRange { get; set; }
            public float OpacityAnimStart { get; set; }
            public float ClipHeight { get; set; }
            public float AngleStart { get; set; }
            public float AngleMax { get; set; }
            public bool ShowDebugAngle { get; set; }
            public bool DisableInTabletMode { get; set; }
            public string AppMode { get; set; } = string.Empty;
            public string Language { get; set; } = string.Empty;
            public bool AutoStart { get; set; }
        }

        private bool _isInitializing = true;
        private bool _isRestoring = false;
        private List<AppUIState> _history = new List<AppUIState>();
        private int _historyIndex = -1;
        private AppUIState _originalState = null!;
        private System.Windows.Threading.DispatcherTimer _debounceTimer;

        public SettingsWindow()
        {
            InitializeComponent();

            _debounceTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                PushHistoryState();
            };

            LoadCurrentValues();
        }

        private void LoadCurrentValues()
        {
            _isInitializing = true;

            DepthSlider.Value = AppSettings.CameraDepth;
            StretchSlider.Value = AppSettings.StretchMultiplier;
            BlurSlider.Value = AppSettings.BlurStrength;
            CornerSlider.Value = AppSettings.CornerRadius;
            CornerStartSlider.Value = AppSettings.CornerStartRadius;
            CornerAnimSlider.Value = AppSettings.CornerAnimRange;
            OpacityAnimSlider.Value = AppSettings.OpacityAnimStart;
            ClipHeightSlider.Value = AppSettings.ClipHeight;
            AngleStartSlider.Value = AppSettings.AngleStart;
            AngleMaxSlider.Value = AppSettings.AngleMax;
            ShowDebugCheckBox.IsChecked = AppSettings.ShowDebugAngle;
            chkTabletMode.IsChecked = AppSettings.DisableInTabletMode;
            AppModeCombo.SelectedIndex = AppSettings.AppMode == "Live" ? 1 : 0;

            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if (item.Tag is string lang && lang == AppSettings.Language)
                {
                    LanguageCombo.SelectedItem = item;
                    break;
                }
            }

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

            _originalState = CaptureUIState();

            _history.Clear();
            _history.Add(CaptureUIState());
            _historyIndex = 0;

            UpdateLabelsFromState(_originalState);
            UpdateFABVisibility();

            _isInitializing = false;
        }

        private AppUIState CaptureUIState()
        {
            return new AppUIState
            {
                CameraDepth = (float)DepthSlider.Value,
                StretchMultiplier = (float)StretchSlider.Value,
                BlurStrength = (float)BlurSlider.Value,
                CornerRadius = (float)CornerSlider.Value,
                CornerStartRadius = (float)CornerStartSlider.Value,
                CornerAnimRange = (float)CornerAnimSlider.Value,
                OpacityAnimStart = (float)OpacityAnimSlider.Value,
                ClipHeight = (float)ClipHeightSlider.Value,
                AngleStart = (float)AngleStartSlider.Value,
                AngleMax = (float)AngleMaxSlider.Value,
                ShowDebugAngle = ShowDebugCheckBox.IsChecked == true,
                DisableInTabletMode = chkTabletMode.IsChecked == true,
                AppMode = AppModeCombo.SelectedIndex == 1 ? "Live" : "Static",
                Language = LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string lang ? lang : "es",
                AutoStart = AutoStartCheckBox.IsChecked == true
            };
        }

        private void RestoreUIState(AppUIState state)
        {
            _isRestoring = true;

            DepthSlider.Value = state.CameraDepth;
            StretchSlider.Value = state.StretchMultiplier;
            BlurSlider.Value = state.BlurStrength;
            CornerSlider.Value = state.CornerRadius;
            CornerStartSlider.Value = state.CornerStartRadius;
            CornerAnimSlider.Value = state.CornerAnimRange;
            OpacityAnimSlider.Value = state.OpacityAnimStart;
            ClipHeightSlider.Value = state.ClipHeight;
            AngleStartSlider.Value = state.AngleStart;
            AngleMaxSlider.Value = state.AngleMax;
            ShowDebugCheckBox.IsChecked = state.ShowDebugAngle;
            chkTabletMode.IsChecked = state.DisableInTabletMode;
            AppModeCombo.SelectedIndex = state.AppMode == "Live" ? 1 : 0;

            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if (item.Tag is string lang && lang == state.Language)
                {
                    LanguageCombo.SelectedItem = item;
                    break;
                }
            }

            AutoStartCheckBox.IsChecked = state.AutoStart;

            UpdateLabelsFromState(state);

            _isRestoring = false;
        }

        private void UpdateLabelsFromState(AppUIState state)
        {
            if (DepthValue != null) DepthValue.Text = $"{state.CameraDepth:F1}";
            if (StretchValue != null) StretchValue.Text = $"{state.StretchMultiplier:F1}x";
            if (BlurValue != null) BlurValue.Text = $"{state.BlurStrength:F2}";
            if (CornerValue != null) CornerValue.Text = $"{state.CornerRadius:F0}px";
            if (CornerStartValue != null) CornerStartValue.Text = $"{state.CornerStartRadius:F0}px";
            if (CornerAnimValue != null) CornerAnimValue.Text = $"{state.CornerAnimRange:P0}";
            if (OpacityAnimValue != null) OpacityAnimValue.Text = $"{state.OpacityAnimStart:P0}";
            if (ClipHeightValue != null) ClipHeightValue.Text = $"{state.ClipHeight:P0}";
            if (AngleStartValue != null) AngleStartValue.Text = $"{state.AngleStart:F0}°";
            if (AngleMaxValue != null) AngleMaxValue.Text = $"{state.AngleMax:F0}°";
        }

        private void RegisterChange()
        {
            if (_isInitializing || _isRestoring) return;

            UpdateFABVisibility();

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void PushHistoryState()
        {
            var newState = CaptureUIState();

            // Si no estamos en el final del historial, truncar
            if (_historyIndex < _history.Count - 1)
            {
                _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
            }

            _history.Add(newState);
            _historyIndex++;
            UpdateFABVisibility();
        }

        private bool AreStatesEqual(AppUIState a, AppUIState b)
        {
            string jsonA = System.Text.Json.JsonSerializer.Serialize(a);
            string jsonB = System.Text.Json.JsonSerializer.Serialize(b);
            return jsonA == jsonB;
        }

        private void UpdateFABVisibility()
        {
            if (BtnApply == null) return;
            var current = CaptureUIState();
            bool isDirty = !AreStatesEqual(current, _originalState);

            BtnApply.IsEnabled = isDirty;
            BtnUndo.IsEnabled = _historyIndex > 0;
            BtnRedo.IsEnabled = _historyIndex < _history.Count - 1;
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            var state = CaptureUIState();

            bool modeChanged = AppSettings.AppMode != state.AppMode;
            bool langChanged = AppSettings.Language != state.Language;

            AppSettings.CornerRadius = state.CornerRadius;
            AppSettings.CornerStartRadius = state.CornerStartRadius;
            AppSettings.ClipHeight = state.ClipHeight;
            AppSettings.CornerAnimRange = state.CornerAnimRange;
            AppSettings.OpacityAnimStart = state.OpacityAnimStart;
            AppSettings.CameraDepth = state.CameraDepth;
            AppSettings.StretchMultiplier = state.StretchMultiplier;
            AppSettings.BlurStrength = state.BlurStrength;
            AppSettings.AngleStart = state.AngleStart;
            AppSettings.AngleMax = state.AngleMax;
            AppSettings.ShowDebugAngle = state.ShowDebugAngle;
            AppSettings.DisableInTabletMode = state.DisableInTabletMode;
            AppSettings.AppMode = state.AppMode;
            AppSettings.Language = state.Language;

            AppSettings.Save();

            // AutoStart
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    if (state.AutoStart)
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

            if (langChanged)
            {
                App.ChangeLanguage(state.Language);
            }

            if (System.Windows.Application.Current is App app)
            {
                app.ForceUpdate();
                if (modeChanged)
                {
                    app.SwitchCaptureMode();
                }
            }

            _originalState = CaptureUIState();
            UpdateFABVisibility();
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                RestoreUIState(_history[_historyIndex]);
                UpdateFABVisibility();
            }
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            if (_historyIndex < _history.Count - 1)
            {
                _historyIndex++;
                RestoreUIState(_history[_historyIndex]);
                UpdateFABVisibility();
            }
        }

        private void DepthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            if (DepthValue != null) DepthValue.Text = $"{e.NewValue:F1}";
            RegisterChange();
        }

        private void StretchSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            if (StretchValue != null) StretchValue.Text = $"{e.NewValue:F1}x";
            RegisterChange();
        }

        private void BlurSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            if (BlurValue != null) BlurValue.Text = $"{e.NewValue:F2}";
            RegisterChange();
        }

        private void CornerSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            if (CornerValue != null) CornerValue.Text = $"{e.NewValue:F0}px";
            RegisterChange();
        }

        private void CornerStartSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            if (CornerStartValue != null) CornerStartValue.Text = $"{e.NewValue:F0}px";
            RegisterChange();
        }

        private void CornerAnimSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            if (CornerAnimValue != null) CornerAnimValue.Text = $"{e.NewValue:P0}";
            RegisterChange();
        }

        private void OpacityAnimSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            if (OpacityAnimValue != null) OpacityAnimValue.Text = $"{e.NewValue:P0}";
            RegisterChange();
        }

        private void ClipHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            if (ClipHeightValue != null) ClipHeightValue.Text = $"{e.NewValue:P0}";
            RegisterChange();
        }

        private void AngleStart_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            float newStart = (float)e.NewValue;
            float currentMax = (float)AngleMaxSlider.Value;

            // Garantizar que AngleStart < AngleMax
            if (newStart >= currentMax)
            {
                AngleMaxSlider.Value = newStart + 5f;
            }
            if (AngleStartValue != null) AngleStartValue.Text = $"{newStart:F0}°";
            RegisterChange();
        }

        private void AngleMax_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing || _isRestoring) return;
            float newMax = (float)e.NewValue;
            float currentStart = (float)AngleStartSlider.Value;

            // Garantizar que AngleMax > AngleStart
            if (newMax <= currentStart)
            {
                AngleStartSlider.Value = newMax - 5f;
            }
            if (AngleMaxValue != null) AngleMaxValue.Text = $"{newMax:F0}°";
            RegisterChange();
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
            try
            {
                using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("FoldVision.default_settings.json");
                if (stream != null)
                {
                    using var reader = new System.IO.StreamReader(stream);
                    string json = reader.ReadToEnd();
                    var data = System.Text.Json.JsonSerializer.Deserialize<SettingsData>(json);
                    if (data != null)
                    {
                        var state = CaptureUIState();
                        state.CameraDepth = data.CameraDepth;
                        state.StretchMultiplier = data.StretchMultiplier;
                        state.BlurStrength = data.BlurStrength;
                        state.CornerRadius = data.CornerRadius;
                        state.CornerStartRadius = data.CornerStartRadius;
                        state.CornerAnimRange = data.CornerAnimRange;
                        state.OpacityAnimStart = data.OpacityAnimStart;
                        state.ClipHeight = data.ClipHeight;
                        state.AngleStart = data.AngleStart;
                        state.AngleMax = data.AngleMax;
                        state.ShowDebugAngle = data.ShowDebugAngle;
                        state.DisableInTabletMode = data.DisableInTabletMode;
                        state.AppMode = data.AppMode ?? "Static";
                        state.Language = data.Language ?? "es";

                        RestoreUIState(state);
                        RegisterChange();
                    }
                }
            }
            catch { }
        }

        private void AutoStart_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isRestoring) return;
            RegisterChange();
        }

        private void ShowDebug_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isRestoring) return;
            RegisterChange();
        }

        private void TabletMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _isRestoring) return;
            RegisterChange();
        }

        private void AppMode_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing || _isRestoring) return;
            RegisterChange();
        }

        private void Language_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing || _isRestoring) return;
            RegisterChange();
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - (e.Delta / 6.0));
            e.Handled = true;
        }
    }
}
