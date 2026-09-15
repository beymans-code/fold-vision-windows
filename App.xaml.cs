using WpfApplication     = System.Windows.Application;
using WpfStartupEventArgs = System.Windows.StartupEventArgs;
using WpfExitEventArgs    = System.Windows.ExitEventArgs;
using System.Threading;


namespace FoldVision
{
    public partial class App : WpfApplication
    {
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private OverlayWindow? _overlay;
        private Mutex? _mutex;

        private void Application_Startup(object sender, WpfStartupEventArgs e)
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);
            
            AppSettings.Load();
            ChangeLanguage(AppSettings.Language);

            _mutex = new Mutex(true, "FoldVisionSingleInstanceMutex", out bool createdNew);
            if (!createdNew)
            {
                System.Windows.MessageBox.Show(
                    GetResourceString("AppAlreadyRunningMsg"),
                    GetResourceString("AppAlreadyRunningTitle"),
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                Shutdown();
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                try { System.IO.File.WriteAllText(AppSettings.GetCrashLogPath(), args.ExceptionObject.ToString()); } catch { }
            };

            _overlay = new OverlayWindow { Opacity = 0 };
            _overlay.Show();

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add(GetResourceString("TrayDebugSensor"), null, (s, args) => new DebugWindow().Show());
            menu.Items.Add(GetResourceString("TraySettings"), null, (s, args) => new SettingsWindow().Show());
            menu.Items.Add("-");
            menu.Items.Add(GetResourceString("TrayExit"), null, (s, args) => Shutdown());

            System.Drawing.Icon appIcon;
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("FoldVision.FV.ico"))
            {
                if (stream != null)
                    appIcon = new System.Drawing.Icon(stream);
                else
                    appIcon = System.Drawing.SystemIcons.Application;
            }

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = "FoldVision",
                Icon = appIcon,
                ContextMenuStrip = menu,
                Visible = true
            };

            // Abrir ajustes al hacer doble clic en el ícono
            _notifyIcon.DoubleClick += (s, args) => new SettingsWindow().Show();

            SensorService.Instance.FoldFactorChanged += OnFoldFactorChanged;
            SensorService.Instance.AngleChanged += OnAngleChanged;
            
            bool sensorAvailable = SensorService.Instance.Start();
            if (!sensorAvailable)
            {
                System.Windows.MessageBox.Show(
                    GetResourceString("HardwareIncompatibleMsg"), 
                    GetResourceString("HardwareIncompatibleTitle"), 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
                Shutdown();
                return;
            }
        }

        public static void ChangeLanguage(string langCode)
        {
            var dict = new System.Windows.ResourceDictionary();
            dict.Source = new Uri($"Locales/{langCode}.xaml", UriKind.Relative);

            // Reemplazar solo el diccionario de idioma
            var appDicts = Current.Resources.MergedDictionaries;
            for (int i = appDicts.Count - 1; i >= 0; i--)
            {
                if (appDicts[i].Source != null && appDicts[i].Source.OriginalString.StartsWith("Locales/"))
                {
                    appDicts.RemoveAt(i);
                }
            }
            appDicts.Add(dict);

            if (Current is App appInstance)
            {
                appInstance.UpdateTrayMenuText();
            }
        }

        public void SwitchCaptureMode()
        {
            _overlay?.SwitchCaptureMode();
        }

        public void ForceUpdate()
        {
            _overlay?.ForceRender();
        }

        private void UpdateTrayMenuText()
        {
            if (_notifyIcon?.ContextMenuStrip == null) return;
            var items = _notifyIcon.ContextMenuStrip.Items;
            if (items.Count >= 4)
            {
                items[0].Text = GetResourceString("TrayDebugSensor");
                items[1].Text = GetResourceString("TraySettings");
                items[3].Text = GetResourceString("TrayExit");
            }
        }

        public void RestartEffectService()
        {
            _overlay?.Close();
            _overlay = new OverlayWindow { Opacity = 0 };
            _overlay.Show();
            
            // Re-apply the last known fold factor to the new overlay
            OnFoldFactorChanged(this, _lastFoldFactor);
        }

        public static string GetResourceString(string key)
        {
            return Current.TryFindResource(key) as string ?? key;
        }

        private float _lastFoldFactor = 0f;

        private void OnFoldFactorChanged(object? sender, float foldFactor)
        {
            Dispatcher.Invoke(() =>
            {
                _lastFoldFactor = foldFactor;
                if (_overlay == null) return;

                if (AppSettings.DisableInTabletMode)
                {
                    bool isTabletMode = NativeMethods.GetSystemMetrics(NativeMethods.SM_CONVERTIBLESLATEMODE) == 0;
                    if (isTabletMode) foldFactor = 0f;
                }

                if (foldFactor > 0.001f)
                {
                    if (_overlay.Opacity == 0)
                    {
                        _overlay.PrepareCapture();
                        _overlay.Opacity = 1;
                        
                        // Forzar refresco del Z-Order para cubrir ventanas Picture-in-Picture
                        _overlay.Topmost = false;
                        _overlay.Topmost = true;
                    }
                    _overlay.ApplyEffect(foldFactor);
                }
                else
                {
                    _overlay.ApplyEffect(0f);
                }
            });
        }

        private void OnAngleChanged(object? sender, float angleDegrees)
        {
            Dispatcher.Invoke(() =>
            {
                if (_overlay != null)
                {
                    _overlay.UpdateAngleDisplay(angleDegrees);
                }
            });
        }

        protected override void OnExit(WpfExitEventArgs e)
        {
            SensorService.Instance.FoldFactorChanged -= OnFoldFactorChanged;
            SensorService.Instance.AngleChanged -= OnAngleChanged;
            SensorService.Instance.Stop();
            _notifyIcon?.Dispose();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
