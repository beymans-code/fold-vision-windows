<div align="center">
  <img src="FV.svg" width="128" alt="FoldVision Logo">
  <h1>FoldVision ✨</h1>
  <a href="https://github.com/beymans-code/fold-vision-windows/releases">
    <img src="https://img.shields.io/github/downloads/beymans-code/fold-vision-windows/total?style=for-the-badge&color=blue" alt="Downloads">
  </a>
</div>

<br>

| 🇨🇴 🇪🇸 Español (Spanish) | 🇬🇧 🇺🇸 English (Inglés) |
| --- | --- |
| **FoldVision** Lleva la experiencia de pliegue inmersivo del iPhone Duo a la pantalla de tu laptop con Windows.<br><br>⚠️ **Requisito:** Funciona *exclusivamente* en portátiles equipados con un **Inclinómetro** por hardware (ej. Microsoft Surface Laptop Studio o Lenovo Yoga Book). *Este sensor combina datos de giroscopios y acelerómetros físicos, permitiendo a Windows calcular el ángulo exacto de la bisagra.*<br><br>| **FoldVision** Bring the immersive folding experience of the iPhone Duo to your Windows laptop screen.<br><br>⚠️ **Requirement:** Works *exclusively* on laptops equipped with a hardware **Inclinometer** (e.g., Microsoft Surface Laptop Studio or Lenovo Yoga Book). *This sensor combines data from physical gyroscopes and accelerometers, allowing Windows to calculate the exact hinge angle.*<br><br>

<details name="docs">
<summary><b>🇨🇴 🇪🇸 Documentación en Español</b></summary>
<br>

### 🚀 Características Principales
- **Efecto D3D11 Acelerado por Hardware:** Renderizado ultrarrápido a través de GPU que garantiza 0 latencia y no consume casi recursos del procesador (CPU).
- **Sensor Inteligente Integrado:** Detecta automáticamente el grado de inclinación de la bisagra nativamente mediante el Inclinómetro de Windows (Hardware requerido).
- **Dos modos de funcionamiento:**
  - **Modo Estático (Bajo consumo)**
  - **Modo Live (Tiempo real - Overlay)**
- **Personalizable:** Incluye una interfaz (accesible mediante el icono oculto de la barra de tareas) para personalizar el desenfoque (blur), profundidad, sombras y límites de activación.

### ⚙️ Cómo empezar
Puedes descargar el instalador listo para usar desde la [sección de Releases en GitHub](https://github.com/beymans-code/fold-vision-windows/releases). *(Importante: Al ser un instalador (.exe) sin firma digital, el Control Inteligente de Aplicaciones o SmartScreen de Windows podría mostrar una advertencia. Para evitarlo, haz **clic derecho** en el archivo descargado > **Propiedades** > marca la casilla **Desbloquear** en la parte inferior y haz clic en Aplicar antes de ejecutarlo).*

Si prefieres descargar el código fuente y compilarlo tú mismo, es muy fácil:
1. Asegúrate de tener instalado el SDK de **.NET 8**.
2. Haz doble clic en el script `build_release.bat`.
3. Esto generará la carpeta `publish\FoldVision\` con los archivos compilados.
4. Puedes ejecutar `FoldVision.exe` directamente desde ahí, o usar Inno Setup con el archivo `installer.iss` para crear tu propio instalador.
5. Al abrir `FoldVision.exe`, el programa se ocultará en la barra de tareas (abajo a la derecha). Haz doble clic en el icono <img src="FV.svg" width="20" valign="bottom"> `FoldVision` para abrir los ajustes.

### 📜 Licencia
Este proyecto es completamente de código abierto y se distribuye bajo la **Licencia Pública General GNU (GPLv3)**. Tienes total libertad para estudiarlo, modificarlo y compartir tus mejoras. Consulta el archivo `LICENSE` adjunto para más información.

---
</details>


<details name="docs">
<summary><b>🇬🇧 🇺🇸 Documentation in English</b></summary>
<br>

### 🚀 Key Features
- **Hardware-Accelerated D3D11 Effect:** Blazing-fast GPU rendering that ensures 0 latency with almost zero CPU overhead.
- **Smart Built-in Sensor:** Automatically detects the exact tilt angle of the hinge natively using the Windows Inclinometer (Hardware required).
- **Two operating modes:**
  - **Static Mode (Low power)**
  - **Live Mode (Real-time - Overlay)**
- **Customizable:** Comes with a GUI (accessible from the system tray) to tweak the blur strength, depth, shadowing, and activation limits.

### ⚙️ Getting Started
You can download the latest ready-to-use installer from the [GitHub Releases section](https://github.com/beymans-code/fold-vision-windows/releases). *(Important: Since this is an unsigned installer (.exe) file, Windows SmartScreen or Smart App Control might show a warning. To prevent this, **right-click** the downloaded file > **Properties** > check the **Unblock** box at the bottom and click Apply before running it).*

If you prefer to download the source code and build it yourself, it's very straightforward:
1. Make sure you have the **.NET 8** SDK installed.
2. Double-click the `build_release.bat` script.
3. This will generate a `publish\FoldVision\` folder containing the compiled files.
4. You can run `FoldVision.exe` directly from there, or use Inno Setup with the `installer.iss` file to build your own installer.
5. When you open `FoldVision.exe`, it will hide in your system tray (bottom right corner). Double-click the <img src="FV.svg" width="20" valign="bottom"> `FoldVision` icon to open the settings.

### 📜 License
This project is completely open source and is distributed under the **GNU General Public License (GPLv3)**. You have absolute freedom to study, modify, and share your improvements. Please see the attached `LICENSE` file for more details.

</details>
