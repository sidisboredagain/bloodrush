// Highlight for the last enemies of a wave: a rim glow that shows ONLY where the body is hidden behind something.
// BR/XRayMask runs first and stamps the stencil wherever the enemy is directly visible; this glow then skips those
// pixels, so visible parts (and parts merely behind the enemy's own body) never glow.
Shader "BR/XRay"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,0.2,0.1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+21" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            ZTest Greater
            Cull Off
            Stencil { Ref 64 ReadMask 64 Comp NotEqual }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _BaseColor;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.v = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                float rim = 1 - abs(dot(normalize(i.n), i.v));
                return float4(_BaseColor.rgb * (0.12 + rim * rim * 1.4), 1);
            }
            ENDCG
        }
    }
}
