Shader "Hands/Hunter/Duck Spawn Bar"
{
    Properties
    {
        _BarInactiveTint("Color inactivo", Color) = (0.13, 0.15, 0.17, 1)
        _BarActiveTint("Color activo", Color) = (0.68, 0.82, 0.95, 1)
        _BarGlowTint("Color de emision", Color) = (0.68, 0.82, 0.95, 1)
        _BarIdleGlow("Emision inactiva", Range(0, 10)) = 0.03
        _BarPeakGlow("Emision de pulso", Range(0, 10)) = 2.4
        _BarPulse("Pulso", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _BarInactiveTint;
            float4 _BarActiveTint;
            float4 _BarGlowTint;
            float _BarIdleGlow;
            float _BarPeakGlow;
            float _BarPulse;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(
                    lerp(_BarInactiveTint.rgb, _BarActiveTint.rgb, _BarPulse) +
                    _BarGlowTint.rgb * lerp(_BarIdleGlow, _BarPeakGlow, _BarPulse),
                    1.0);
            }
            ENDHLSL
        }
    }
}
