
Shader "FightForLife/Water_URP"
{
    Properties
    {
        _BaseColor ("Water Color", Color) = (0.05, 0.28, 0.38, 0.7)
        _DeepColor ("Deep Color", Color) = (0.02, 0.15, 0.25, 0.85)
        _Smoothness ("Smoothness", Range(0,1)) = 0.95
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _NormalMap1 ("Normal Map 1", 2D) = "bump" {}
        _NormalMap2 ("Normal Map 2", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,2)) = 0.6

        _WaveSpeed1 ("Wave Speed 1", Vector) = (0.02, 0.015, 0, 0)
        _WaveSpeed2 ("Wave Speed 2", Vector) = (-0.015, 0.02, 0, 0)
        _WaveTiling1 ("Wave Tiling 1", Float) = 0.04
        _WaveTiling2 ("Wave Tiling 2", Float) = 0.06

        _VertexWaveHeight ("Wave Height", Range(0, 0.5)) = 0.12
        _VertexWaveFreq ("Wave Frequency", Range(0.001, 0.1)) = 0.015
        _VertexWaveSpeed ("Wave Anim Speed", Range(0, 3)) = 0.6

        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3.5
        _FresnelColor ("Fresnel Color", Color) = (0.35, 0.6, 0.75, 1)

        _FoamColor ("Foam Color", Color) = (0.85, 0.92, 0.97, 0.5)
        _FoamAmount ("Foam Amount", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 viewDirWS : TEXCOORD5;
                float fogFactor : TEXCOORD6;
            };

            TEXTURE2D(_NormalMap1); SAMPLER(sampler_NormalMap1);
            TEXTURE2D(_NormalMap2); SAMPLER(sampler_NormalMap2);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DeepColor;
                float _Smoothness;
                float _Metallic;
                float _NormalStrength;
                float4 _WaveSpeed1;
                float4 _WaveSpeed2;
                float _WaveTiling1;
                float _WaveTiling2;
                float _VertexWaveHeight;
                float _VertexWaveFreq;
                float _VertexWaveSpeed;
                float _FresnelPower;
                float4 _FresnelColor;
                float4 _FoamColor;
                float _FoamAmount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Convert to world space FIRST, then apply gentle waves
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float time = _Time.y;

                // Gentle waves using world-space coords with very low frequency
                // This works correctly even on a 1500m plane
                float wave1 = sin(posWS.x * _VertexWaveFreq + time * _VertexWaveSpeed) * _VertexWaveHeight;
                float wave2 = sin(posWS.z * _VertexWaveFreq * 1.3 + time * _VertexWaveSpeed * 0.7) * _VertexWaveHeight * 0.7;
                float wave3 = sin((posWS.x + posWS.z) * _VertexWaveFreq * 0.8 + time * _VertexWaveSpeed * 1.1) * _VertexWaveHeight * 0.4;

                // Apply wave to object-space Y
                float3 posOS = input.positionOS.xyz;
                posOS.y += wave1 + wave2 + wave3;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(posOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;

                // Scrolling normal maps for surface detail
                float2 uv1 = input.positionWS.xz * _WaveTiling1 + _WaveSpeed1.xy * time;
                float2 uv2 = input.positionWS.xz * _WaveTiling2 + _WaveSpeed2.xy * time;

                float3 normal1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap1, sampler_NormalMap1, uv1));
                float3 normal2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap2, sampler_NormalMap2, uv2));

                // Blend two normal maps for complex ripple pattern
                float3 blendedNormal = normalize(float3(
                    (normal1.xy + normal2.xy) * _NormalStrength,
                    normal1.z * normal2.z
                ));

                // Transform normal to world space
                float3x3 TBN = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 worldNormal = normalize(mul(blendedNormal, TBN));

                // Fresnel - water edges appear lighter and more reflective
                float fresnel = pow(1.0 - saturate(dot(input.viewDirWS, worldNormal)), _FresnelPower);

                // Water color - blend base and deep
                float4 waterColor = lerp(_BaseColor, _DeepColor, 0.4);
                waterColor = lerp(waterColor, _FresnelColor, fresnel * 0.5);

                // Subtle foam using world-space low-frequency pattern
                float foamNoise = sin(input.positionWS.x * _VertexWaveFreq + time * _VertexWaveSpeed) * 0.5 + 0.5;
                float foam = smoothstep(1.0 - _FoamAmount, 1.0, foamNoise);
                waterColor = lerp(waterColor, _FoamColor, foam * 0.3);

                // Main light
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(worldNormal, mainLight.direction));

                // Specular highlight (sun glint on water)
                float3 halfDir = normalize(mainLight.direction + input.viewDirWS);
                float spec = pow(saturate(dot(worldNormal, halfDir)), 256.0 * _Smoothness);

                // Final color composition
                float3 ambient = waterColor.rgb * 0.25;
                float3 diffuse = waterColor.rgb * NdotL * 0.6 * mainLight.color;
                float3 specular = spec * mainLight.color * 0.7;
                float3 rim = fresnel * _FresnelColor.rgb * 0.15;

                float3 finalColor = ambient + diffuse + specular + rim;

                // Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                // Alpha - more opaque where looking straight down, transparent at edges
                float alpha = waterColor.a + fresnel * 0.15 + spec * 0.05;
                alpha = saturate(alpha);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
