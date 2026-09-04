using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Mathematics;
using Evergine.Metal;
using EvergineMauiMetal.Rendering;

namespace EvergineMauiMetal.Game;

internal sealed class DashboardScene : Scene
{
    private const int DashboardSize = 1024;

    private MTLTexture? dashboardTexture;
    private SamplerState? dashboardSampler;
    private Evergine.Framework.Graphics.Effects.Effect? dashboardEffect;
    private Material? dashboardMaterial;

    protected override void CreateScene()
    {
        var graphicsContext =
            (MTLGraphicsContext)global::Evergine.Framework.Application.Current.Container.Resolve<GraphicsContext>();

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

        dashboardTexture = (MTLTexture)graphicsContext.Factory.CreateTexture(ref textureDescription);

        var samplerDescription = SamplerStates.LinearClamp;
        dashboardSampler = graphicsContext.Factory.CreateSamplerState(ref samplerDescription);
        dashboardEffect = new MetalDashboardEffect(graphicsContext);
        dashboardMaterial = new Material(dashboardEffect)
        {
            LayerDescription = new RenderLayerDescription
            {
                RenderState = new RenderStateDescription
                {
                    RasterizerState = RasterizerStates.CullBack,
                    BlendState = BlendStates.Opaque,
                    DepthStencilState = DepthStencilStates.ReadWrite,
                },
            },
        };
        dashboardMaterial.SetTexture(dashboardTexture, 0);
        dashboardMaterial.SetSampler(dashboardSampler, 0);

        var camera = new Entity("Camera")
            .AddComponent(new Transform3D
            {
                LocalPosition = new Vector3(0, 0, 5.5f),
            })
            .AddComponent(new Camera3D
            {
                BackgroundColor = new Evergine.Common.Graphics.Color(7, 13, 28, 255),
            });

        var cube = new Entity("SkiaDashboardCube")
            .AddComponent(new Transform3D())
            .AddComponent(new MaterialComponent
            {
                Material = dashboardMaterial,
            })
            .AddComponent(new CubeMesh
            {
                Size = 1.6f,
            })
            .AddComponent(new MeshRenderer())
            .AddComponent(new Spinner
            {
                AxisIncrease = new Vector3(0.38f, 0.62f, 0.17f),
            })
            .AddComponent(new SkiaTextureUpdater(
                graphicsContext,
                dashboardTexture));

        Managers.EntityManager.Add(camera);
        Managers.EntityManager.Add(cube);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        dashboardMaterial?.Dispose();
        dashboardEffect?.Dispose();
        dashboardSampler?.Dispose();

        dashboardTexture?.Dispose();
    }
}
