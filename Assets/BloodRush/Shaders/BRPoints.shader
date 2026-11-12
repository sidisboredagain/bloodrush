// three.js PointsMaterial: each point is a quad (4 vertices sharing the centre, uv = corner) expanded in screen space.
// _Atten 1: size in world units scaled by perspective (sizeAttenuation); 0: size in pixels.
Shader "BR/Points"
{
    Properties
    {
        _MainTex ("Map", 2D) = "white" {} _Size ("Size", Float) = 1 _Atten ("Attenuation", Float) = 0 _PxScale ("Pixel Scale", Float) = 1
        _Opacity ("Opacity", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _Src ("Src", Float) = 5 [Enum(UnityEngine.Rendering.BlendMode)] _Dst ("Dst", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend [_Src] [_Dst] ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _Size, _Atten, _PxScale, _Opacity;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            v2f vert(appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(float4(v.vertex.xyz, 1));
                float depth = max(-UnityObjectToViewPos(v.vertex.xyz).z, 1e-3);
                float px = _Atten > 0.5 ? _Size * _ScreenParams.y * 0.5 / depth : _Size * _PxScale;
                o.pos.xy += v.uv * 2.0 * px / _ScreenParams.xy * o.pos.w;
                o.uv = v.uv + 0.5; o.color = v.color; return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 t = tex2D(_MainTex, float2(i.uv.x, 1 - i.uv.y));
                float a = t.a * i.color.a * _Opacity;
                return float4(t.rgb * i.color.rgb, a);
            }
            ENDCG
        }
    }
}
