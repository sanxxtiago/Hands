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

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)

                float4 _ShadowColor;
                float _ShadowStrength;
                float _ShadowThreshold;

            CBUFFER_END


            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;

                return output;
            }


            half4 Frag(Varyings input) : SV_Target
            {
                /*
                 * Obtiene la luz principal incluyendo
                 * la información de sombras para esta
                 * posición concreta del mundo.
                 */
                Light mainLight = GetMainLight();

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