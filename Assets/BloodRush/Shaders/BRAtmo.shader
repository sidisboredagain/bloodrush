// Planet atmosphere (additive). _Mode 0: outer rim shell seen from inside (BackSide); _Mode 1: haze just above the surface.
Shader "BR/Atmo"
{
    Properties { _SunDir ("Sun Dir", Vector) = (-1,0,-1,0) _Col ("Color", Color) = (0.31,0.61,1,1) _Mode ("Mode", Float) = 0 _Cull ("Cull", Float) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend One One ZWrite Off Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _SunDir, _Col; float _Mode;
            struct v2f { float4 pos : SV_POSITION; float3 wn : TEXCOORD0; float3 wp : TEXCOORD1; };
            v2f vert(appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.wn = normalize(mul((float3x3)unity_ObjectToWorld, v.normal)); o.wp = mul(unity_ObjectToWorld, v.vertex).xyz; return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                float3 V = normalize(_WorldSpaceCameraPos - i.wp), N = normalize(i.wn), S = normalize(_SunDir.xyz);
                float ls = dot(N, S);
                if (_Mode < 0.5)
                {
                    float d = -dot(N, V);
                    float rim = pow(smoothstep(0.0, 0.36, d), 2.2);
                    float day = smoothstep(-0.25, 0.35, ls), fwd = pow(max(dot(-V, S), 0.0), 6.0) * smoothstep(-0.5, 0.1, ls);
                    float3 c = lerp(float3(1.0, 0.5, 0.25), _Col.rgb, smoothstep(-0.05, 0.4, ls));
                    return float4(c * rim * (0.03 + 1.6 * day + 2.2 * fwd), 1.0);
                }
                float f = pow(1.0 - max(dot(N, V), 0.0), 3.0);
                float day2 = smoothstep(-0.2, 0.4, ls);
                return float4(lerp(float3(1.0, 0.55, 0.3), _Col.rgb, smoothstep(0.0, 0.4, ls)) * f * (0.02 + 1.4 * day2), 1.0);
            }
            ENDCG
        }
    }
}
