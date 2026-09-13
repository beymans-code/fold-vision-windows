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
                    System.IO.File.AppendAllText("crash.log", $"EnumOutputs falló: {result.Code}\n");
                    return;
                }
                using var output1 = output.QueryInterface<IDXGIOutput1>();
                
                _deskDupl = output1.DuplicateOutput(_device);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("crash.log", $"Error InitDuplication: {ex.Message}\n");
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
                var result = _deskDupl.AcquireNextFrame(0, out var frameInfo, out var desktopResource);
                
                if (result.Success && desktopResource != null)
                {
                    using var acquiredTexture = desktopResource.QueryInterface<ID3D11Texture2D>();
                    
                    // Si no tenemos textura persistente o cambió el tamaño, la creamos
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

                    // Copiar el frame capturado a nuestra textura persistente
                    _device.ImmediateContext.CopyResource(LatestFrame, acquiredTexture);
                    
                    desktopResource.Dispose();
                    _deskDupl.ReleaseFrame();
                    
                    NewFrameReady = true;
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
                    else if (result.Code == (int)Vortice.DXGI.ResultCode.WaitTimeout && LatestFrame == null)
                    {
                        // Si hace timeout pero nunca tuvimos frame, forzamos que devuelva algo (GDI+ fallback temporal)
                        ForceInitialFrameGdi();
                    }
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("crash.log", $"Error CaptureScreen: {ex.Message}\n");
                try { _deskDupl?.ReleaseFrame(); } catch { }
            }
        }

        private void ForceInitialFrameGdi()
        {
            try
            {
                IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
                int w = NativeMethods.GetDeviceCaps(hdc, NativeMethods.DESKTOPHORZRES);
                int h = NativeMethods.GetDeviceCaps(hdc, NativeMethods.DESKTOPVERTRES);
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);

                using var bmp = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size, System.Drawing.CopyPixelOperation.SourceCopy);
                }

                var data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var desc = new Texture2DDescription
                {
                    Width = (uint)w,
                    Height = (uint)h,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.ShaderResource
                };
                
                var subresource = new Vortice.Direct3D11.SubresourceData(data.Scan0, (uint)data.Stride, 0);
                LatestFrame = _device.CreateTexture2D(desc, new[] { subresource });
                bmp.UnlockBits(data);
                
                NewFrameReady = true;
            }
            catch { }
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
