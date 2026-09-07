Shader "Hands/Hunter/Duck Spawn Reveal"
{
    Properties
    {
        _DuckTint("Color base", Color) = (1, 1, 1, 1)
        _DuckPortalPositionX("Posicion X de la barra", Float) = 0
        _DuckSpawnDirection("Direccion de salida", Float) = 0
        _DuckRevealEdgeWidth("Ancho del borde", Range(0.001, 1)) = 0.025
        _DuckRevealEdgeTint("Color del borde", Color) = (0.35, 0.85, 1, 1)
        _DuckRevealEmission("Emision del borde", Range(0, 10)) = 2
        _DuckOpacity("Opacidad", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "AlphaTest" }
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

            float4 _DuckTint;
            float _DuckPortalPositionX;
            float _DuckSpawnDirection;
            float _DuckRevealEdgeWidth;
            float4 _DuckRevealEdgeTint;
            float _DuckRevealEmission;
            float _DuckOpacity;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float distanceToPortal =
                    (input.positionWS.x - _DuckPortalPositionX) * _DuckSpawnDirection;
                float isPortalSpawn = step(0.5, abs(_DuckSpawnDirection));
                clip(lerp(1.0, distanceToPortal, isPortalSpawn));
                clip(_DuckOpacity - 0.01);
                float edge = isPortalSpawn *
                    (1.0 - smoothstep(0.0, _DuckRevealEdgeWidth, distanceToPortal));
                half3 color = _DuckTint.rgb +
                    _DuckRevealEdgeTint.rgb * (edge * _DuckRevealEmission);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
