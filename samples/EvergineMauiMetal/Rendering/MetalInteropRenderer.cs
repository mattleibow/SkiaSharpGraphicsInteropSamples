using System.Diagnostics;
using System.Runtime.CompilerServices;
using CoreAnimation;
using Evergine.Common.Graphics;
using Evergine.Common.Graphics.VertexFormats;
using EvergineMauiMetal.Controls;
using EvergineMauiMetal.Interop;
using Evergine.Mathematics;
using Evergine.Metal;
using EwgBuffer = Evergine.Common.Graphics.Buffer;
using EwgTexture = Evergine.Metal.MTLTexture;

namespace EvergineMauiMetal.Rendering;

internal sealed class MetalInteropRenderer : IDisposable
{
    private const int DashboardSize = 1024;

    private static readonly VertexPositionTexture[] QuadVertices =
    [
        new(new Vector3(-1.6f, -1.0f, 0), new Vector2(0, 1)),
        new(new Vector3(-1.6f,  1.0f, 0), new Vector2(0, 0)),
        new(new Vector3( 1.6f,  1.0f, 0), new Vector2(1, 0)),
        new(new Vector3(-1.6f, -1.0f, 0), new Vector2(0, 1)),
        new(new Vector3( 1.6f,  1.0f, 0), new Vector2(1, 0)),
        new(new Vector3( 1.6f, -1.0f, 0), new Vector2(1, 1)),
    ];

    private readonly MTLGraphicsContext graphicsContext;
    private readonly CAMetalLayer presentationLayer;
    private readonly CommandQueue renderQueue;
    private readonly GraphicsPipelineState pipelineState;
    private readonly ResourceLayout resourceLayout;
    private readonly ResourceSet resourceSet;
    private readonly Shader vertexShader;
    private readonly Shader fragmentShader;
    private readonly EwgBuffer vertexBuffer;
    private readonly EwgBuffer constantBuffer;
    private readonly SamplerState sampler;
    private readonly EwgTexture dashboardTexture;
    private readonly SkiaMetalTextureBridge skiaBridge;
    private readonly InteropFrameContract frameContract = new();
    private readonly ZeroCopyDiagnostics diagnostics;
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private Viewport[] viewports = [];
    private Rectangle[] scissors = [];
    private bool disposed;

    public MetalInteropRenderer(MetalHostView hostView)
    {
        graphicsContext = new MTLGraphicsContext();
        graphicsContext.CreateDevice();
        presentationLayer = hostView.MetalLayer;
        presentationLayer.Device = graphicsContext.device;

        var textureDescription = new TextureDescription
        {
            Type = TextureType.Texture2D,
            Width = DashboardSize,
            Height = DashboardSize,
            Depth = 1,
            ArraySize = 1,
            MipLevels = 1,
            Format = PixelFormat.R8G8B8A8_UNorm,
            Flags = TextureFlags.RenderTarget | TextureFlags.ShaderResource,
            CpuAccess = ResourceCpuAccess.None,
            Usage = ResourceUsage.Default,
            SampleCount = TextureSampleCount.None,
        };

        dashboardTexture = (EwgTexture)graphicsContext.Factory.CreateTexture(ref textureDescription);
        diagnostics = new ZeroCopyDiagnostics(dashboardTexture.NativePointer, "Evergine Metal + Skia Ganesh Metal");
        skiaBridge = new SkiaMetalTextureBridge(
            graphicsContext.device,
            graphicsContext.NativeDevicePointer,
            dashboardTexture.NativePointer,
            DashboardSize,
            DashboardSize);

        vertexShader = CreateShader(MetalShaders.Vertex, "VS", ShaderStages.Vertex);
        fragmentShader = CreateShader(MetalShaders.Fragment, "PS", ShaderStages.Pixel);

        var vertexBufferDescription = new BufferDescription(
            (uint)(Unsafe.SizeOf<VertexPositionTexture>() * QuadVertices.Length),
            BufferFlags.VertexBuffer,
            ResourceUsage.Default);
        vertexBuffer = graphicsContext.Factory.CreateBuffer(QuadVertices, ref vertexBufferDescription);

        var constantBufferDescription = new BufferDescription(
            64,
            BufferFlags.ConstantBuffer,
            ResourceUsage.Default);
        constantBuffer = graphicsContext.Factory.CreateBuffer(ref constantBufferDescription);

        var samplerDescription = SamplerStates.LinearClamp;
        sampler = graphicsContext.Factory.CreateSamplerState(ref samplerDescription);

        var resourceLayoutDescription = new ResourceLayoutDescription(
            new LayoutElementDescription(0, ResourceType.ConstantBuffer, ShaderStages.Vertex),
            new LayoutElementDescription(0, ResourceType.TextureView, ShaderStages.Pixel),
            new LayoutElementDescription(0, ResourceType.Sampler, ShaderStages.Pixel));
        resourceLayout = graphicsContext.Factory.CreateResourceLayout(ref resourceLayoutDescription);

        var resourceSetDescription = new ResourceSetDescription(
            resourceLayout,
            constantBuffer,
            dashboardTexture,
            sampler);
        resourceSet = graphicsContext.Factory.CreateResourceSet(ref resourceSetDescription);

        var pipelineDescription = new GraphicsPipelineDescription
        {
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            InputLayouts = new InputLayouts().Add(VertexPositionTexture.VertexFormat),
            ResourceLayouts = [resourceLayout],
            Shaders = new GraphicsShaderStateDescription
            {
                VertexShader = vertexShader,
                PixelShader = fragmentShader,
            },
            RenderStates = new RenderStateDescription
            {
                RasterizerState = RasterizerStates.CullBack,
                BlendState = BlendStates.Opaque,
                DepthStencilState = DepthStencilStates.None,
            },
            Outputs = new OutputDescription(
                null,
                [new OutputAttachmentDescription(PixelFormat.B8G8R8A8_UNorm)],
                TextureSampleCount.None,
                1),
        };
        pipelineState = graphicsContext.Factory.CreateGraphicsPipeline(ref pipelineDescription);
        renderQueue = graphicsContext.Factory.CreateCommandQueue();

        WriteDiagnostic(
            $"Interop initialized: backend={diagnostics.Backend}, " +
            $"nativeTexture=0x{diagnostics.NativeTextureHandle:X}, CPU readbacks=0, CPU uploads after creation=0.");
    }

    public void Resize(double width, double height, double displayScale)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var pixelWidth = Math.Max(1u, (uint)Math.Ceiling(width * displayScale));
        var pixelHeight = Math.Max(1u, (uint)Math.Ceiling(height * displayScale));
        viewports = [new Viewport(0, 0, pixelWidth, pixelHeight)];
        scissors = [new Rectangle(0, 0, (int)pixelWidth, (int)pixelHeight)];
    }

    public void DrawFrame()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (viewports.Length == 0)
        {
            return;
        }

        using var drawable = presentationLayer.NextDrawable();
        if (drawable is null)
        {
            return;
        }

        var targetDescription = new TextureDescription
        {
            Type = TextureType.Texture2D,
            Width = (uint)drawable.Texture.Width,
            Height = (uint)drawable.Texture.Height,
            Depth = 1,
            ArraySize = 1,
            MipLevels = 1,
            Format = PixelFormat.B8G8R8A8_UNorm,
            Flags = TextureFlags.RenderTarget,
            CpuAccess = ResourceCpuAccess.None,
            Usage = ResourceUsage.Default,
            SampleCount = TextureSampleCount.None,
        };
        using var targetTexture = EwgTexture.FromMetalImage(
            graphicsContext,
            ref targetDescription,
            drawable.Texture);
        using var frameBuffer = new MTLFrameBuffer(
            graphicsContext,
            null,
            [new FrameBufferAttachment(targetTexture)],
            disposeAttachments: false);

        frameContract.BeginSkia();
        skiaBridge.RenderDashboard(diagnostics, elapsed.Elapsed);
        frameContract.CompleteSkiaSynchronously();

        frameContract.BeginEvergine();

        var aspect = viewports[0].Width / viewports[0].Height;
        var view = Matrix4x4.CreateLookAt(new Vector3(0, 0, 4.8f), Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            aspect,
            0.1f,
            100f,
            reverseDepthBuffer: true);
        var rotation = Matrix4x4.CreateRotationY((float)elapsed.Elapsed.TotalSeconds * 0.55f);
        var worldViewProjection = rotation * view * projection;

        var commandBuffer = renderQueue.CommandBuffer();
        commandBuffer.Begin();
        commandBuffer.UpdateBufferData(constantBuffer, ref worldViewProjection);

        var renderPass = new RenderPassDescription(
            frameBuffer,
            new ClearValue(
                ClearFlags.All,
                1,
                0,
                new Evergine.Common.Graphics.Color(0.027f, 0.043f, 0.086f, 1)));
        commandBuffer.BeginRenderPass(ref renderPass);
        commandBuffer.SetViewports(viewports);
        commandBuffer.SetScissorRectangles(scissors);
        commandBuffer.SetGraphicsPipelineState(pipelineState);
        commandBuffer.SetResourceSet(resourceSet);
        commandBuffer.SetVertexBuffers([vertexBuffer]);
        commandBuffer.Draw((uint)QuadVertices.Length);
        commandBuffer.EndRenderPass();
        commandBuffer.End();
        commandBuffer.Commit();

        renderQueue.Submit();
        renderQueue.WaitIdle();
        drawable.Present();
        frameContract.CompleteEvergineSynchronously();

        diagnostics.CompleteFrame(dashboardTexture.NativePointer);
        if (!diagnostics.IsNativeHandleStable)
        {
            throw new InvalidOperationException("The Evergine-owned native texture handle changed.");
        }

        if (diagnostics.FrameCount % 300 == 0)
        {
            WriteDiagnostic(
                $"Interop proof: frame={diagnostics.FrameCount}, CPU readbacks={diagnostics.CpuReadbacks}, " +
                $"CPU uploads after creation={diagnostics.CpuUploadsAfterCreation}, " +
                $"native handle stable={diagnostics.IsNativeHandleStable}, backend={diagnostics.Backend}.");
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        renderQueue.WaitIdle();
        skiaBridge.Dispose();
        resourceSet.Dispose();
        resourceLayout.Dispose();
        sampler.Dispose();
        constantBuffer.Dispose();
        vertexBuffer.Dispose();
        fragmentShader.Dispose();
        vertexShader.Dispose();
        pipelineState.Dispose();
        dashboardTexture.Dispose();
        renderQueue.Dispose();
        graphicsContext.Dispose();
        disposed = true;
    }

    private Shader CreateShader(string source, string entryPoint, ShaderStages stage)
    {
        var compilation = graphicsContext.ShaderCompile(source, entryPoint, stage, CompilerParameters.Default);
        var description = new ShaderDescription(stage, entryPoint, compilation.ByteCode);
        return graphicsContext.Factory.CreateShader(ref description);
    }

    private static void WriteDiagnostic(string message)
    {
        Trace.WriteLine(message);
        Console.WriteLine(message);
    }
}
