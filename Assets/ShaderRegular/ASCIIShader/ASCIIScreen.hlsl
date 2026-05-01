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

    #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
        mainLight = GetMainLight(shadowCoord);
    #else
        mainLight = GetMainLight();
    #endif


    float ndotlMain = saturate(dot(normal, mainLight.direction));


    float mainCel = smoothstep(
        celRampOffset,
        celRampOffset + celRampSmoothness,
        ndotlMain
    );

    mainCel *= mainLight.shadowAttenuation;

    result += mainLight.color * mainCel;



    // extra lights (point / spot etc)
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


        // fade + shadows
        cel *= light.distanceAttenuation;
        cel *= light.shadowAttenuation;

        result += light.color * cel;
    }



    // final mix
    celRampOutput = result + celRampTint.rgb;


    // main light direction for debugging / other effects
    lightDirection = mainLight.direction;

#endif
}