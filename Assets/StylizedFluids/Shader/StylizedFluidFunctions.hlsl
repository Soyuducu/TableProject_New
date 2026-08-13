#ifndef STYLIZEDFLUID_FUNCTIONS_INCLUDED
#define STYLIZEDFLUID_FUNCTIONS_INCLUDED

half2 worldSpaceDepth(float3 viewVectorWS, float4 positionHCS, float2 screenUV, half depth)
{
    float3 cameraPos = _WorldSpaceCameraPos;
    half rawDepth = SampleSceneDepth(screenUV);
    half linearEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);           
    half linearDepth = saturate(viewVectorWS.y * ((linearEyeDepth / positionHCS.w) - 1) / depth);
    half expDepth = saturate(exp(-viewVectorWS.y * ((linearEyeDepth / positionHCS.w) - 1) / depth));
    return half2(linearDepth, expDepth);
}

half DistanceBasedFade(float3 positionWS, half2 lodFadeRange)
{
    float3 cameraToPixel = positionWS - _WorldSpaceCameraPos;
    float distOffset = length(cameraToPixel) - lodFadeRange.x;
    float fadeRangeLength = lodFadeRange.y - lodFadeRange.x;
    fadeRangeLength = max(fadeRangeLength, 0.001f);
    float normalizedDistFactor = distOffset / fadeRangeLength;
    return 1 - saturate(normalizedDistFactor);
}

half3 HSVToRGB(half3 In)
{
    half4 K = half4(1.0, 0.667, 0.333, 3.0);
    half3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
    return In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
}

half3 HSVLerp(half3 A, half3 B, half T)
{
    half d = B.x - A.x;
    A.x += step(0.5, d);
    B.x += step(0.5, -d);
    half3 result = lerp(A, B, T);
    result.x = frac(result.x);
    return result;
}

half3 HSVBlend(half4 A, half4 B, half4 C, half4 D, half4 E, half4 T, half2 LODFadeRange, float3 positionWS, half2 screenUV)
{
    half fadeFactor = DistanceBasedFade(positionWS, LODFadeRange);
    half3 sceneColor = SampleSceneColor(screenUV);
    A.xyz = lerp(sceneColor, A.xyz, A.w);
    B.xyz = lerp(sceneColor, B.xyz, B.w);
    half3 AB = HSVLerp(A.xyz, B.xyz, T.x);
    half3 ABC = HSVLerp(AB, C.xyz, C.w * T.y * fadeFactor);
    half3 ABCD = HSVLerp(ABC, D.xyz, D.w * T.z * fadeFactor);
    half3 ABCDE = HSVLerp(ABCD, E.xyz, E.w * T.w * fadeFactor);
    return HSVToRGB(ABCDE);
}

half3 GerstnerWave(half3 objectPos, half3 objectScale, half3 worldPos, half waveDirection, half waveSteepness, half wavelength, half waveSpeed, inout half3 tangent, inout half3 bitangent)
{
    half k = 6.2832 / wavelength;
    half2 d = half2(cos(waveDirection), sin(waveDirection));
    half f = k * (dot(worldPos.xz, d) - waveSpeed * _Time.y);
    half a = waveSteepness / k;

    half s, c;
    sincos(f, s, c);
    
    tangent += half3(-d.x * d.x * waveSteepness * s, d.x * waveSteepness * c, -d.x * d.y * waveSteepness * s);
    bitangent += half3(-d.x * d.y * waveSteepness * s, d.y * waveSteepness * c, -d.y * d.y * waveSteepness * s);
    return half3(d.x * a * c, a * s, d.y * a * c) / objectScale;
}

float3 BlendNormalsRNM(float3 baseNormal, float3 detailNormal)
{
    float3 t = baseNormal + float3(0.0, 0.0, 1.0);
    float3 u = detailNormal * float3(-1.0, -1.0, 1.0);
    float3 blended = t * dot(t, u) / t.z - u;
    return normalize(blended);
}

half uniformDots(half2 patternUV)
{
    return length(frac(patternUV) + half2(-0.5, -0.5));
}

half hatched(half2 patternUV)
{
    half2 centeredUV = patternUV - 0.5;
    return abs(frac(centeredUV.x + centeredUV.y) - 0.5);
}

half diamonds(half2 patternUV)
{
    half2 grid = frac(patternUV);
    half2 centered = abs(grid - 0.5);
    return abs(2 * (abs(centered.x + centered.y) - 0.5));
}

half flowMap(half2 patternUV, TEXTURE2D_PARAM(baseTexture, sampler_baseTexture), half2 flowMappatternUV)
{
    half flowMapStrength = 0.1;
    half time = _Time.y * 0.1;
    
    half2 patternUVA = patternUV + flowMappatternUV * flowMapStrength * frac(time);
    half2 patternUVB = patternUV + flowMappatternUV * flowMapStrength * frac(time + .33333);
    half2 patternUVC = patternUV + flowMappatternUV * flowMapStrength * frac(time + .66667);
    
    half A = SAMPLE_TEXTURE2D(baseTexture, sampler_baseTexture, patternUVA).z;
    half B = SAMPLE_TEXTURE2D(baseTexture, sampler_baseTexture, patternUVB).z;
    half C = SAMPLE_TEXTURE2D(baseTexture, sampler_baseTexture, patternUVC).z;
    
    A = A * (cos(6.28319 * frac(time) - 3.14) + 1);
    B = B * (cos(6.28319 * frac(time + .33333) - 3.14) + 1);
    C = C * (cos(6.28319 * frac(time + .66667) - 3.14) + 1);
 
    return .33 * (A + B + C);
}

half SurfacePattern(float2 patternUV, float2 cutoffUV, half2 cutoffRange, TEXTURE2D_PARAM(patternTexture, sampler_patternTexture), TEXTURE2D_PARAM(noiseTexture, sampler_noiseTexture), int patternIndex)
{
    half cutoff = lerp(cutoffRange.x, cutoffRange.y, SAMPLE_TEXTURE2D(noiseTexture, sampler_noiseTexture, cutoffUV).w);
    
    #if defined(_SURFACE_PATTERN_TYPE_FLOWMAP)
        // Flowing Ripples
        float remappedNoise = 2.0 * (SAMPLE_TEXTURE2D(noiseTexture, sampler_noiseTexture, patternUV)).w - 1.0;
        return step(flowMap(patternUV, TEXTURE2D_ARGS(noiseTexture, sampler_noiseTexture), float2(remappedNoise, remappedNoise)), cutoff);
    #elif defined(_SURFACE_PATTERN_TYPE_TEXTURE)
        float4 noise = SAMPLE_TEXTURE2D(noiseTexture, sampler_noiseTexture, patternUV);
        [branch]
        switch(patternIndex)
        {
            case 1: // Static Ripples
                return step(noise.z, cutoff); 
                break;
            case 2: // Edge Voronoi
                return step(noise.y, cutoff); 
                break;
            case 6: // Random Dots
                return 1.0 - step(noise.x, cutoff); 
                break;
            case 7: // Custom Texture
                return SAMPLE_TEXTURE2D(patternTexture, sampler_patternTexture, patternUV).x; 
                break;
            default:
                return 0.0; 
                break;
        }
    #elif defined(_SURFACE_PATTERN_TYPE_PATTERN)
        [branch]
        switch (patternIndex)
        {
            case 3: // Diamonds
                return 1.0 - step(diamonds(patternUV * 10.0), 1.0 - cutoff);
                break;
            case 4: // Hatches
                return step(1.0 - hatched(patternUV * 15.0), cutoff);
                break;
            case 5: // Uniform Dots
                return step(uniformDots(patternUV * 15.0), cutoff);
                break;
            default: // None
                return 0.0;
                break;
        }
    #endif
}

half SubsurfacePattern(float2 patternUV, float2 cutoffUV, half2 cutoffRange, half linearDepth, TEXTURE2D_PARAM(patternTexture, sampler_patternTexture), TEXTURE2D_PARAM(noiseTexture, sampler_noiseTexture), int patternIndex)
{
    half cutoff = lerp(cutoffRange.x, cutoffRange.y, SAMPLE_TEXTURE2D(noiseTexture, sampler_noiseTexture, cutoffUV).w) * (linearDepth * linearDepth);
    
    #if defined(_SUBSURFACE_PATTERN_TYPE)
        half4 noise = SAMPLE_TEXTURE2D(noiseTexture, sampler_noiseTexture, patternUV);
        [branch]
        switch(patternIndex)
        {
            case 0: // Static Ripples
                return step(noise.z, cutoff); 
                break;
            case 1: // Edge Voronoi
                return step(noise.y, cutoff); 
                break;
            case 5: // Random Dots
                return 1.0 - step(noise.x, 1.0 - cutoff); 
                break;
            case 6: // Custom Texture
                return SAMPLE_TEXTURE2D(patternTexture, sampler_patternTexture, patternUV).x; 
                break;
            default: 
                return 0.0; 
                break;
        }
    #else
        [branch]
        switch (patternIndex)
        {
            case 2: // Diamonds
                return 1.0 - step(diamonds(patternUV * 10.0), 1.0 - cutoff);
                break;
            case 3: // Hatches
                return step(1.0 - hatched(patternUV * 15.0), cutoff);
                break;
            case 4: // Uniform Dots
                return step(uniformDots(patternUV * 15.0), cutoff);
                break;
            default: // None
                return 0.0;
                break;
        }
    #endif
}

half IntersectionPattern(float2 uv, half2 cutoffRange, TEXTURE2D_PARAM(noiseTexture, sampler_noiseTexture), int patternIndex, half Depth, half LineDensity)
{
    half x = LineDensity * Depth - 0.5;
    half sinDepth = (abs(fmod(x, 2.0) - 1.0)) * (1.0 - Depth);
    #if defined(_INTERSECTION_PATTERN_TYPE)
    half4 noise = SAMPLE_TEXTURE2D(noiseTexture, sampler_noiseTexture, uv);
    [branch]
    switch(patternIndex)
    {
        case 0: // Static Ripples
            return step(lerp(cutoffRange.x, cutoffRange.y, noise.z), sinDepth);
            break;
        case 1: // Edge Voronoi
            return step(lerp(cutoffRange.x, cutoffRange.y, noise.y), sinDepth);
            break;
        case 5: // Random Dots
            return step(lerp(cutoffRange.x, cutoffRange.y, (1.0 - noise.x)), sinDepth);
            break;
        default: 
            return 0.0;
            break;
    }
    #else
        [branch]
        switch (patternIndex)
        {
            case 2: // Diamonds
                half diamondPattern = 1 - diamonds(uv * 10.0);
                return step(lerp(cutoffRange.x, cutoffRange.y, diamondPattern), sinDepth);
                break;
            case 3: // Hatches
                half hatchedPattern = hatched(uv * 15.0);
                return step(lerp(cutoffRange.x, cutoffRange.y, (1.0 - hatchedPattern)), sinDepth);
                break;
            case 4: // Uniform Dots
                half uniformDotsPattern = uniformDots(uv * 15.0);
                return step(lerp(cutoffRange.x, cutoffRange.y, uniformDotsPattern), sinDepth);
                break;
            case 6: //Simple
                return 1.0 - step(sinDepth, 0.5 * (cutoffRange.x + cutoffRange.y));
                break;
            default: // None
                return 0.0;
                break;
        }
    #endif 
}

#endif
