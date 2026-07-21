Shader "Mahjong Prototype/Reaction Highlight Shell"
{
    Properties
    {
        [MainColor] _RimColor("Rim Color", Color) = (1, 0.88, 0.55, 1)
        _Alpha("Alpha", Range(0, 1)) = 0.18
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 4
        _FresnelIntensity("Fresnel Intensity", Range(0, 2)) = 1
        _PulseStrength("Pulse Strength", Range(0, 2)) = 1
        _VertexExtrusion("Vertex Normal Extrusion", Range(0, 0.01)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "ReactionHighlightShell"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _RimColor;
                half _Alpha;
                half _FresnelPower;
                half _FresnelIntensity;
                half _PulseStrength;
                half _VertexExtrusion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 normalOS = normalize(input.normalOS);
                float3 positionOS = input.positionOS.xyz + normalOS * _VertexExtrusion;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalWS = normalize(input.normalWS);
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(
                    saturate(1.0h - saturate(dot(normalWS, viewDirectionWS))),
                    max(_FresnelPower, 0.001h));
                half rimStrength = saturate(fresnel * _FresnelIntensity * _PulseStrength);
                half finalAlpha = saturate(rimStrength * _Alpha);
                half3 finalColor = lerp(half3(1.0h, 1.0h, 1.0h), _RimColor.rgb, rimStrength);

                return half4(finalColor * finalAlpha, finalAlpha);
            }
            ENDHLSL
        }
    }
}
