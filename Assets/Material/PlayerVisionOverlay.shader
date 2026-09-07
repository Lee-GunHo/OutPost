Shader "OutPost/Player Vision Overlay"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
        _VisionCenter ("Vision Center", Vector) = (0.5, 0.5, 0, 0)
        _ScreenScale ("Screen Scale", Vector) = (1, 1, 0, 0)
        _VisionRadius ("Vision Radius", Float) = 0.3
        _EdgeSoftness ("Edge Softness", Float) = 0.2
        _DarknessOpacity ("Darkness Opacity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _VisionCenter;
            float4 _ScreenScale;
            float _VisionRadius;
            float _EdgeSoftness;
            float _DarknessOpacity;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                // Equal pixel distances must give equal darkness on any aspect ratio.
                float distanceFromPlayer = length((input.uv - _VisionCenter.xy) * _ScreenScale.xy);
                float alpha = smoothstep(_VisionRadius, _VisionRadius + max(_EdgeSoftness, 0.001), distanceFromPlayer);
                return fixed4(0, 0, 0, alpha * saturate(_DarknessOpacity));
            }
            ENDCG
        }
    }
    Fallback Off
}
