using System;
using Vortice.Direct3D11;

namespace FoldVision
{
    public sealed class StaticCaptureService : IDisposable
    {
        private readonly ID3D11Device _device;
        private LiveCaptureService? _liveBackend;

        public ID3D11Texture2D? LatestFrame { get; private set; }
        public bool NewFrameReady = false;

        public StaticCaptureService(ID3D11Device device)
        {
            _device = device;
            // Inicializar el backend DXGI en segundo plano para que esté listo al instante
            _liveBackend = new LiveCaptureService(device);
        }

        public void CaptureScreen()
        {
            try
            {
                if (_liveBackend == null) return;

                LatestFrame?.Dispose();
                LatestFrame = null;

                // DXGI a veces requiere unos milisegundos para capturar el primer frame
                for (int i = 0; i < 15; i++)
                {
                    _liveBackend.CaptureScreen();
                    if (_liveBackend.NewFrameReady && _liveBackend.LatestFrame != null)
                    {
                        var desc = _liveBackend.LatestFrame.Description;
                        LatestFrame = _device.CreateTexture2D(desc);
                        _device.ImmediateContext.CopyResource(LatestFrame, _liveBackend.LatestFrame);
                        
                        NewFrameReady = true;

                        // Liberar el backend DXGI activo, ya tenemos nuestra captura estática
                        _liveBackend.Dispose();
                        _liveBackend = null;
                        break;
                    }
                    System.Threading.Thread.Sleep(2);
                }
            }
            catch
            {
                // Si falla la captura, no actualizamos LatestFrame
            }
        }

        public void Dispose()
        {
            _liveBackend?.Dispose();
            LatestFrame?.Dispose();
            LatestFrame = null;
        }
    }
}
