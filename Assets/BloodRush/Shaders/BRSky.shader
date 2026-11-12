// Dawn sky dome: gradient, sun disc, glow and horizon haze (HDR so the sun blooms). Port of the HTML game's makeSky().
Shader "BR/Sky"
{
    Properties
    {
        _Top ("Top", Color) = (0,0,1,1) _Mid ("Mid", Color) = (0,0,1,1) _Horizon ("Horizon", Color) = (1,0.5,0,1) _Bottom ("Bottom", Color) = (0,0,0,1)
        _SunColor ("Sun Color", Color) = (1,0.8,0.6,1) _SunDir ("Sun Dir", Vector) = (0,0.05,-1,0)
        _SunSize ("Sun Size", Float) = 0.035 _Glow ("Glow", Float) = 1 _SunPower ("Sun Power", Float) = 9
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Top, _Mid, _Horizon, _Bottom, _SunColor, _SunDir; float _SunSize, _Glow, _SunPower;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o; o.dir = normalize(v.vertex.xyz); o.pos = UnityObjectToClipPos(v.vertex);
                #if UNITY_REVERSED_Z
                o.pos.z = o.pos.w * 1e-6;
                #else
                o.pos.z = o.pos.w * 0.999999;
                #endif
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir); float h = d.y;
                float3 col = h > 0.0 ? lerp(lerp(_Horizon.rgb, _Mid.rgb, smoothstep(0.0, 0.22, h)), _Top.rgb, smoothstep(0.18, 0.85, h)) : lerp(_Horizon.rgb, _Bottom.rgb, smoothstep(0.0, -0.25, h));
                float3 s = normalize(_SunDir.xyz); float c = max(dot(d, s), 0.0);
                float disc = smoothstep(cos(_SunSize), cos(_SunSize * 0.82), c);
                float halo = pow(c, 6.0) * 0.45 + pow(c, 48.0) * 1.1 + pow(c, 600.0) * 2.5;
                float haze = exp(-abs(h - s.y) * 9.0) * pow(c, 2.5) * 0.6;
                col += _SunColor.rgb * (halo * _Glow + haze * _Glow + disc * _SunPower);
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}
