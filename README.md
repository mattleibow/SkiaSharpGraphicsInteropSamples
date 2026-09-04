# SkiaSharp graphics interop samples

GPU-interoperability samples for sharing native graphics resources between
SkiaSharp and rendering engines without application-level pixel transfers.

## Samples

| Sample | Platform | Status |
|---|---|---|
| [SkiaSharp + Evergine Metal](samples/EvergineMauiMetal/README.md) | .NET MAUI iOS Simulator, Metal | Genuine Evergine application; locally runtime-verified |

The Metal sample is the reference implementation. MAUI hosts an Evergine view;
an Evergine Framework application owns the scene, frame loop, rotating cube,
material, and `MTLTexture`; and a scene behavior renders a live SkiaSharp
dashboard directly into that same texture before Evergine samples it.
