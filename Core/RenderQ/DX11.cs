using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ExileCore.Shared;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;
using Vector4 = System.Numerics.Vector4;

namespace ExileCore.RenderQ;

/// <summary>
/// Owns the Direct3D11 device, swap chain and render target, and drives the per-frame
/// render loop for the ImGui and sprite renderers. Also acts as the texture cache.
/// </summary>
public class DX11 : IDisposable
{
    private readonly RenderForm _form;

    private readonly Vector4
        _clearColor = Color.Transparent.ToVector4().ToVector4Num(); //new System.Numerics.Vector4(0.45f, 0.55f, 0.60f, 0.1f);

    private readonly SwapChain _swapChain;
    private readonly object _sync = new object();
    private Color4 ClearColor = new Color4(0, 0, 0, 0);
    private Buffer ConstantBuffer;
    private double debugTime;
    private double endFrameTime;
    private readonly Factory factory;
    private readonly DebugInformation ImGuiDebug;
    private Buffer IndexBuffer;
    private readonly DebugInformation SpritesDebug;
    private double startFrameTime;
    private readonly Stopwatch sw;
    private readonly DebugInformation SwapchainDebug;
    private Buffer VertexBuffer;
    private Viewport Viewport;

    /// <summary>
    /// Creates the Direct3D11 device, swap chain, render target and child renderers
    /// bound to the supplied <paramref name="form"/>.
    /// </summary>
    public DX11(RenderForm form, CoreSettings coreSettings)
    {
        _form = form;
        sw = Stopwatch.StartNew();
        LoadedTexturesByName = new Dictionary<string, ShaderResourceView>();
        LoadedTexturesByPtrs = new Dictionary<IntPtr, ShaderResourceView>();

        var swapChainDesc = new SwapChainDescription
        {
            Usage = Usage.RenderTargetOutput,
            OutputHandle = form.Handle,
            BufferCount = 1,
            IsWindowed = true,
            Flags = SwapChainFlags.AllowModeSwitch,
            SwapEffect = SwapEffect.Discard,
            SampleDescription = new SampleDescription(1, 0),
            ModeDescription = new ModeDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                Width = form.Width,
                Height = form.Height,
                Scaling = DisplayModeScaling.Unspecified,
                RefreshRate = new Rational(60, 1),
                ScanlineOrdering = DisplayModeScanlineOrder.Unspecified
            }
        };

        Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None,
            new[] {FeatureLevel.Level_11_0, FeatureLevel.Level_10_0}, swapChainDesc, out var device,
            out var swapChain);

        D11Device = device;
        DeviceContext = device.ImmediateContext;
        _swapChain = swapChain;

        factory = swapChain.GetParent<Factory>();
        factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
        BackBuffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
        RenderTargetView = new RenderTargetView(device, BackBuffer);

        using (new PerformanceTimer("Init ImGuiRender"))
        {
            ImGuiRender = new ImGuiRender(this, form, coreSettings);
        }

        using (new PerformanceTimer("Init SpriteRender"))
        {
            SpritesRender = new SpritesRender(this, form, coreSettings);
        }

        InitStates();

        form.UserResized += (sender, args) =>
        {
            RenderTargetView?.Dispose();
            BackBuffer.Dispose();

            swapChain.ResizeBuffers(1, form.Width, form.Height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);
            BackBuffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
            RenderTargetView = new RenderTargetView(device, BackBuffer);
            ImGuiRender.Resize(form.Bounds);
            ImGuiRender.UpdateConstantBuffer();
            SpritesRender.ResizeConstBuffer(BackBuffer.Description);
            var descp = BackBuffer.Description;
            Viewport.Height = form.Height;
            Viewport.Width = form.Width;
            DeviceContext.Rasterizer.SetViewport(Viewport);
            DeviceContext.OutputMerger.SetRenderTargets(RenderTargetView);
        };

        ImGuiDebug = new DebugInformation("ImGui");
        SpritesDebug = new DebugInformation("Sprites");
        SwapchainDebug = new DebugInformation("Swapchain");
    }

    /// <summary>The immediate device context used to issue draw and resource commands.</summary>
    public DeviceContext DeviceContext { get; }

    /// <summary>The Direct3D11 device.</summary>
    public Device D11Device { get; }

    /// <summary>The active vertex shader.</summary>
    public VertexShader VertexShader { get; set; }

    /// <summary>The active pixel shader.</summary>
    public PixelShader PixelShader { get; set; }

    /// <summary>The render target view bound to the swap chain back buffer.</summary>
    public RenderTargetView RenderTargetView { get; set; }

    /// <summary>The input layout describing the vertex format.</summary>
    public InputLayout Layout { get; set; }

    /// <summary>The swap chain back buffer texture.</summary>
    public Texture2D BackBuffer { get; private set; }

    /// <summary>Whether presentation waits for vertical sync.</summary>
    public bool VSync { get; set; } = false;

    private Dictionary<string, ShaderResourceView> LoadedTexturesByName { get; }
    private Dictionary<IntPtr, ShaderResourceView> LoadedTexturesByPtrs { get; }

    /// <summary>The ImGui renderer.</summary>
    public ImGuiRender ImGuiRender { get; }

    /// <summary>The sprite renderer.</summary>
    public SpritesRender SpritesRender { get; }

    /// <summary>The number of currently loaded textures.</summary>
    public int TextutresCount => LoadedTexturesByName.Count;

    /// <summary>Releases the Direct3D11 device, swap chain and render resources.</summary>
    public void Dispose()
    {
        RenderTargetView.Dispose();
        BackBuffer.Dispose();
        DeviceContext.Dispose();
        D11Device.Dispose();
        _swapChain.Dispose();
        factory.Dispose();
    }

    private void InitStates()
    {
        //Debug if texture broken

        //Blend
        var blendStateDescription = new BlendStateDescription();
        blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
        blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
        blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
        blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
        blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.InverseSourceAlpha;
        blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
        blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
        blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
        var blendState = new BlendState(D11Device, blendStateDescription);
        DeviceContext.OutputMerger.BlendFactor = Color.White;
        DeviceContext.OutputMerger.SetBlendState(blendState);

        //Depth
        var depthStencilStateDescription = new DepthStencilStateDescription
        {
            IsDepthEnabled = false,
            IsStencilEnabled = false,
            DepthWriteMask = DepthWriteMask.All,
            DepthComparison = Comparison.Always,
            FrontFace =
            {
                FailOperation = StencilOperation.Keep,
                DepthFailOperation = StencilOperation.Keep,
                PassOperation = StencilOperation.Keep,
                Comparison = Comparison.Always
            }
        };

        depthStencilStateDescription.BackFace = depthStencilStateDescription.FrontFace;
        var depthStencilState = new DepthStencilState(D11Device, depthStencilStateDescription);
        DeviceContext.OutputMerger.SetDepthStencilState(depthStencilState);

        Viewport = new Viewport
        {
            Height = _form.ClientSize.Height,
            Width = _form.ClientSize.Width,
            X = 0,
            Y = 0,
            MaxDepth = 1,
            MinDepth = 0
        };

        // Setup and create the viewport for rendering.
        DeviceContext.Rasterizer.SetViewport(Viewport);
        DeviceContext.OutputMerger.SetRenderTargets(RenderTargetView);

        DeviceContext.Rasterizer.State =
            new RasterizerState(D11Device, new RasterizerStateDescription {FillMode = FillMode.Solid, CullMode = CullMode.None});
    }

    /// <summary>Sets the clear color and clears the render target.</summary>
    public void Clear(Color4 clear)
    {
        ClearColor = clear;
        Clear();
    }

    /// <summary>Clears the render target using the current clear color.</summary>
    public void Clear()
    {
        DeviceContext.ClearRenderTargetView(RenderTargetView, ClearColor);
    }

    /// <summary>Disposes and removes the cached texture with the given name.</summary>
    public void DisposeTexture(string name)
    {
        lock (_sync)
        {
            if (LoadedTexturesByName.TryGetValue(name, out var texture))
            {
                LoadedTexturesByPtrs.Remove(texture.NativePointer);
                LoadedTexturesByName.Remove(name);
                texture.Dispose();
            }
            else
                DebugWindow.LogError($"({nameof(DisposeTexture)}) Texture {name} not found.", 10);
        }
    }

    /// <summary>Adds or replaces the cached texture stored under the given name.</summary>
    public void AddOrUpdateTexture(string name, ShaderResourceView texture)
    {
        lock (_sync)
        {
            if (LoadedTexturesByName.TryGetValue(name, out var res)) res.Dispose();
            LoadedTexturesByName[name] = texture;
            LoadedTexturesByPtrs[texture.NativePointer] = texture;
        }
    }

    /// <summary>Gets the cached texture by name, throwing if it is not loaded.</summary>
    public ShaderResourceView GetTexture(string name)
    {
        if (LoadedTexturesByName.TryGetValue(name, out var result)) return result;
        throw new FileNotFoundException($"Texture by name: {name} not found");
    }

    /// <summary>Gets the cached texture by its native pointer, throwing if it is not loaded.</summary>
    public ShaderResourceView GetTexture(IntPtr ptr)
    {
        if (LoadedTexturesByPtrs.TryGetValue(ptr, out var result)) return result;
        throw new FileNotFoundException($"Texture by ptr: {ptr} not found");
    }

    /// <summary>Returns whether a texture with the given name is loaded.</summary>
    public bool HasTexture(string name)
    {
        return LoadedTexturesByName.ContainsKey(name);
    }

    /// <summary>Returns whether a texture with the given native pointer is loaded.</summary>
    public bool HasTexture(IntPtr name)
    {
        return LoadedTexturesByPtrs.ContainsKey(name);
    }

    /// <summary>Runs a single render frame: ticks the core, draws sprites and ImGui, then presents.</summary>
    public void Render(double sleepTime, Core core)
    {
        try
        {
            startFrameTime = sw.Elapsed.TotalSeconds;
            Clear(new Color(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W));
            debugTime = sw.Elapsed.TotalMilliseconds;
            ImGui.NewFrame();

            ImGuiRender.BeginBackGroundWindow();
            core.Tick();
            debugTime = sw.Elapsed.TotalMilliseconds;
            SpritesRender.Render();
            SpritesDebug.Tick = sw.Elapsed.TotalMilliseconds - debugTime;
            debugTime = sw.Elapsed.TotalMilliseconds;
            ImGuiRender.Render();
            ImGuiDebug.Tick = sw.Elapsed.TotalMilliseconds - debugTime;
            debugTime = sw.Elapsed.TotalMilliseconds;
            _swapChain.Present(VSync ? 1 : 0, PresentFlags.None);
            SwapchainDebug.Tick = (float) (sw.Elapsed.TotalMilliseconds - debugTime);
            endFrameTime = sw.Elapsed.TotalSeconds;
            ImGui.GetIO().DeltaTime = (float) Time.DeltaTime;
        }
        catch (Exception e)
        {
            Core.Logger.Error($"DX11.Render -> {e}");
        }
    }
}
