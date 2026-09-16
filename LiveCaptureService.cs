using System;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace FoldVision
{
    public sealed class LiveCaptureService : IDisposable
    {
        private readonly ID3D11Device _device;
        private IDXGIOutputDuplication? _deskDupl;
        
        public ID3D11Texture2D? LatestFrame { get; private set; }
        public bool NewFrameReady = false;

        public LiveCaptureService(ID3D11Device device)
        {
            _device = device;
            InitDuplication();
        }

        private void InitDuplication()
        {
            try
            {
                using var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
                using var adapter = dxgiDevice.GetAdapter();
                var result = adapter.EnumOutputs(0, out var output);
                if (result.Failure) 
                {
                try { System.IO.File.AppendAllText(AppSettings.GetCrashLogPath(), $"EnumOutputs falló: {result.Code}\n"); } catch { }
                return;
                }
                using var output1 = output.QueryInterface<IDXGIOutput1>();
                
                _deskDupl = output1.DuplicateOutput(_device);
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText(AppSettings.GetCrashLogPath(), $"Error InitDuplication: {ex.Message}\n"); } catch { }
                _deskDupl = null;
            }
        }

        public void CaptureScreen()
        {
            if (_deskDupl == null)
            {
                InitDuplication();
                if (_deskDupl == null) return;
            }

            try
            {
                bool gotNewFrame = false;

                // Vaciamos la cola de DXGI (hasta 5 frames) para garantizar que si la app estuvo inactiva
                // en segundo plano, no procesemos frames viejos acumulados.
                for (int i = 0; i < 5; i++)
                {
                    var result = _deskDupl.AcquireNextFrame(0, out var frameInfo, out var desktopResource);
                    
                    if (result.Success && desktopResource != null)
                    {
                        using var acquiredTexture = desktopResource.QueryInterface<ID3D11Texture2D>();
                        
                        if (LatestFrame == null || 
                            LatestFrame.Description.Width != acquiredTexture.Description.Width ||
                            LatestFrame.Description.Height != acquiredTexture.Description.Height)
                        {
                            LatestFrame?.Dispose();
                            var desc = new Texture2DDescription
                            {
                                Width = acquiredTexture.Description.Width,
                                Height = acquiredTexture.Description.Height,
                                MipLevels = 1,
                                ArraySize = 1,
                                Format = acquiredTexture.Description.Format,
                                SampleDescription = new SampleDescription(1, 0),
                                Usage = ResourceUsage.Default,
                                BindFlags = BindFlags.ShaderResource,
                                MiscFlags = ResourceOptionFlags.None
                            };
                            LatestFrame = _device.CreateTexture2D(desc);
                        }

                        _device.ImmediateContext.CopyResource(LatestFrame, acquiredTexture);
                        desktopResource.Dispose();
                        _deskDupl.ReleaseFrame();
                        gotNewFrame = true;
                    }
                    else if (result.Failure)
                    {
                        if (result.Code == (int)Vortice.DXGI.ResultCode.AccessLost)
                        {
                            _deskDupl.Dispose();
                            _deskDupl = null;
                        }
                        else if (result.Code != (int)Vortice.DXGI.ResultCode.WaitTimeout)
                        {
                            try { _deskDupl.ReleaseFrame(); } catch { }
                        }
                        break; // Timeout o Error: ya no hay más frames en la cola
                    }
                }

                if (gotNewFrame)
                {
                    NewFrameReady = true;
                }
                // Eliminamos el fallback a GDI+ (ForceInitialFrameGdi) aquí.
                // Si DXGI aún no tiene frame, es mejor que devuelva null por unos milisegundos.
                // OverlayWindow detectará que es null y mantendrá la ventana transparente 
                // para que el usuario siga viendo su escritorio real hasta que DXGI entregue
                // el primer frame 100% exacto, evitando así el parpadeo de "imágenes combinadas".
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText(AppSettings.GetCrashLogPath(), $"Error CaptureScreen: {ex.Message}\n"); } catch { }
                try { _deskDupl?.ReleaseFrame(); } catch { }
            }
        }


        public void ClearMemory()
        {
            LatestFrame?.Dispose();
            LatestFrame = null;
        }

        public void Dispose()
        {
            try { _deskDupl?.ReleaseFrame(); } catch { }
            _deskDupl?.Dispose();
            LatestFrame?.Dispose();
        }
    }
}
