using System;
using System.Windows.Threading;
using Windows.Devices.Sensors;

namespace FoldVision
{
    /// <summary>
    /// Orquesta la detección del ángulo de la tapa usando dos estrategias en cascada:
    ///  1. Windows.Devices.Sensors.Inclinometer (si el hardware lo expone — tablets, 2-en-1)
    ///  2. LidMonitor via WM_POWERBROADCAST (funciona en cualquier laptop Windows 10/11)
    ///
    /// Expone FoldFactorChanged con valores normalizados 0.0 (abierto) a 1.0 (cerrado).
    /// </summary>
    public class SensorService
    {
        public static SensorService Instance { get; } = new SensorService();

        private Inclinometer? _inclinometer;
        private LidMonitor?   _lidMonitor;

        /// <summary>Factor de pliegue normalizado: 0.0 = tapa abierta, 1.0 = tapa cerrada.</summary>
        public event EventHandler<float>? FoldFactorChanged;

        /// <summary>Ángulo crudo en grados de la bisagra, útil para OSD/Debug.</summary>
        public event EventHandler<float>? AngleChanged;

        public void Start()
        {
            // Estrategia 1: Inclinómetro hardware (solo disponible en algunos 2-en-1 / tablets)
            try
            {
                _inclinometer = Inclinometer.GetDefault();
                if (_inclinometer != null)
                {
                    _inclinometer.ReportInterval =
                        Math.Max(_inclinometer.MinimumReportInterval, 100); // max 10fps
                    _inclinometer.ReadingChanged += Inclinometer_ReadingChanged;
                    return; // Tenemos sensor real, no necesitamos el fallback
                }
            }
            catch { /* El sensor no está disponible en este dispositivo */ }

            // Estrategia 2 (Fallback): Detección binaria de tapa via WM_POWERBROADCAST
            _lidMonitor = new LidMonitor();
            _lidMonitor.FoldFactorChanged += (s, factor) => {
                FoldFactorChanged?.Invoke(this, factor);
                // Si es fallback binario, simulamos el ángulo (180 o 0)
                AngleChanged?.Invoke(this, factor > 0.5f ? 180f : 0f);
            };
            _lidMonitor.Start();
        }

        private void Inclinometer_ReadingChanged(Inclinometer sender,
                                                  InclinometerReadingChangedEventArgs args)
        {
            // Pitch ≈ ángulo de la bisagra. Varía entre dispositivos; usamos el rango 130°→10°.
            float pitch  = args.Reading.PitchDegrees;
            float factor = CalculateFoldFactor(pitch);
            FoldFactorChanged?.Invoke(this, factor);
            AngleChanged?.Invoke(this, pitch);
        }

        /// <summary>
        /// Permite simular el ángulo manualmente desde la ventana de depuración.
        /// </summary>
        public void SetDebugAngle(float angleDegrees)
        {
            FoldFactorChanged?.Invoke(this, CalculateFoldFactor(angleDegrees));
            AngleChanged?.Invoke(this, angleDegrees);
        }

        private static float CalculateFoldFactor(float angle)
        {
            float start = AppSettings.AngleStart;
            float max   = AppSettings.AngleMax;
            if (angle <= start) return 0f;
            return Math.Clamp((angle - start) / (max - start), 0f, 1f);
        }

        public void Stop()
        {
            if (_inclinometer != null)
                _inclinometer.ReadingChanged -= Inclinometer_ReadingChanged;
            _lidMonitor?.Dispose();
        }
    }
}
