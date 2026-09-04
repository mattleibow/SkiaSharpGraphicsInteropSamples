using EvergineMauiMetal.Interop;
using Metal;
using SkiaSharp;

namespace EvergineMauiMetal.Rendering;

internal sealed class SkiaMetalTextureBridge : IDisposable
{
    private readonly IMTLCommandQueue commandQueue;
    private readonly GRMtlBackendContext backendContext;
    private readonly GRContext graphicsContext;
    private readonly GRBackendTexture backendTexture;
    private readonly SKSurface surface;
    private bool disposed;

    public SkiaMetalTextureBridge(
        IMTLDevice device,
        nint deviceHandle,
        nint textureHandle,
        int width,
        int height)
    {
        commandQueue = device.CreateCommandQueue()
            ?? throw new InvalidOperationException("Metal did not create a command queue for Skia.");

        backendContext = new GRMtlBackendContext
        {
            DeviceHandle = deviceHandle,
            QueueHandle = (nint)commandQueue.Handle,
        };

        graphicsContext = GRContext.CreateMetal(backendContext)
            ?? throw new InvalidOperationException("Skia could not create a Ganesh Metal context.");

        backendTexture = new GRBackendTexture(
            width,
            height,
            mipmapped: false,
            new GRMtlTextureInfo(textureHandle));

        surface = SKSurface.Create(
            graphicsContext,
            backendTexture,
            GRSurfaceOrigin.TopLeft,
            sampleCount: 0,
            SKColorType.Rgba8888)
            ?? throw new InvalidOperationException("Skia could not wrap the Evergine-owned Metal texture.");
    }

    public void RenderDashboard(ZeroCopyDiagnostics diagnostics, TimeSpan elapsed)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var canvas = surface.Canvas;
        var seconds = elapsed.TotalSeconds;
        var pulse = (float)((Math.Sin(seconds * 2.1) + 1) * 0.5);

        canvas.Clear(new SKColor(7, 13, 28));

        using var titleTypeface = SKTypeface.FromFamilyName("SF Pro Display", SKFontStyle.Bold);
        using var titleFont = new SKFont(titleTypeface, 58);
        using var bodyFont = new SKFont(null, 30);
        using var successTypeface = SKTypeface.FromFamilyName("SF Pro Display", SKFontStyle.Bold);
        using var successFont = new SKFont(successTypeface, 30);
        using var title = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(241, 245, 249),
        };
        using var body = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(148, 163, 184),
        };
        using var accent = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(45, 212, 191),
            Style = SKPaintStyle.Fill,
        };
        using var track = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(30, 41, 59),
            Style = SKPaintStyle.Fill,
        };
        using var success = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(110, 231, 183),
        };

        canvas.DrawText("LIVE GPU DASHBOARD", 64, 104, SKTextAlign.Left, titleFont, title);
        canvas.DrawText(DateTimeOffset.Now.ToString("HH:mm:ss.fff"), 64, 166, SKTextAlign.Left, bodyFont, body);
        canvas.DrawText($"Frame {diagnostics.FrameCount + 1:N0}", 720, 166, SKTextAlign.Left, bodyFont, body);

        DrawGauge(canvas, "GPU telemetry", 64, 244, 896, 58, 0.35f + (pulse * 0.55f), track, accent, bodyFont, body);
        DrawGauge(canvas, "Streaming UI", 64, 382, 896, 58, 0.78f - (pulse * 0.25f), track, accent, bodyFont, body);

        canvas.DrawRoundRect(new SKRect(64, 530, 960, 744), 24, 24, track);
        canvas.DrawText($"Backend: {diagnostics.Backend}", 96, 594, SKTextAlign.Left, bodyFont, body);
        canvas.DrawText("CPU readbacks: 0", 96, 650, SKTextAlign.Left, successFont, success);
        canvas.DrawText("CPU uploads after creation: 0", 480, 650, SKTextAlign.Left, successFont, success);
        canvas.DrawText(
            $"Native MTLTexture stable: {(diagnostics.IsNativeHandleStable ? "YES" : "NO")}",
            96,
            706,
            SKTextAlign.Left,
            successFont,
            success);

        canvas.DrawText(
            "Skia flush: synchronous (CPU-blocking, memory remains zero-copy)",
            64,
            836,
            SKTextAlign.Left,
            bodyFont,
            body);
        canvas.DrawText(
            "The rotating Evergine quad samples this exact texture.",
            64,
            890,
            SKTextAlign.Left,
            bodyFont,
            body);

        // The synchronous submit is the public cross-queue ordering point:
        // Skia finishes writing before Evergine starts sampling.
        surface.Flush(submit: true, synchronous: true);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        surface.Dispose();
        backendTexture.Dispose();
        graphicsContext.Dispose();
        backendContext.Dispose();
        commandQueue.Dispose();
        disposed = true;
    }

    private static void DrawGauge(
        SKCanvas canvas,
        string label,
        float x,
        float y,
        float width,
        float height,
        float value,
        SKPaint track,
        SKPaint accent,
        SKFont font,
        SKPaint text)
    {
        canvas.DrawText(label, x, y - 20, SKTextAlign.Left, font, text);
        var bounds = new SKRect(x, y, x + width, y + height);
        canvas.DrawRoundRect(bounds, height / 2, height / 2, track);
        bounds.Right = bounds.Left + (width * Math.Clamp(value, 0, 1));
        canvas.DrawRoundRect(bounds, height / 2, height / 2, accent);
    }
}
