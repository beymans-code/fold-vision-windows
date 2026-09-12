using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace FoldVision
{
    /// <summary>
    /// Renderer D3D11 que:
    ///  1. Compila FoldShader.hlsl en runtime con D3DCompiler
    ///  2. Recibe la textura de GpuCaptureService
    ///  3. Pasa los Uniforms al cbuffer (turn, blur, motionBoost…)
    ///  4. Renderiza en un D3D11 render-target compatible con D3DImage (WPF)
    /// </summary>
    public sealed class GpuRenderer : IDisposable
    {
        // ── D3D11 core ───────────────────────────────────────────────────
        public  readonly ID3D11Device         Device;
        private readonly ID3D11DeviceContext  _ctx;

        // ── Pipeline ─────────────────────────────────────────────────────
        private ID3D11VertexShader?   _vs;
        private ID3D11PixelShader?    _ps;
        private ID3D11Buffer?         _cbuffer;
        private ID3D11SamplerState?   _sampler;
        private ID3D11RenderTargetView? _rtv;

        // ── WPF interop ───────────────────────────────────────────────────
        public  ID3D11Texture2D?      RenderTarget { get; private set; }

        // ── Uniforms (debe coincidir exactamente con cbuffer en HLSL) ────
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = 64)]
        private struct ShaderUniforms
        {
            [System.Runtime.InteropServices.FieldOffset(0)]  public float ImageW;
            [System.Runtime.InteropServices.FieldOffset(4)]  public float ImageH;
            [System.Runtime.InteropServices.FieldOffset(8)]  public float CoverX;
            [System.Runtime.InteropServices.FieldOffset(12)] public float CoverY;
            [System.Runtime.InteropServices.FieldOffset(16)] public float Aspect;
            [System.Runtime.InteropServices.FieldOffset(20)] public float Turn;
            [System.Runtime.InteropServices.FieldOffset(24)] public float BlurStrength;
            [System.Runtime.InteropServices.FieldOffset(28)] public float ReflectionIntensity;
            [System.Runtime.InteropServices.FieldOffset(32)] public float SampleCount;
            [System.Runtime.InteropServices.FieldOffset(36)] public float MotionBoost;
            [System.Runtime.InteropServices.FieldOffset(40)] public float CameraDepth;
            [System.Runtime.InteropServices.FieldOffset(44)] public float StretchMult;
            [System.Runtime.InteropServices.FieldOffset(48)] public float _pad0;
            [System.Runtime.InteropServices.FieldOffset(52)] public float _pad1;
            [System.Runtime.InteropServices.FieldOffset(56)] public float _pad2;
            [System.Runtime.InteropServices.FieldOffset(60)] public float _pad3;
        }

        // Uniforms dinamicos (actualizar via AppSettings)
        public float Turn        = 0f;
        public float MotionBoost = 0f;

        private int _width, _height;

        public GpuRenderer(int width, int height)
        {
            _width  = width;
            _height = height;

            // Crear D3D11 device
            D3D11.D3D11CreateDevice(
                null,
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport, // necesario para DXGI interop con WPF
                new[] { FeatureLevel.Level_11_0, FeatureLevel.Level_10_0 },
                out var dev, out _, out var ctx).CheckError();

            Device = dev!;
            _ctx   = ctx!;

            // Swap chain (para D3DImage necesitamos un surface compartido)
            CreateRenderTarget(width, height);
            CompileShaders();
            CreateConstantBuffer();
            CreateSampler();
        }

        // ── Creación de recursos ──────────────────────────────────────────

        private void CreateRenderTarget(int w, int h)
        {
            _rtv?.Dispose();
            RenderTarget?.Dispose();

            // Textura render-target compartida con WPF (D3DImage)
            RenderTarget = Device.CreateTexture2D(new Texture2DDescription
            {
                Width             = (uint)w,
                Height            = (uint)h,
                MipLevels         = 1,
                ArraySize         = 1,
                Format            = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage             = ResourceUsage.Default,
                BindFlags         = BindFlags.RenderTarget | BindFlags.ShaderResource,
                MiscFlags         = ResourceOptionFlags.Shared // ← compartida con WPF
            });

            _rtv = Device.CreateRenderTargetView(RenderTarget);
        }

        private void CompileShaders()
        {
            string src;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FoldVision.FoldShader.hlsl"))
            {
                if (stream == null) throw new Exception("No se pudo encontrar el recurso FoldShader.hlsl incrustado.");
                using (var reader = new StreamReader(stream))
                {
                    src = reader.ReadToEnd();
                }
            }

            Compiler.Compile(src, null, null, "VS", "FoldShader.hlsl", "vs_5_0", ShaderFlags.OptimizationLevel3,
                             out var vsBlob, out var vsErr);
            if (vsBlob == null)
                throw new Exception($"Error compilando VS: {vsErr?.AsString()}");

            Compiler.Compile(src, null, null, "PS", "FoldShader.hlsl", "ps_5_0", ShaderFlags.OptimizationLevel3,
                             out var psBlob, out var psErr);
            if (psBlob == null)
                throw new Exception($"Error compilando PS: {psErr?.AsString()}");

            _vs = Device.CreateVertexShader(vsBlob.AsBytes());
            _ps = Device.CreatePixelShader(psBlob.AsBytes());

            // Desactivar culling (CullMode.None) para evitar descartar los triángulos
            var rsDesc = new RasterizerDescription
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                DepthClipEnable = false
            };
            var rasterizerState = Device.CreateRasterizerState(rsDesc);
            _ctx.RSSetState(rasterizerState);

            // No usamos InputLayout porque el quad se genera en el Vertex Shader
            _ctx.IASetInputLayout(null);
        }

        private void CreateConstantBuffer()
        {
            _cbuffer = Device.CreateBuffer(new BufferDescription
            {
                ByteWidth      = (uint)((((System.Runtime.InteropServices.Marshal.SizeOf<ShaderUniforms>()) + 15) / 16) * 16),
                Usage          = ResourceUsage.Dynamic,
                BindFlags      = BindFlags.ConstantBuffer,
                CPUAccessFlags = CpuAccessFlags.Write
            });
        }

        private void CreateSampler()
        {
            _sampler = Device.CreateSamplerState(new SamplerDescription
            {
                Filter        = Filter.MinMagMipLinear,
                AddressU      = TextureAddressMode.Border,
                AddressV      = TextureAddressMode.Border,
                AddressW      = TextureAddressMode.Border,
                BorderColor   = new Vortice.Mathematics.Color4(0f, 0f, 0f, 0f),
                MaxAnisotropy = 1,
                MinLOD        = 0,
                MaxLOD        = float.MaxValue
            });
        }

        // ── Render frame ─────────────────────────────────────────────────

        /// <summary>
        /// Renderiza un frame con los efectos de fold.
        /// sourceTex: la textura D3D11 con la captura de pantalla (de GpuCaptureService).
        /// </summary>
        public void Render(ID3D11Texture2D sourceTex)
        {
            // 1. Actualizar cbuffer
            UpdateUniforms((int)sourceTex.Description.Width, (int)sourceTex.Description.Height);

            // 2. Crear SRV de la textura fuente
            var srvDesc = new ShaderResourceViewDescription
            {
                Format        = Format.B8G8R8A8_UNorm,
                ViewDimension = ShaderResourceViewDimension.Texture2D,
                Texture2D     = new Texture2DShaderResourceView { MipLevels = 1, MostDetailedMip = 0 }
            };
            using var srv = Device.CreateShaderResourceView(sourceTex, srvDesc);

            // 3. Setup pipeline
            _ctx.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _ctx.VSSetShader(_vs);
            _ctx.PSSetShader(_ps);
            _ctx.PSSetConstantBuffer(0, _cbuffer);
            _ctx.PSSetShaderResource(0, srv);
            _ctx.PSSetSampler(0, _sampler);

            // 4. Setup render target + viewport
            _ctx.OMSetRenderTargets(_rtv);
            _ctx.RSSetViewport(new Viewport(0, 0, _width, _height));

            // 5. Clear + draw (6 vértices = 2 triángulos fullscreen)
            _ctx.ClearRenderTargetView(_rtv, new Vortice.Mathematics.Color4(0.0f, 0.0f, 0.0f, 1.0f));
            _ctx.Draw(6, 0);
            _ctx.Flush();
        }

        private void UpdateUniforms(int srcW, int srcH)
        {
            float aspect = (float)_width / _height;
            float coverX = srcW > srcH * aspect ? srcH * aspect / srcW : 1f;
            float coverY = srcH > srcW / aspect ? srcW / aspect / srcH : 1f;

            // Adaptive sample count (igual que MetalFoldView.adaptiveSampleCount)
            float sampleCount = Turn < 0.20f ? 12f : Turn < 0.60f ? 20f : 32f;

            var u = new ShaderUniforms
            {
                ImageW              = srcW,
                ImageH              = srcH,
                CoverX              = coverX,
                CoverY              = coverY,
                Aspect              = aspect,
                Turn                = Turn,
                BlurStrength        = AppSettings.BlurStrength,
                ReflectionIntensity = 0.6f,
                SampleCount         = sampleCount,
                MotionBoost         = MotionBoost,
                CameraDepth         = AppSettings.CameraDepth,
                StretchMult         = AppSettings.StretchMultiplier
            };

            var mapped = _ctx.Map(_cbuffer!, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
            unsafe { System.Runtime.InteropServices.Marshal.StructureToPtr(u, mapped.DataPointer, false); }
            _ctx.Unmap(_cbuffer!, 0);
        }

        // ── Cleanup ──────────────────────────────────────────────────────
        public void Dispose()
        {
            _rtv?.Dispose();
            RenderTarget?.Dispose();
            _cbuffer?.Dispose();
            _sampler?.Dispose();
            _vs?.Dispose();
            _ps?.Dispose();
            _ctx.Dispose();
            Device.Dispose();
        }
    }
}
