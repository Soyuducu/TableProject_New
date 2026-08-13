Shader "Stylized Fluids/StylizedFluidsC"
{
    Properties
    {
        // Color Variables
        [HDR] _IntersectionColorHSV("Intersection Color", Vector) = (1.0, 0.0, 1.0, 1.0)
        [HDR] _SurfaceColorHSV("Surface Color", Vector) = (1.0, 0.0, 0.9, 1.0)
        [HDR] _SubsurfaceColorHSV("Subsurface Color", Vector) = (1.0, 0.0, 0.5, 1.0)
        [HDR] _ShallowColorHSV("Shallow Color", Vector) = (1.0, 0.0, 0.75, 1.0)
        [HDR] _DeepColorHSV("Deep Color", Vector) = (1.0, 0.0, 0.0, 1.0)

        [HDR] _IntersectionColor("Intersection Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [HDR] _SurfaceColor("Surface Color", Color) = (0.9, 0.9, 0.9, 1.0)
        [HDR] _SubsurfaceColor("Subsurface Color", Color) = (0.25, 0.25, 0.25, 1.0)
        [HDR] _ShallowColor("Shallow Color", Color) = (0.5, 0.5, 0.5, 1.0)
        [HDR] _DeepColor("Deep Color", Color) = (0.0, 0.0, 0.0, 1.0)

        // Feature Specific Variables
        _NoiseTex("Noise Texture", 2D) = "White" {}
        _SurfaceTex("Surface Texture", 2D) = "White" {}
        _SubsurfaceTex("Subsurface Texture", 2D) = "White" {}

        // Surface Variables
        _SurfacePattern("Surface Pattern", int) = 4
        [KeywordEnum(FLOWMAP, TEXTURE, PATTERN)] _SURFACE_PATTERN_TYPE("Pattern Type", Float) = 2
        _SurfaceScale("Surface Scale", float) = 0.2
        _SurfaceSpeed("Surface Speed", float) = 1.0
        _SurfaceCutoff("Surface Cutoff", Vector) = (-1.5, 0.75, 0.0, 0.0)

        // Subsurface Variables
        _SubsurfacePattern("Subsurface Pattern", int) = 5
        [Toggle(_SUBSURFACE_PATTERN_TYPE)] _SUBSURFACE_PATTERN_TYPE("Texture Pattern", Float) = 1
        _SubsurfaceScale("Subsurface Scale", float) = 0.15
        _SubsurfaceSpeed("Subsurface Speed", float) = 1.0
        _SubsurfaceCutoff("Subsurface Cutoff", Vector) = (-0.75, 0.75, 0.0, 0.0)

        //InterSection Variables
        _IntersectionPattern("Intersection Pattern", int) = 6
        [Toggle(_INTERSECTION_PATTERN_TYPE)] _INTERSECTION_PATTERN_TYPE("Texture Pattern", Float) = 0
        _IntersectionScale("Intersection Scale", float) = 1.0
        _IntersectionSpeed("Intersection Speed", float) = 1.0
        _IntersectionCutoff("Intersection Cutoff", Vector) = (1.0, 1.0, 0.0, 0.0)
        _LineDensity("Line Density", float) = 0.0

        // Wave Variables
        [Toggle(_WAVES)] _WAVES("Waves", float) = 1
        _WaveSteepness("Wave Steepness", Range(0.0, 1.0)) = 0.25
        _Wavelength("Wavelength", float) = 5.0
        _WaveSpeed("Wave Speed", float) = 1.0

        // Normal Variables
        [Toggle(_USE_NORMAL_MAP)] _USE_NORMAL_MAP("Use Normal Map", Float) = 0
        _NormalTex("Normal Texture", 2D) = "Bump" {}
        _NormalStrength("Normal Strength", Range(0.0, 1.0)) = 0.01
        _NormalScale("Normal Scale", float) = 1.0

        // General Variables
        [Toggle] _RecalculateNormals("Recalculate Normals", Float) = 1
        [Toggle] _WorldSpaceUVs("World Space UVs", Float) = 1
        _FlowDirection("Flow Direction", Range(0.0, 6.28)) = 0.0
        _Depth("Depth", float) = 0.25
        _Parallax("Parallax", Range(0.0, 0.1)) = 0.025
        _LODFadeRange("LOD Fade Range", Vector) = (25.0, 50.0, 0.0, 0.0)
    }

    SubShader
    {
        Tags 
        {  
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent"
        }

        Pass
        {
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _INTERSECTION_PATTERN_TYPE
            #pragma shader_feature_local _SUBSURFACE_PATTERN_TYPE
            #pragma shader_feature_local _SURFACE_PATTERN_TYPE_FLOWMAP _SURFACE_PATTERN_TYPE_TEXTURE _SURFACE_PATTERN_TYPE_PATTERN
            #pragma shader_feature_local _WAVES
            #pragma shader_feature_local _USE_NORMAL_MAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "StylizedFluidFunctions.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float3 viewDirTS : TEXCOORD3;
                float3 normalWS  : TEXCOORD4;
                float3 tangentWS : TEXCOORD5;
            };

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_SurfaceTex);
            SAMPLER(sampler_SurfaceTex);
            TEXTURE2D(_SubsurfaceTex);
            SAMPLER(sampler_SubsurfaceTex);
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _IntersectionColorHSV;    
                half4 _SurfaceColorHSV;
                half4 _SubsurfaceColorHSV;
                half4 _ShallowColorHSV;
                half4 _DeepColorHSV;
                half4 _IntersectionColor;    
                half4 _SurfaceColor;
                half4 _SubsurfaceColor;
                half4 _ShallowColor;
                half4 _DeepColor;


                float _Depth;
                float _Parallax;
                int _SurfacePattern;
                float _SurfaceScale;
                float _SurfaceSpeed;
                half4 _SurfaceCutoff;
                int _SubsurfacePattern;
                float _SubsurfaceScale;
                float _SubsurfaceSpeed;
                half4 _SubsurfaceCutoff;
                int _IntersectionPattern;
                float _IntersectionScale;
                float _IntersectionSpeed;
                half4 _IntersectionCutoff;
                float _WaveSteepness;
                float _Wavelength;
                float _WaveSpeed;
                float _LineDensity;
                float _RecalculateNormals;
                float _WorldSpaceUVs;
                float _FlowDirection;
                half4 _LODFadeRange;
                float _NormalScale;
                float _NormalStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                    
                float3 positionOS = IN.positionOS.xyz;
                half3 normalOS = IN.normalOS;
                half3 tangentOS = half3(1.0, 0.0, 0.0);
                half3 bitangentOS = half3(0.0, 0.0, 1.0);;

                #if defined (_WAVES)
                    // Gerstner Waves
                    float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                    half3 objectScale = half3(length(UNITY_MATRIX_M[0].xyz), 0.0, length(UNITY_MATRIX_M[2].xyz));
                    objectScale.y = min(objectScale.x, objectScale.z);
                    positionOS += GerstnerWave(IN.positionOS.xyz, objectScale, positionWS, _FlowDirection, _WaveSteepness * 0.5, _Wavelength, _WaveSpeed, tangentOS, bitangentOS);
                    positionOS += GerstnerWave(IN.positionOS.xyz, objectScale, positionWS, _FlowDirection - 1.0, _WaveSteepness * 0.3, _Wavelength * 0.5, _WaveSpeed, tangentOS, bitangentOS);
                    positionOS += GerstnerWave(IN.positionOS.xyz, objectScale, positionWS, _FlowDirection + 1.0, _WaveSteepness * 0.2, _Wavelength * 0.2, _WaveSpeed, tangentOS, bitangentOS);
                    normalOS = lerp(normalOS, normalize(cross(bitangentOS, tangentOS)), _RecalculateNormals);
                #endif

                // Create position vectors
                OUT.positionHCS = TransformObjectToHClip(positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(positionOS.xyz);

                // Create viewDir Vectors
                OUT.normalWS = TransformObjectToWorldNormal(normalOS);
                OUT.tangentWS = TransformObjectToWorldDir(lerp(half3(1.0, 0.0, 0.0), tangentOS.xyz, _RecalculateNormals));
                float3 bitangentWS = cross(OUT.normalWS, OUT.tangentWS) * IN.tangentOS.w;
                float3x3 worldToTangent = float3x3(OUT.tangentWS, bitangentWS, OUT.normalWS);
                OUT.viewDirWS = GetWorldSpaceViewDir(lerp(TransformObjectToWorld(IN.positionOS.xyz), OUT.positionWS, _RecalculateNormals));
                OUT.viewDirTS = mul(worldToTangent, OUT.viewDirWS);
                OUT.uv = IN.uv;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 cameraPos = _WorldSpaceCameraPos;
                float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;
                float3 viewDirWS = normalize(IN.viewDirWS);
                float3 viewDirTS = normalize(IN.viewDirTS);
                float3 normalWS = normalize(IN.normalWS);
                float3 tangentWS = normalize(IN.tangentWS);
                        

                // Pattern UVs
                float s, c;
                sincos(_FlowDirection, s, c); 
                float2 flowDirection = float2(c, s) * frac(_Time.y * 0.0001) * 1000.0;
                float2 surfaceSpeed = flowDirection * _SurfaceSpeed;
                float2 subsurfaceSpeed = flowDirection * _SubsurfaceSpeed;
                float2 intersectionSpeed = flowDirection * _IntersectionSpeed;

                float2 uv = lerp(IN.uv, -IN.positionWS.xz * 0.25, _WorldSpaceUVs);

                float2 surfaceUV = (uv + surfaceSpeed) * 1.5 * _SurfaceScale;
                float2 surfaceCutoffUV = (uv + surfaceSpeed) * _SurfaceScale;

                half2 parallaxUV = (viewDirTS.xy / viewDirTS.z) * _Parallax;
                float2 subsurfaceUV = ((uv + parallaxUV) * 1.5 + subsurfaceSpeed + 0.5) * _SubsurfaceScale;
                float2 subsurfaceCutoffUV = ((uv + parallaxUV) + subsurfaceSpeed + 0.5) * _SubsurfaceScale;

                half2 intersectionUV = (uv + intersectionSpeed) * _IntersectionScale;
                #if defined (_USE_NORMAL_MAP)
                    // Setup Normals
                    float4 normalMap = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, (uv + surfaceSpeed) * _NormalScale);
                    float3 detailNormalTS = UnpackNormal(normalMap);
                    detailNormalTS = normalize(float3(detailNormalTS.xy * _NormalStrength, detailNormalTS.z));
                    float3 bitangentWS = cross(normalWS, tangentWS);
                    float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, normalWS);
                    normalWS = normalize(mul(detailNormalTS, tangentToWorld));

                    // Apply Refraction
                    half rawDepth = SampleSceneDepth(screenUV + detailNormalTS.xy);
                    half linearEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);       
                    half depthCheck = saturate((linearEyeDepth - IN.positionHCS.w) * 5.0);
                    screenUV = screenUV + detailNormalTS * depthCheck * _NormalStrength;
                #endif
                
                // Add Depth
                half2 depth = worldSpaceDepth(IN.viewDirWS, IN.positionHCS, screenUV, _Depth); 

                // Create patterns
                half surfacePattern = SurfacePattern(surfaceUV, surfaceCutoffUV, _SurfaceCutoff.xy, TEXTURE2D_ARGS(_SurfaceTex, sampler_SurfaceTex), TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), _SurfacePattern);
                half subsurfacePattern = SubsurfacePattern(subsurfaceUV, subsurfaceCutoffUV, _SubsurfaceCutoff.xy, depth.x, TEXTURE2D_ARGS(_SubsurfaceTex, sampler_SubsurfaceTex), TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), _SubsurfacePattern);
                half intersectionPattern = IntersectionPattern(intersectionUV, _IntersectionCutoff.xy, TEXTURE2D_ARGS(_NoiseTex, sampler_NoiseTex), _IntersectionPattern, depth.x, _LineDensity);

                // Fresnel Effect
                half fresnel = pow(1 - saturate(dot(viewDirWS, normalWS)), 8.0);

                // Blend Colors
                half4 patternMasks = half4(max(depth.y, fresnel), subsurfacePattern, surfacePattern, intersectionPattern);
                half3 Out = HSVBlend(_DeepColorHSV, _ShallowColorHSV, _SubsurfaceColorHSV, _SurfaceColorHSV, _IntersectionColorHSV, patternMasks, _LODFadeRange.xy, IN.positionWS, screenUV);

                return half4(Out, 1.0);
            }
            ENDHLSL
        }
    }
    
    CustomEditor "FirstCrowCode.FluidShader.FluidShaderGUI"
}
