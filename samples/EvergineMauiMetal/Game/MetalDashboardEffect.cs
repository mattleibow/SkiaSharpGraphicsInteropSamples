using System.Text;
using Evergine.Common.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Effects.Analyzer;

namespace EvergineMauiMetal.Game;

internal sealed class MetalDashboardEffect : EffectFromCode
{
    private const string PassName = "Default";
    private const string Metadata = """
        [Begin_ResourceLayout]

            cbuffer PerDrawCall : register(b0)
            {
                float4x4 WorldViewProj : packoffset(c0); [WorldViewProjection]
            };

            Texture2D DashboardTexture : register(t0);
            SamplerState DashboardSampler : register(s0);

        [End_ResourceLayout]

        [Begin_Pass:Default]
            [Profile 10_0]
            [Entrypoints VS=VS PS=PS]

            struct VS_IN
            {
                float4 Position : POSITION;
                float3 Normal : NORMAL;
                float2 TexCoord : TEXCOORD;
            };

            struct PS_IN
            {
                float4 Position : SV_POSITION;
                float3 Normal : NORMAL;
                float2 TexCoord : TEXCOORD;
            };

            PS_IN VS(VS_IN input)
            {
                PS_IN output = (PS_IN)0;
                output.Position = mul(input.Position, WorldViewProj);
                output.Normal = input.Normal;
                output.TexCoord = input.TexCoord;
                return output;
            }

            float4 PS(PS_IN input) : SV_Target
            {
                float3 lightDirection = normalize(float3(0.35, 0.7, 0.6));
                float lighting = 0.38 + (0.62 * saturate(dot(normalize(input.Normal), lightDirection)));
                float4 dashboard = DashboardTexture.Sample(DashboardSampler, input.TexCoord);
                return float4(dashboard.rgb * lighting, dashboard.a);
            }

        [End_Pass]
        """;
    private const string MetalVertex = """
        #include <metal_stdlib>
        using namespace metal;

        struct type_PerDrawCall
        {
            float4x4 WorldViewProj;
        };

        struct VertexInput
        {
            float4 position [[attribute(0)]];
            float3 normal [[attribute(1)]];
            float2 textureCoordinate [[attribute(2)]];
        };

        struct VertexOutput
        {
            float4 position [[position]];
            float3 normal [[user(locn0)]];
            float2 textureCoordinate [[user(locn1)]];
        };

        vertex VertexOutput VS(
            VertexInput input [[stage_in]],
            constant type_PerDrawCall& perDrawCall [[buffer(0)]])
        {
            VertexOutput output;
            output.position = perDrawCall.WorldViewProj * input.position;
            output.normal = input.normal;
            output.textureCoordinate = input.textureCoordinate;
            return output;
        }
        """;
    private const string MetalFragment = """
        #include <metal_stdlib>
        using namespace metal;

        struct FragmentInput
        {
            float3 normal [[user(locn0)]];
            float2 textureCoordinate [[user(locn1)]];
        };

        fragment float4 PS(
            FragmentInput input [[stage_in]],
            texture2d<float> SPIRV_Cross_CombinedDashboardTextureDashboardSampler [[texture(20)]],
            sampler DashboardSampler [[sampler(0)]])
        {
            const float3 lightDirection = normalize(float3(0.35, 0.7, 0.6));
            const float lighting = 0.38 + (0.62 * saturate(dot(normalize(input.normal), lightDirection)));
            const float4 dashboard =
                SPIRV_Cross_CombinedDashboardTextureDashboardSampler.sample(
                    DashboardSampler,
                    input.textureCoordinate);
            return float4(dashboard.rgb * lighting, dashboard.a);
        }
        """;

    public MetalDashboardEffect(GraphicsContext graphicsContext)
        : base(graphicsContext, Metadata)
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
            Encoding.UTF8.GetBytes(MetalVertex));
        var vertexShader = graphicsContext.Factory.CreateShader(ref vertexDescription);

        var fragmentDescription = new ShaderDescription(
            ShaderStages.Pixel,
            "PS",
            Encoding.UTF8.GetBytes(MetalFragment));
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
