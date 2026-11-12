// Additive glow (sprites, beams, portals, muzzle flashes). Fog fades the glow out with distance.
Shader "BR/Additive"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Texture", 2D) = "white" {}
        _Cull ("Cull", Float) = 0
        _UseFog ("Use Fog", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _BaseMap; float4 _BaseMap_ST;
            float4 _BaseColor; float _UseFog;
            float4 _BRFog;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                float d = distance(wp, _WorldSpaceCameraPos);
                o.fog = saturate((d - _BRFog.x) / max(0.001, _BRFog.y - _BRFog.x)) * _UseFog;
                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                float4 t = tex2D(_BaseMap, i.uv);
                float3 c = t.rgb * _BaseColor.rgb * t.a * _BaseColor.a * (1 - i.fog);
                return float4(c, 1);
            }
            ENDCG
        }
    }
}
