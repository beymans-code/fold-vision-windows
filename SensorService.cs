using System;
using System.Windows.Threading;
using Windows.Devices.Sensors;

namespace FoldVision
{
    /// <summary>
    /// Gestiona la detección del ángulo de la tapa usando el sensor Inclinómetro.
    /// (Una fusión de giroscopios y acelerómetros físicos que Windows usa para calcular 
    /// el ángulo exacto de la bisagra en grados).
    /// Si el dispositivo no tiene este hardware físico, la app informará que no es compatible.
    ///
    /// Expone FoldFactorChanged con valores normalizados 0.0 (abierto) a 1.0 (cerrado).
    /// </summary>
    public class SensorService
    {
        public static SensorService Instance { get; } = new SensorService();

        private Inclinometer? _inclinometer;

        /// <summary>Factor de pliegue normalizado: 0.0 = tapa abierta, 1.0 = tapa cerrada.</summary>
        public event EventHandler<float>? FoldFactorChanged;

        /// <summary>Ángulo crudo en grados de la bisagra, útil para OSD/Debug.</summary>
        public event EventHandler<float>? AngleChanged;

        public bool Start()
        {
            try
            {
                _inclinometer = Inclinometer.GetDefault();
                if (_inclinometer != null)
                {
                    _inclinometer.ReportInterval = Math.Max(_inclinometer.MinimumReportInterval, 100);
                    _inclinometer.ReadingChanged += Inclinometer_ReadingChanged;
                    return true;
                }
            }
            catch { }

            return false;
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
        }
    }
}
