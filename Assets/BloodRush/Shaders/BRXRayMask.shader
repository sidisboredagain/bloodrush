// Marks (stencil bit 64) the pixels where a highlighted enemy is directly visible, so BR/XRay leaves them alone.
// Draws no colour; runs before BR/XRay (lower queue).
Shader "BR/XRayMask"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+20" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ColorMask 0
            ZWrite Off
            ZTest LEqual
            Cull Off
            Stencil { Ref 64 WriteMask 64 Comp Always Pass Replace }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert (float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            float4 frag () : SV_Target { return 0; }
            ENDCG
        }
    }
}
