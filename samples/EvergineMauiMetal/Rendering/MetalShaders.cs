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

        struct FragmentOutput
        {
            float4 color [[color(0)]];
        };

        fragment FragmentOutput PS(
            FragmentInput input [[stage_in]],
            // Evergine Metal maps TextureView slot 0 to native texture binding 20.
            texture2d<float> dashboard [[texture(20)]],
            sampler dashboardSampler [[sampler(0)]])
        {
            FragmentOutput output;
            float4 dashboardColor = dashboard.sample(dashboardSampler, input.textureCoordinate);
            float2 edgeDistance = min(input.textureCoordinate, 1.0 - input.textureCoordinate);
            float edge = 1.0 - smoothstep(0.0, 0.035, min(edgeDistance.x, edgeDistance.y));
            output.color = mix(dashboardColor, float4(0.18, 0.95, 0.80, 1.0), edge);
            return output;
        }
        """;
}
