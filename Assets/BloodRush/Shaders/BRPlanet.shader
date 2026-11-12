// The world from orbit: physically based day side (GGX on the oceans), ambient, and night-side city lights resolved
// per pixel from 3D noise. _Mode 1 draws the lit, transparent cloud shell instead. Port of the HTML game's buildWorld().
Shader "BR/Planet"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {} _LitTex ("Night lights", 2D) = "black" {} _RoughTex ("Roughness", 2D) = "white" {}
        _EmisColor ("Emissive", Color) = (1,0.76,0.48,1) _EmisI ("Emissive Intensity", Float) = 1.3
        _LightDir ("Light Dir", Vector) = (-1,0,-1,0) _LightCol ("Light Color", Color) = (1,0.94,0.86,1) _LightI ("Light Intensity", Float) = 3.4
        _AmbCol ("Ambient", Color) = (0.1,0.14,0.22,1) _AmbI ("Ambient Intensity", Float) = 0.25
        _Mode ("Mode", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _Src ("Src", Float) = 1 [Enum(UnityEngine.Rendering.BlendMode)] _Dst ("Dst", Float) = 0 _ZW ("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend [_Src] [_Dst] ZWrite [_ZW]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _LitTex, _RoughTex;
            float4 _EmisColor, _LightDir, _LightCol, _AmbCol; float _EmisI, _LightI, _AmbI, _Mode;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wn : TEXCOORD1; float3 wp : TEXCOORD2; float3 op : TEXCOORD3; };
            v2f vert(appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy;
                o.wn = normalize(mul((float3x3)unity_ObjectToWorld, v.normal)); o.wp = mul(unity_ObjectToWorld, v.vertex).xyz; o.op = normalize(v.vertex.xyz);
                return o;
            }
            float h3(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float vn(float3 x)
            {
                float3 i = floor(x), f = frac(x); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(h3(i), h3(i + float3(1, 0, 0)), f.x), lerp(h3(i + float3(0, 1, 0)), h3(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(h3(i + float3(0, 0, 1)), h3(i + float3(1, 0, 1)), f.x), lerp(h3(i + float3(0, 1, 1)), h3(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }
            float4 frag(v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv);
                float3 N = normalize(i.wn), L = normalize(_LightDir.xyz), V = normalize(_WorldSpaceCameraPos - i.wp);
                float dNL = saturate(dot(N, L));
                float3 irr = _LightCol.rgb * _LightI * dNL;
                float3 amb = _AmbCol.rgb * _AmbI;
                if (_Mode > 0.5) return float4(tex.rgb * (irr + amb) / UNITY_PI, tex.a); // cloud shell: lambert, alpha from the map
                float3 albedo = tex.rgb;
                float rough = max(tex2D(_RoughTex, i.uv).r, 0.0525); float a = rough * rough;
                float3 H = normalize(L + V); float dNH = saturate(dot(N, H)), dNV = abs(dot(N, V)) + 1e-5, dVH = saturate(dot(V, H));
                float F = 0.04 + 0.96 * exp2((-5.55473 * dVH - 6.98316) * dVH);
                float a2 = a * a, gv = dNL * sqrt(a2 + (1 - a2) * dNV * dNV), gl = dNV * sqrt(a2 + (1 - a2) * dNL * dNL);
                float Vis = 0.5 / max(gv + gl, 1e-6), den = dNH * dNH * (a2 - 1) + 1, D = a2 / (UNITY_PI * den * den);
                float3 col = irr * (albedo / UNITY_PI + F * Vis * D) + amb * albedo / UNITY_PI;
                float reg = tex2D(_LitTex, i.uv).r;
                float sprawl = vn(i.op * 34.0) * 0.6 + vn(i.op * 71.0) * 0.4, streets = vn(i.op * 190.0), dots = vn(i.op * 520.0);
                float lit = smoothstep(0.5, 0.68, sprawl) * (0.35 + 0.65 * smoothstep(0.35, 0.75, streets)) + smoothstep(0.78, 0.86, dots) * 0.5 * smoothstep(0.3, 0.5, sprawl);
                lit *= smoothstep(0.15, 0.6, reg) * reg;
                col += _EmisColor.rgb * _EmisI * float3(lit * 1.6, lit * 1.25, lit);
                return float4(col, 1);
            }
            ENDCG
        }
    }
}
