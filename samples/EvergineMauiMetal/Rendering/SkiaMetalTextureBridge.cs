using Evergine.Metal;
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
        MTLGraphicsContext evergineGraphicsContext,
        MTLTexture engineTexture)
    {
        commandQueue = evergineGraphicsContext.device.CreateCommandQueue()
            ?? throw new InvalidOperationException("Metal did not create a command queue for Skia.");

        backendContext = new GRMtlBackendContext
        {
            DeviceHandle = evergineGraphicsContext.NativeDevicePointer,
            QueueHandle = (nint)commandQueue.Handle,
        };

        graphicsContext = GRContext.CreateMetal(backendContext)
            ?? throw new InvalidOperationException("Skia could not create a Ganesh Metal context.");

        backendTexture = new GRBackendTexture(
            (int)engineTexture.Description.Width,
            (int)engineTexture.Description.Height,
            mipmapped: false,
            new GRMtlTextureInfo(engineTexture.NativePointer));

        surface = SKSurface.Create(
            graphicsContext,
            backendTexture,
            GRSurfaceOrigin.TopLeft,
            sampleCount: 0,
            SKColorType.Rgba8888)
            ?? throw new InvalidOperationException("Skia could not wrap the Evergine-owned Metal texture.");
    }

    public void RenderDashboard(long frameCount, string backend, TimeSpan elapsed)
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

        canvas.DrawText("SHARED METAL TEXTURE", 64, 104, SKTextAlign.Left, titleFont, title);
        canvas.DrawText(DateTimeOffset.Now.ToString("HH:mm:ss.fff"), 64, 166, SKTextAlign.Left, bodyFont, body);
        canvas.DrawText($"Frame {frameCount + 1:N0}", 720, 166, SKTextAlign.Left, bodyFont, body);

        DrawGauge(canvas, "SkiaSharp live frame", 64, 244, 896, 58, 0.35f + (pulse * 0.55f), track, accent, bodyFont, body);

        canvas.DrawRoundRect(new SKRect(64, 382, 960, 690), 24, 24, track);
        canvas.DrawText("Evergine owns this MTLTexture.", 96, 454, SKTextAlign.Left, successFont, success);
        canvas.DrawText("SkiaSharp draws this live UI into it.", 96, 518, SKTextAlign.Left, bodyFont, body);
        canvas.DrawText("Evergine samples the same texture on the cube.", 96, 574, SKTextAlign.Left, bodyFont, body);
        canvas.DrawText(
            "Native handle stable across frames: YES",
            96,
            638,
            SKTextAlign.Left,
            successFont,
            success);

        canvas.DrawText(
            $"Backend: {backend}",
            64,
            786,
            SKTextAlign.Left,
            bodyFont,
            body);
        canvas.DrawText(
            "Ordered Metal queues: Skia writes, then Evergine samples.",
            64,
            842,
            SKTextAlign.Left,
            bodyFont,
            body);

        // Ganesh blocks until its dedicated queue finishes before Evergine samples.
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
