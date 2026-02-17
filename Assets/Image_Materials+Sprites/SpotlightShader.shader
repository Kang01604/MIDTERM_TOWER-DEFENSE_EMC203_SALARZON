Shader "UI/SpotlightShader"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0,0,0,0.5)
        _Center ("Center (Screen px)", Vector) = (0,0,0,0)
        _Radius ("Radius (Screen px)", Float) = 100
        _Softness ("Edge Softness", Float) = 10
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Softness;

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Get the screen position of this vertex
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Convert to actual pixel coordinates (0 to 1920, etc.)
                float2 pixelPos = (i.screenPos.xy / i.screenPos.w) * _ScreenParams.xy;
                
                // Calculate distance from the Spotlight Center
                float dist = distance(pixelPos, _Center.xy);
                
                // 1. Hard Cutoff:
                // if (dist < _Radius) return fixed4(0,0,0,0);
                
                // 2. Soft Edge (Better):
                // If dist < Radius, alpha becomes 0. If dist > Radius, alpha becomes _Color.a
                float alphaFactor = smoothstep(_Radius - _Softness, _Radius, dist);
                
                return fixed4(_Color.rgb, _Color.a * alphaFactor);
            }
            ENDCG
        }
    }
}