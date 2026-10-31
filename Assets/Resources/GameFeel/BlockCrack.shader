// 블록 금 가는 효과 (BreakableWallCrackView 전용)
// 원본 블록과 같은 메시를 한 번 더 그리면서, 월드 좌표 기준으로 금 텍스처를 입힘(메시 UV와 무관).
// 텍스처 R = 그 픽셀이 나타나는 순서(0~1), A = 금이 있는 픽셀. _Crack 이하인 픽셀만 그려서 데미지에 따라 금이 자라남.
// Resources 폴더에 있어야 빌드에 포함되고 Resources.Load로 찾을 수 있음.
Shader "OutPost/BlockCrack"
{
    Properties
    {
        _CrackTex ("Crack Texture", 2D) = "black" {}
        _CrackColor ("Crack Color", Color) = (0.08, 0.06, 0.05, 0.85)
        _Crack ("Crack Amount", Range(0, 1)) = 0
        _Tiling ("Texture Tiles Per Unit", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+40" }

        Pass
        {
            Name "BlockCrack"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            // 같은 면 위에 겹쳐 그리므로 깊이를 살짝 당겨 z-fighting 방지
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CrackTex);
            SAMPLER(sampler_CrackTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _CrackColor;
                float _Crack;
                float _Tiling;
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
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 블록 면은 축에 맞춰져 있으므로 가장 가까운 축 평면 하나로만 투영(픽셀이 번지지 않게)
                float3 n = abs(input.normalWS);
                float2 uv;

                if (n.y >= n.x && n.y >= n.z)
                    uv = input.positionWS.xz;
                else if (n.x >= n.z)
                    uv = input.positionWS.zy;
                else
                    uv = input.positionWS.xy;

                half4 crack = SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uv * _Tiling);

                if (crack.a < 0.5 || crack.r > _Crack)
                    discard;

                return _CrackColor;
            }
            ENDHLSL
        }
    }
}
