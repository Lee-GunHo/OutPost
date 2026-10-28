// 상호작용 대상 외곽선 (InteractionHighlight 전용)
// 뒷면만 그리면서 법선 방향으로 화면 픽셀 기준 _OutlineWidth만큼 부풀려, 원본 바깥 테두리만 보이게 한다.
// Resources 폴더에 있어야 빌드에 포함되고 Resources.Load로 찾을 수 있음.
Shader "OutPost/InteractionOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width (px)", Float) = 3
    }

    SubShader
    {
        // 불투명 패스(뎁스 프라이밍 시 ZTest Equal 강제)에 섞이지 않도록 반투명 큐에서 그림
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+50" }

        Pass
        {
            Name "InteractionOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float2 normalCS = TransformWorldToHClipDir(normalWS).xy;

                float normalLength = length(normalCS);
                normalCS = normalLength > 1e-5 ? normalCS / normalLength : float2(0, 0);

                // 클립 공간 xy 1 = 화면 절반 → 픽셀 단위로 환산, w를 곱해 원근과 무관하게 일정한 두께
                positionCS.xy += normalCS * (_OutlineWidth * 2.0 / _ScreenParams.xy) * positionCS.w;

                output.positionCS = positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
