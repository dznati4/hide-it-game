Shader "Hide iT/Guard Sight"
{
    Properties { _Color ("Sight color", Color) = (1,.67,.18,.28) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct Input { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct Varying { float4 position : SV_POSITION; fixed alpha : TEXCOORD0; };
            Varying vert(Input input)
            {
                Varying output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.alpha = input.color.a;
                return output;
            }
            fixed4 frag(Varying input) : SV_Target { return fixed4(_Color.rgb,_Color.a*input.alpha); }
            ENDCG
        }
    }
}
