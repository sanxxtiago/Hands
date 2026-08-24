Shader "Custom/ShadowCatcher"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.65
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ShadowCatcher"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing

            // Fuerza el camino de sombras para materiales transparentes, incluso si se usa screen-space shadows.
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)

                float4 _ShadowColor;
                float _ShadowStrength;
                float _ShadowThreshold;

            CBUFFER_END


            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;

                return output;
            }


            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half shadowAttenuation = mainLight.shadowAttenuation;

                /*
                 * 1 = sombra completa
                 * 0 = sin sombra
                 */
                half shadowAmount = 1.0h - shadowAttenuation;

                /*
                 * El threshold permite eliminar pequeñas
                 * diferencias producidas por iluminación.
                 */
                shadowAmount = saturate(
                    (shadowAmount - _ShadowThreshold) /
                    max(1.0h - _ShadowThreshold, 0.001h)
                );

                half alpha = shadowAmount * _ShadowStrength;

                return half4(
                    _ShadowColor.rgb,
                    alpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
