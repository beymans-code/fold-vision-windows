using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace FoldVision
{
    public sealed class StaticCaptureService : IDisposable
    {
        private readonly ID3D11Device _device;
        public ID3D11Texture2D? LatestFrame { get; private set; }
        public bool NewFrameReady = false;

        public StaticCaptureService(ID3D11Device device)
        {
            _device = device;
        }

        public void CaptureScreen()
        {
            try
            {
                // Limpiar textura anterior si existe
                LatestFrame?.Dispose();
                LatestFrame = null;

                IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
                int w = NativeMethods.GetDeviceCaps(hdc, NativeMethods.DESKTOPHORZRES);
                int h = NativeMethods.GetDeviceCaps(hdc, NativeMethods.DESKTOPVERTRES);
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);

                // Capturar con GDI+
                using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);
                }

                // Bloquear bits para leer
                var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                // Formato de GDI+ Format32bppArgb -> BGRA en memoria
                var desc = new Texture2DDescription
                {
                    Width = (uint)w,
                    Height = (uint)h,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.ShaderResource,
                    MiscFlags = ResourceOptionFlags.None
                };

                // Crear textura D3D11 con los datos iniciales
                var subresource = new Vortice.Direct3D11.SubresourceData(data.Scan0, (uint)data.Stride, 0);
                LatestFrame = _device.CreateTexture2D(desc, new[] { subresource });

                bmp.UnlockBits(data);
                
                NewFrameReady = true;
            }
            catch
            {
                // Si falla la captura, no actualizamos LatestFrame
            }
        }

        public void Dispose()
        {
            LatestFrame?.Dispose();
            LatestFrame = null;
        }
    }
}
