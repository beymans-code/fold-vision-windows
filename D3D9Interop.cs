using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace FoldVision
{
    internal static class D3D9Interop
    {
        private static IDirect3D9Ex s_d3d9;
        private static IDirect3DDevice9Ex s_device9;

        private static void EnsureDevice9()
        {
            if (s_device9 != null) return;

            s_d3d9 = D3D9.Direct3DCreate9Ex();

            var pp = new PresentParameters
            {
                BackBufferWidth = 1,
                BackBufferHeight = 1,
                BackBufferFormat = Format.Unknown,
                BackBufferCount = 1,
                MultiSampleType = MultisampleType.None,
                SwapEffect = SwapEffect.Discard,
                DeviceWindowHandle = NativeMethods.GetDesktopWindow(),
                Windowed = true,
                PresentationInterval = PresentInterval.Default
            };

            s_device9 = s_d3d9.CreateDeviceEx(
                0, // D3DADAPTER_DEFAULT
                DeviceType.Hardware,
                NativeMethods.GetDesktopWindow(),
                CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.FpuPreserve,
                pp);
        }

        public static IntPtr OpenSharedSurface(int width, int height, IntPtr sharedHandle)
        {
            EnsureDevice9();

            // Usamos IntPtr como Handle para Texture
            // Vortice tiene CreateTexture con param ref IntPtr para hSharedHandle
            IntPtr pShared = sharedHandle;
            
            // Creamos la textura usando el handle compartido
            var tex = s_device9.CreateTexture(
                (uint)width, (uint)height, 
                1, 
                Usage.RenderTarget, 
                Format.A8R8G8B8, 
                Pool.Default, 
                ref pShared);

            // Obtenemos la superficie 0
            var surface = tex.GetSurfaceLevel(0);

            // Obtenemos el puntero nativo para WPF
            IntPtr ptr = surface.NativePointer;

            // Retenemos el puntero porque WPF va a tomar posesión/AddRef.
            // (La envoltura de Vortice liberará un ref al ser recolectada, así que
            // AddRef manual o usar Marshal.GetIUnknownForObject no es necesario si NativePointer hace AddRef,
            // pero NativePointer no hace AddRef en Vortice. Así que hacemos Marshal.AddRef)
            Marshal.AddRef(ptr);
            
            // Podemos hacer dispose de la textura managed y surface managed
            // porque WPF mantendrá el objeto vivo internamente con su propio AddRef.
            surface.Dispose();
            tex.Dispose();

            return ptr;
        }
    }
}
