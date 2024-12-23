Shader "WataOfuton/SleepSphere"
{
Properties
{
    [NoScaleOffset]_MainTex ("Memory Texture", 2D) = "white" {}
    // [HideInInspector]_W("Pixel Width", Float) = 16.0
    // [Toggle]_useSDK2("Use SDK2", int) = 0
    _WaitTime("Wait Time (Minutes)", Float) = 30.0
    _Scale("Scale", Float) = 2.0
    _Alpha("Alpha", Range(0.0, 1.0)) = 0.95
}
SubShader
{
Tags {
    "Queue"="Transparent"
    "DisableBatching" = "True"
    "IgnoreProjector" = "True" 
    }
//LOD 100
Blend SrcAlpha OneMinusSrcAlpha
Cull Off
// ZWrite Off

Pass
{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    //float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    //float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    nointerpolation float time : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

static const float _W = 16.0;
sampler2D _MainTex;
float _WaitTime, _Scale, _Alpha;
// int _useSDK2;
uniform float _VRChatCameraMode;
uniform float _VRChatMirrorMode;

float3 unpack(float2 uv) {
    float texWidth = _W;
    float3 e = float3(1.0/texWidth/2, 3.0/texWidth/2, 0);
    uint3 v0 = uint3(tex2Dlod(_MainTex, float4(uv - e.yz,0,0)).xyz * 255.) << 0;
    uint3 v1 = uint3(tex2Dlod(_MainTex, float4(uv - e.xz,0,0)).xyz * 255.) << 8;
    uint3 v2 = uint3(tex2Dlod(_MainTex, float4(uv + e.xz,0,0)).xyz * 255.) << 16;
    uint3 v3 = uint3(tex2Dlod(_MainTex, float4(uv + e.yz,0,0)).xyz * 255.) << 24;
    uint3 v = v0 + v1 + v2 + v3;
    return asfloat(v);
}

v2f vert (appdata v)
{
    v2f o = (v2f)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    o.time = unpack(float2(2.0/_W, 0.5)).x;
    v.vertex.xyz *= o.time>(_WaitTime*60.0) ? _Scale : 0.0;
    o.vertex = UnityObjectToClipPos(v.vertex);
    //o.uv = v.uv;
    return o;
}

float4 frag (v2f i) : SV_Target
{
    float c = -1;
    // https://docs.vrchat.com/docs/vrchat-202231#features-1
    if(_VRChatMirrorMode==0.0 && _VRChatCameraMode==0.0) c = 1.0;
    clip(c);

    return float4(0, 0, 0, _Alpha*saturate(i.time-_WaitTime*60.0));
}
ENDCG
}
}
}
