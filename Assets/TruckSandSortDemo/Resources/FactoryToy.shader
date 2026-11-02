Shader "TruckSort/Moulded plastic" {
 Properties { _Color ("Plastic",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"} Pass {
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
 struct A{float4 vertex:POSITION;float3 normal:NORMAL;};
 struct V{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;};
 float4 _Color;
 V vert(A a){V o;o.world=TransformObjectToWorld(a.vertex.xyz);o.pos=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normal);return o;}
 half4 frag(V i):SV_Target{
 float3 n=normalize(i.normal),v=normalize(float3(0,.559,-.829)),l=normalize(float3(-.48,.85,-.35));
 float k=dot(n,l),wrap=saturate(.48+.52*k);
 float3 c=_Color.rgb;
 #ifndef UNITY_COLORSPACE_GAMMA
 c=LinearToSRGB(c);
 #endif
 c=c*(.55+.47*wrap)+float3(.035,.009,.065)*(1-wrap);
 float spec=pow(saturate(dot(n,normalize(v+l))),29)*.22;
 float rim=pow(1-saturate(abs(dot(n,v))),3)*(.045+.10*saturate(n.y));
 c=lerp(c,float3(1,.91,1),saturate(spec+rim+.07*saturate(k)*saturate(n.y)));
 #ifndef UNITY_COLORSPACE_GAMMA
 c=SRGBToLinear(c);
 #endif
 return half4(c,1);}
 ENDHLSL
 } }
}

