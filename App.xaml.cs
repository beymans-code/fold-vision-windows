using WpfApplication     = System.Windows.Application;
using WpfStartupEventArgs = System.Windows.StartupEventArgs;
using WpfExitEventArgs    = System.Windows.ExitEventArgs;

namespace FoldVision
{
    public partial class App : WpfApplication
    {
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private OverlayWindow? _overlay;

        private void Application_Startup(object sender, WpfStartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                System.IO.File.WriteAllText("crash.log", args.ExceptionObject.ToString());

            AppSettings.Load();
            ChangeLanguage(AppSettings.Language);

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

            // Reemplazar el diccionario actual
            var appDicts = Current.Resources.MergedDictionaries;
            if (appDicts.Count > 0)
            {
                appDicts.Clear();
            }
            appDicts.Add(dict);
        }

        public static string GetResourceString(string key)
        {
            return Current.TryFindResource(key) as string ?? key;
        }

        private void OnFoldFactorChanged(object? sender, float foldFactor)
        {
            Dispatcher.Invoke(() =>
            {
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
            base.OnExit(e);
        }
    }
}
