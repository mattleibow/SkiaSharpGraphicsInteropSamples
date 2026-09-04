# SkiaSharp graphics interop samples

Truthful GPU-interoperability samples for using SkiaSharp with native graphics
engines without CPU readback or texture re-upload.

## Samples

| Sample | Platform | Status |
|---|---|---|
| [SkiaSharp + Evergine Metal](samples/EvergineMauiMetal/README.md) | .NET MAUI Mac Catalyst, Metal | Working and locally runtime-verified |
| SkiaSharp + Evergine Direct3D 12 | Windows | Blocked by the current public Evergine queue API; details are in the Metal sample README |

The Metal sample is the reference implementation. It renders a live SkiaSharp
dashboard directly into an Evergine-owned `MTLTexture`, then samples that same
texture on a rotating Evergine cube.