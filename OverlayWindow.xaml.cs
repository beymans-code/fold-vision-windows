using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Vortice.Direct3D11;

namespace FoldVision
{
    public partial class OverlayWindow : Window
    {
        // ── Servicios GPU ────────────────────────────────────────────────
        private GpuRenderer?           _renderer;
        private StaticCaptureService?  _staticCapture;
        private LiveCaptureService?    _liveCapture;
        private D3DImage?              _d3dImage;

        // ── Motor de animación ───────────────────────────────────────────
        private DispatcherTimer? _animTimer;
        private float _targetFold   = 0f;
        private float _curTurn      = 0f;
        private float _prevTurn     = 0f;
        private float _turnVelocity = 0f;

        private const float SpringTension = 250f;
        private const float SpringDamping = 30f;

        // ─────────────────────────────────────────────────────────────────
        public OverlayWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Clicks pasan al escritorio
            var hwnd    = new WindowInteropHelper(this).Handle;
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                exStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);

            // Evitar que nuestra ventana sea capturada por DXGI (Droste effect)
            NativeMethods.SetWindowDisplayAffinity(hwnd, NativeMethods.WDA_EXCLUDEFROMCAPTURE);

            InitGpu();
            StartAnimTimer();
        }

        // ── Inicialización GPU ───────────────────────────────────────────

        private void InitGpu()
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            int w = NativeMethods.GetDeviceCaps(hdc, NativeMethods.DESKTOPHORZRES);
            int h = NativeMethods.GetDeviceCaps(hdc, NativeMethods.DESKTOPVERTRES);
            NativeMethods.ReleaseDC(IntPtr.Zero, hdc);

            _renderer = new GpuRenderer(w, h);
            
            if (AppSettings.AppMode == "Live")
            {
                _liveCapture = new LiveCaptureService(_renderer.Device);
            }
            else
            {
                _staticCapture = new StaticCaptureService(_renderer.Device);
            }

            _d3dImage = new D3DImage();
            _d3dImage.Lock();
            _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9,
                GetSharedSurface(_renderer.RenderTarget!));
            _d3dImage.Unlock();

            GpuImage.Source = _d3dImage;
        }

        private IntPtr GetSharedSurface(ID3D11Texture2D rt)
        {
            var resource    = rt.QueryInterface<Vortice.DXGI.IDXGIResource>();
            var sharedHandle = resource.SharedHandle;
            resource.Dispose();
            return D3D9Interop.OpenSharedSurface(
                (int)rt.Description.Width,
                (int)rt.Description.Height,
                sharedHandle);
        }

        // ── Bucle de animación 60 fps ────────────────────────────────────

        private void StartAnimTimer()
        {
            _animTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _animTimer.Tick += OnAnimTick;
            _animTimer.Start();
        }

        private void OnAnimTick(object? sender, EventArgs e)
        {
            if (_renderer == null || _d3dImage == null) return;

            _prevTurn = _curTurn;

            // Spring physics
            float dt          = 16f / 1000f;
            float springForce = SpringTension * (_targetFold - _curTurn) - SpringDamping * _turnVelocity;
            _turnVelocity += springForce * dt;
            _curTurn      += _turnVelocity * dt;

            if (Math.Abs(_targetFold - _curTurn) < 0.0001f && Math.Abs(_turnVelocity) < 0.001f)
            {
                _curTurn      = _targetFold;
                _turnVelocity = 0f;
            }

            // MotionBoost
            float velocity = Math.Abs(_curTurn - _prevTurn) * 60f;
            float boost    = velocity > 0.5f ? Math.Min((velocity - 0.5f) * 2f, 12f) : 0f;

            _renderer.Turn        = _curTurn;
            _renderer.MotionBoost = boost;

            bool isAnimating = Math.Abs(_curTurn - _targetFold) > 0.001f;

            // Lógica Live vs Static
            bool newFrameReady = false;
            ID3D11Texture2D? currentFrame = null;

            if (AppSettings.AppMode == "Live")
            {
                if (_curTurn > 0.01f || isAnimating)
                {
                    _liveCapture?.CaptureScreen();
                }
                newFrameReady = _liveCapture?.NewFrameReady ?? false;
                currentFrame = _liveCapture?.LatestFrame;
                if (_liveCapture != null) _liveCapture.NewFrameReady = false;
            }
            else
            {
                newFrameReady = _staticCapture?.NewFrameReady ?? false;
                currentFrame = _staticCapture?.LatestFrame;
                if (_staticCapture != null) _staticCapture.NewFrameReady = false;
            }

            // Renderizar si hay frame o hay animación en curso
            if ((newFrameReady || isAnimating) && currentFrame != null)
            {
                _renderer.Render(currentFrame);
                _d3dImage.Lock();
                _d3dImage.AddDirtyRect(new Int32Rect(0, 0, _d3dImage.PixelWidth, _d3dImage.PixelHeight));
                _d3dImage.Unlock();
            }

            // Auto-ocultar cuando la animación de apertura termina
            if (!isAnimating && _curTurn == 0f && Opacity > 0)
            {
                Opacity = 0;
                
                if (AppSettings.AppMode == "Static")
                {
                    _staticCapture?.Dispose();
                    _staticCapture = new StaticCaptureService(_renderer.Device);
                }
            }
        }

        // ── API pública ──────────────────────────────────────────────────

        /// <summary>Captura la pantalla y muestra el overlay.</summary>
        public void PrepareCapture()
        {
            if (AppSettings.AppMode == "Static")
            {
                if (_staticCapture != null && _staticCapture.LatestFrame == null)
                    _staticCapture.CaptureScreen();
            }
        }

        /// <summary>Actualiza el factor de pliegue objetivo.</summary>
        public void ApplyEffect(float foldFactor) => _targetFold = foldFactor;

        public void UpdateAngleDisplay(float angleDegrees)
        {
            if (AppSettings.ShowDebugAngle)
            {
                DebugText.Text = $"{angleDegrees:F1}°";
                DebugText.Visibility = Visibility.Visible;
            }
            else
            {
                DebugText.Visibility = Visibility.Collapsed;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        private void OnClosed(object? sender, EventArgs e)
        {
            _animTimer?.Stop();
            _staticCapture?.Dispose();
            _liveCapture?.Dispose();
            _renderer?.Dispose();
        }
    }
}
