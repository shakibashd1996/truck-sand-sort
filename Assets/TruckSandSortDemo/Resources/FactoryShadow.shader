Shader "TruckSort/Contact" {
 Properties {_Color("Shadow",Color)=(.06,.015,.15,.4)}
 SubShader { Tags {"Queue"="Transparent" "RenderPipeline"="UniversalPipeline"} Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};float4 _Color;
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv*2-1;return o;}
 half4 frag(V i):SV_Target{float r=pow(pow(abs(i.uv.x),4)+pow(abs(i.uv.y),4),.25);return half4(_Color.rgb,_Color.a*(1-smoothstep(.52,1,r)));}
 ENDHLSL
 } }
}

