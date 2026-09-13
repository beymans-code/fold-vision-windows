using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace FoldVision
{
    /// <summary>
    /// Detecta el estado físico de la tapa de la laptop usando la API nativa de Windows:
    /// WM_POWERBROADCAST + GUID_LIDSWITCH_STATE_CHANGE.
    ///
    /// Cuando detecta cierre de tapa, anima el foldFactor de 0→1 con un DispatcherTimer
    /// (porque no tenemos el ángulo exacto sin Inclinómetro hardware).
    /// Cuando detecta apertura, anima 1→0 rápidamente.
    /// </summary>
    public class LidMonitor : IDisposable
    {
        private HwndSource? _msgOnlyWindow;
        private IntPtr      _powerNotifyHandle = IntPtr.Zero;
        private DispatcherTimer? _animTimer;

        private float _currentFactor = 0f;
        private float _targetFactor  = 0f;

        // Velocidad de animación: cuánto cambia el factor por tick (cada 16ms ≈ 60fps)
        private const float CloseSpeed = 0.012f;  // ~2 segundos para cerrar
        private const float OpenSpeed  = 0.08f;   // ~0.2 segundos para abrir

        /// <summary>
        /// Se dispara cada vez que cambia el factor de pliegue (0.0 = abierto, 1.0 = cerrado).
        /// </summary>
        public event EventHandler<float>? FoldFactorChanged;

        public void Start()
        {
            // Crear una ventana solo de mensajes (HWND_MESSAGE) para recibir WM_POWERBROADCAST
            var parameters = new HwndSourceParameters("FoldVision_LidMonitor")
            {
                WindowStyle         = 0,
                ExtendedWindowStyle = 0,
                ParentWindow        = new IntPtr(-3) // HWND_MESSAGE
            };

            _msgOnlyWindow = new HwndSource(parameters);
            _msgOnlyWindow.AddHook(WndProc);

            // Registrar para notificaciones de cambio de estado de tapa
            var guid = NativeMethods.GUID_LIDSWITCH_STATE_CHANGE;
            _powerNotifyHandle = NativeMethods.RegisterPowerSettingNotification(
                _msgOnlyWindow.Handle, ref guid, 0);

            // Timer de animación a ~60fps
            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _animTimer.Tick += AnimTimer_Tick;
            _animTimer.Start();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam,
                               ref bool handled)
        {
            if (msg == NativeMethods.WM_POWERBROADCAST &&
                wParam.ToInt32() == NativeMethods.PBT_POWERSETTINGCHANGE)
            {
                var setting = Marshal.PtrToStructure<NativeMethods.POWERBROADCAST_SETTING>(lParam);
                if (setting.PowerSetting == NativeMethods.GUID_LIDSWITCH_STATE_CHANGE)
                {
                    // NOTA: La documentación de Windows dice Data=1 → abierto, Data=0 → cerrado,
                    // pero en muchos OEM ocurre lo contrario. Si el efecto se ejecuta al abrir,
                    // el valor está invertido. Usamos Data==0 → tapa abierta (sin efecto).
                    bool lidOpen = setting.Data == 0;
                    _targetFactor = lidOpen ? 0f : 1f;
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void AnimTimer_Tick(object? sender, EventArgs e)
        {
            if (Math.Abs(_currentFactor - _targetFactor) < 0.001f) return;

            float speed = _targetFactor > _currentFactor ? CloseSpeed : OpenSpeed;
            _currentFactor = _targetFactor > _currentFactor
                ? Math.Min(_currentFactor + speed, _targetFactor)
                : Math.Max(_currentFactor - speed, _targetFactor);

            FoldFactorChanged?.Invoke(this, _currentFactor);
        }

        public void Dispose()
        {
            _animTimer?.Stop();
            if (_powerNotifyHandle != IntPtr.Zero)
                NativeMethods.UnregisterPowerSettingNotification(_powerNotifyHandle);
            _msgOnlyWindow?.Dispose();
        }
    }
}
