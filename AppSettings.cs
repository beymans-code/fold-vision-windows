using System;
using System.IO;
using System.Text.Json;

namespace FoldVision
{
    /// <summary>
    /// Configuración global de FoldVision.
    /// Todos los valores son leídos por GpuRenderer y SensorService en cada frame.
    /// </summary>
    public static class AppSettings
    {
        // ── Efecto visual ───────────────────────────────────────────────
        /// <summary>Distancia de cámara para la perspectiva 3D. Mayor = menos perspectiva.</summary>
        public static float CameraDepth       = 3.2f;   // rango: 0.5 – 5.0

        /// <summary>Multiplicador del estiramiento vertical máximo.</summary>
        public static float StretchMultiplier = 0.6f;   // rango: 0.0 – 4.0

        /// <summary>Intensidad del desenfoque. 1.0 = valor por defecto.</summary>
        public static float BlurStrength      = 0.50f;  // rango: 0.0 – 2.5

        // ── Rango de activación (sensor) ────────────────────────────────
        /// <summary>Ángulo a partir del cual empieza el efecto (grados).</summary>
        public static float AngleStart        = 110f;   // rango: 60 – 130

        /// <summary>Ángulo en el que el efecto alcanza su máximo (grados).</summary>
        public static float AngleMax          = 140f;   // rango: 100 – 180

        // ── Depuración ──────────────────────────────────────────────────
        public static bool ShowDebugAngle     = false;
        public static bool DisableInTabletMode = true;
        
        /// <summary>Modo de captura: "Static" (captura única) o "Live" (60 FPS real-time).</summary>
        public static string AppMode          = "Static";

        private static string GetConfigPath()
        {
            // Para hacer la aplicación verdaderamente portable, guardamos los ajustes
            // en la misma carpeta donde se encuentra el ejecutable.
            return Path.Combine(AppContext.BaseDirectory, "settings.json");
        }

        public static void Load()
        {
            string path = GetConfigPath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<SettingsData>(json);
                    if (data != null)
                    {
                        CameraDepth = data.CameraDepth;
                        StretchMultiplier = data.StretchMultiplier;
                        BlurStrength = data.BlurStrength;
                        AngleStart = data.AngleStart;
                        AngleMax = data.AngleMax;
                        ShowDebugAngle = data.ShowDebugAngle;
                        DisableInTabletMode = data.DisableInTabletMode;
                        AppMode = data.AppMode ?? "Static";
                    }
                }
                catch { /* Si hay error, se quedan los por defecto */ }
            }
        }

        public static void Save()
        {
            try
            {
                var data = new SettingsData
                {
                    CameraDepth = CameraDepth,
                    StretchMultiplier = StretchMultiplier,
                    BlurStrength = BlurStrength,
                    AngleStart = AngleStart,
                    AngleMax = AngleMax,
                    ShowDebugAngle = ShowDebugAngle,
                    DisableInTabletMode = DisableInTabletMode,
                    AppMode = AppMode
                };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetConfigPath(), json);
            }
            catch { }
        }
    }

    public class SettingsData
    {
        public float CameraDepth { get; set; } = 3.2f;
        public float StretchMultiplier { get; set; } = 0.6f;
        public float BlurStrength { get; set; } = 0.50f;
        public float AngleStart { get; set; } = 110f;
        public float AngleMax { get; set; } = 140f;
        public bool ShowDebugAngle { get; set; } = false;
        public bool DisableInTabletMode { get; set; } = true;
        public string AppMode { get; set; } = "Static";
    }
}
