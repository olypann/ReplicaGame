
void CelShading_float(in float3 Normal, in float CelRampSmoothness, in float3 ClipSpacePos, in float3 WorldPos, in float4 CelRampTinting,
in float CelRampOffset, out float3 CelRampOutput, out float3 Direction)
{
 
    // shader graph nodes
    #ifdef SHADERGRAPH_PREVIEW
        CelRampOutput = float3(0.5,0.5,0);
        Direction = float3(0.5,0.5,0);
    #else
 
        // input  the shadow coordinates
        #if SHADOWS_SCREEN
            half4 shadowCoord = ComputeScreenPos(ClipSpacePos);
        #else
            half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
        #endif 
 
        // input the main light
        #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
            Light light = GetMainLight(shadowCoord);
        #else
            Light light = GetMainLight();
        #endif
 
        // dot product for celramp
        half d = dot(Normal, light.direction) * 0.5 + 0.5;
        
        // celramp in a smoothstep
        half celRamp = smoothstep(CelRampOffset, CelRampOffset+ CelRampSmoothness, d );
        // multiply with shadows;
        celRamp *= light.shadowAttenuation;
        // lights and tint
        CelRampOutput = light.color * (celRamp + CelRampTinting) ;
        // rimlight direction
        Direction = light.direction;
    #endif
 
}