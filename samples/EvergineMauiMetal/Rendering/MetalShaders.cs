namespace EvergineMauiMetal.Rendering;

internal static class MetalShaders
{
    public const string Vertex = """
        #include <metal_stdlib>
        using namespace metal;

        struct Uniforms
        {
            float4x4 worldViewProjection;
        };

        struct VertexInput
        {
            float4 position [[attribute(0)]];
            float2 textureCoordinate [[attribute(1)]];
        };

        struct VertexOutput
        {
            float4 position [[position]];
            float2 textureCoordinate [[user(locn0)]];
        };

        vertex VertexOutput VS(
            VertexInput input [[stage_in]],
            constant Uniforms& uniforms [[buffer(0)]])
        {
            VertexOutput output;
            output.position = uniforms.worldViewProjection * input.position;
            output.textureCoordinate = input.textureCoordinate;
            return output;
        }
        """;

    public const string Fragment = """
        #include <metal_stdlib>
        using namespace metal;

        struct FragmentInput
        {
            float2 textureCoordinate [[user(locn0)]];
        };

        fragment float4 PS(
            FragmentInput input [[stage_in]],
            texture2d<float> dashboard [[texture(0)]],
            sampler dashboardSampler [[sampler(0)]])
        {
            return dashboard.sample(dashboardSampler, input.textureCoordinate);
        }
        """;
}
