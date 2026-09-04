using System.Text;
using Evergine.Common.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Effects.Analyzer;

namespace EvergineMauiMetal.Game;

internal sealed class MetalDashboardEffect : EffectFromCode
{
    private const string PassName = "Default";

    public MetalDashboardEffect(GraphicsContext graphicsContext)
        : base(graphicsContext, DashboardEffectSource.Metadata)
    {
    }

    public override EffectTechnique GetEffectTechnique(
        string? passName,
        string[]? activeDirectives)
    {
        var directives = activeDirectives ?? [];
        var hashCode = EffectHelper.GetPassAndDirectivesHashCode(PassName, directives);
        if (techniquesCached.TryGetValue(hashCode, out var cachedTechnique))
        {
            return cachedTechnique;
        }

        var preprocessedShader = analyzer.Preprocess(PassName, directives);

        var vertexDescription = new ShaderDescription(
            ShaderStages.Vertex,
            "VS",
            Encoding.UTF8.GetBytes(DashboardEffectSource.MetalVertex));
        var vertexShader = graphicsContext.Factory.CreateShader(ref vertexDescription);

        var fragmentDescription = new ShaderDescription(
            ShaderStages.Pixel,
            "PS",
            Encoding.UTF8.GetBytes(DashboardEffectSource.MetalFragment));
        var fragmentShader = graphicsContext.Factory.CreateShader(ref fragmentDescription);

        var shaderState = new GraphicsShaderStateDescription
        {
            VertexShader = vertexShader,
            PixelShader = fragmentShader,
            ShaderInputLayout = analyzer.GetInputLayout(PassName, preprocessedShader),
        };
        EffectHelper.AddedResourceBindingData(ref shaderState, ref graphicsResourcesInfo);

        var resourceLayout = GetResourceLayout(preprocessedShader.ResourceLayoutUsage);
        var technique = new EffectTechnique(
            PassName,
            directives,
            hashCode,
            preprocessedShader.ResourceLayoutUsage,
            shaderState,
            resourceLayout,
            [],
            []);
        techniquesCached.Add(hashCode, technique);
        return technique;
    }

    public override bool IsPassRequiredWithDirectives(
        string passName,
        string[] activeDirectives) => true;
}
