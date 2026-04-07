void ToonShading_float(
    float3 normalInput,
    float celRampSmoothness,
    float3 clipSpacePosition,
    float3 worldPosition,
    float4 celRampTint,
    float celRampOffset,
    out float3 celRampOutput,
    out float3 lightDirection
)
{

#ifdef SHADERGRAPH_PREVIEW

    // just something so shader graph doesn't freak out in preview
    celRampOutput = float3(0.5, 0.5, 0.0);
    lightDirection = float3(1.0, 0.0, 0.0);

#else

    float3 normal = normalize(normalInput);

    float3 result = 0.0;

    // main light setup
    #if SHADOWS_SCREEN
        float4 shadowCoord = ComputeScreenPos(clipSpacePosition);
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(worldPosition);
    #endif

    Light mainLight;

    // grabbing main light, sometimes with shadows sometimes not
    #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
        mainLight = GetMainLight(shadowCoord);
    #else
        mainLight = GetMainLight();
    #endif

    float ndotlMain = saturate(dot(normal, mainLight.direction));

    // basic toon ramp for main light
    float mainCel = smoothstep(
        celRampOffset,
        celRampOffset + celRampSmoothness,
        ndotlMain
    );

    // shadows from main light
    mainCel *= mainLight.shadowAttenuation;

    result += mainLight.color * mainCel;

    // extra lights (point + spot stuff)
    int lightCount = GetAdditionalLightsCount();

    for (int i = 0; i < lightCount; i++)
    {
        Light light = GetAdditionalLight(i, worldPosition);

        float3 lightDir = normalize(light.direction);

        float ndotl = saturate(dot(normal, lightDir));

        // same toon ramp but for extra lights
        float cel = smoothstep(
            celRampOffset,
            celRampOffset + celRampSmoothness,
            ndotl
        );

        // distance fade + shadows
        cel *= light.distanceAttenuation;
        cel *= light.shadowAttenuation;

        result += light.color * cel;
    }

    // final color with a bit of tint slapped on
    celRampOutput = result + celRampTint.rgb;

    // giving back main light direction in case we need it later
    lightDirection = mainLight.direction;

#endif
}