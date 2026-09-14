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

        /// <summary>Radio en píxeles de las esquinas superiores del clip [0-80].</summary>
        public static float CornerRadius      = 30f;   // rango: 0 – 80
        
        /// <summary>Radio inicial en píxeles de las esquinas superiores del clip [0-80].</summary>
        public static float CornerStartRadius = 10f;   // rango: 0 – 80
        
        /// <summary>Rango de animación del radio de las esquinas (fracción de la apertura).</summary>
        public static float CornerAnimRange   = 0.20f; // rango: 0.01 – 1.0

        /// <summary>Progreso (0-1) a partir del cual la opacidad empieza a desvanecerse.</summary>
        public static float OpacityAnimStart  = 0.10f; // rango: 0.0 – 1.0

        /// <summary>Altura máxima de la máscara clip [0.0 = sin máscara … 1.0 = pantalla completa].</summary>
        public static float ClipHeight        = 1.0f;  // rango: 0.0 – 1.0

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

        /// <summary>Idioma de la aplicación: "es" o "en".</summary>
        public static string Language         = "es";

        public static string GetAppDir()
        {
            string baseDir = AppContext.BaseDirectory;
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (baseDir.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) ||
                baseDir.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase))
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configDir = Path.Combine(appData, "FoldVision");
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                return configDir;
            }

            return baseDir;
        }

        private static string GetConfigPath()
        {
            return Path.Combine(GetAppDir(), "settings.json");
        }

        public static string GetCrashLogPath()
        {
            return Path.Combine(GetAppDir(), "crash.log");
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
                    if (data != null) ApplyData(data);
                }
                catch { LoadDefaults(); }
            }
            else
            {
                LoadDefaults();
            }
        }

        public static void LoadDefaults()
        {
            try
            {
                using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("FoldVision.default_settings.json");
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();
                    var data = JsonSerializer.Deserialize<SettingsData>(json);
                    if (data != null) ApplyData(data);
                }
            }
            catch { }
        }

        private static void ApplyData(SettingsData data)
        {
            CornerRadius        = data.CornerRadius;
            CornerStartRadius   = data.CornerStartRadius;
            ClipHeight          = data.ClipHeight;
            CornerAnimRange     = data.CornerAnimRange;
            OpacityAnimStart    = data.OpacityAnimStart;
            CameraDepth         = data.CameraDepth;
            StretchMultiplier   = data.StretchMultiplier;
            BlurStrength        = data.BlurStrength;
            AngleStart          = data.AngleStart;
            AngleMax            = data.AngleMax;
            ShowDebugAngle      = data.ShowDebugAngle;
            DisableInTabletMode = data.DisableInTabletMode;
            AppMode             = data.AppMode ?? "Static";
            Language            = data.Language ?? "es";
        }

        public static void Save()
        {
            try
            {
                var data = new SettingsData
                {
                    CornerRadius      = CornerRadius,
                    CornerStartRadius = CornerStartRadius,
                    ClipHeight        = ClipHeight,
                    CornerAnimRange   = CornerAnimRange,
                    OpacityAnimStart  = OpacityAnimStart,
                    CameraDepth       = CameraDepth,
                    StretchMultiplier = StretchMultiplier,
                    BlurStrength = BlurStrength,
                    AngleStart = AngleStart,
                    AngleMax = AngleMax,
                    ShowDebugAngle = ShowDebugAngle,
                    DisableInTabletMode = DisableInTabletMode,
                    AppMode = AppMode,
                    Language = Language
                };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetConfigPath(), json);
            }
            catch { }
        }
    }

    public class SettingsData
    {
        public float CornerRadius      { get; set; }
        public float CornerStartRadius { get; set; }
        public float CornerAnimRange   { get; set; }
        public float OpacityAnimStart  { get; set; }
        public float ClipHeight        { get; set; }
        public float CameraDepth       { get; set; }
        public float StretchMultiplier { get; set; }
        public float BlurStrength      { get; set; }
        public float AngleStart        { get; set; }
        public float AngleMax          { get; set; }
        public bool ShowDebugAngle     { get; set; }
        public bool DisableInTabletMode{ get; set; }
        public string AppMode          { get; set; } = "Static";
        public string Language         { get; set; } = "es";
    }
}
