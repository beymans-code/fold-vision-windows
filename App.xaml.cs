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

            _overlay = new OverlayWindow { Opacity = 0 };
            _overlay.Show();

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("🔧 Debug Sensor", null, (s, args) => new DebugWindow().Show());
            menu.Items.Add("⚙️ Ajustes del efecto", null, (s, args) => new SettingsWindow().Show());
            menu.Items.Add("-");
            menu.Items.Add("❌ Salir", null, (s, args) => Shutdown());

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = "FoldVision",
                Icon = System.Drawing.SystemIcons.Application,
                ContextMenuStrip = menu,
                Visible = true
            };

            // Abrir ajustes al hacer doble clic en el ícono
            _notifyIcon.DoubleClick += (s, args) => new SettingsWindow().Show();

            SensorService.Instance.FoldFactorChanged += OnFoldFactorChanged;
            SensorService.Instance.AngleChanged += OnAngleChanged;
            SensorService.Instance.Start();
        }

        private void OnFoldFactorChanged(object? sender, float foldFactor)
        {
            Dispatcher.Invoke(() =>
            {
                if (_overlay == null) return;

                if (foldFactor > 0.001f)
                {
                    if (_overlay.Opacity == 0)
                    {
                        _overlay.PrepareCapture();
                        _overlay.Opacity = 1;
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
