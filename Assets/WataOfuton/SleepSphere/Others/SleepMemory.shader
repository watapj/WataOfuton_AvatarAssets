Shader "WataOfuton/SleepMemory"
{
Properties
{
    [NoScaleOffset]_MainTex ("Memory Texture", 2D) = "black" {}
    // [HideInInspector]_W("Pixel Width", Float) = 16.0
    _MoveDist("Reset Move Distance", Float) = 0.2
}
SubShader
{
Tags {
    "Queue"="Overlay+1000"
    "DisableBatching" = "True"
    "IgnoreProjector" = "True" 
    }
ZWrite Off
ZTest Always
Cull Off

Pass
{
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"

static float4 jackpos[4] =
{
    float4( 1.0,-1.0, 0.0, 1.0),
    float4(-1.0,-1.0, 0.0, 1.0),
    float4( 1.0, 1.0, 0.0, 1.0),
    float4(-1.0, 1.0, 0.0, 1.0),
};
static float2 jackuv[4] =
{
    float2( 1.0, 1.0),
    float2( 0.0, 1.0),
    float2( 1.0, 0.0),
    float2( 0.0, 0.0),
};

struct appdata
{
    float4 vertex : POSITION;
    uint   vID	  : SV_VertexID;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

sampler2D _MainTex;
static const float _W = 16.0;
float _MoveDist;

v2f vert (appdata v)
{
    v2f o = (v2f)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    
    // o.vertex = UNITY_MATRIX_P[3][3] ? jackpos[v.vID] : UnityObjectToClipPos(v.vertex);
    // o.uv = UNITY_MATRIX_P[3][3] ? jackuv[v.vID] : v.uv;
    o.vertex = UNITY_MATRIX_P[3][3] ? jackpos[v.vID] : -1.0;
    o.vertex = (0.1-_ProjectionParams.z)>=0.0 ? o.vertex : -1.0;
    o.uv = jackuv[v.vID];
    return o;
}

float3 pack(float3 xyz, uint ix) {
    uint3 xyzI = asuint(xyz);
    xyzI = (xyzI >> (ix * 8)) % 256;
    return (float3(xyzI) + 0.5) / 255.0;
}

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

float4 frag (v2f i) : SV_Target
{
    // clip(0.1 - _ProjectionParams.z);
    float texWidth = _W;

    float2 intuv = i.uv;
    float intx = intuv.x*texWidth - frac(intuv.x*texWidth);
    intuv.x = (intx-fmod(intx,4.0)+2.0) / texWidth;
    float xmod = fmod(intx, 4.0);

    float4 col = 0.0;

    if(intuv.x>(7.0/texWidth)) { 
        float3 pos1f = unpack(intuv-float2(4.0/texWidth,0));
        col = float4(pack(pos1f, (uint)xmod), 1.0);
    }else if(intuv.x>(3.0/texWidth)){
        float3 pos = mul(UNITY_MATRIX_M, float4(0,0,0,1));
        col = float4(pack(pos, (uint)xmod), 1.0);
    }else{
        //一番左のpixelなら、時間を計算する
        float3 pos  = mul(UNITY_MATRIX_M, float4(0,0,0,1));
        float3 pos4f = unpack(intuv + float2(12.0 / texWidth, 0.5));

        float3 time = unpack(float2(2.0/_W, 0.5));
        if(length(pos-pos4f)<_MoveDist){
            time += unity_DeltaTime.x;
        }else{
            time = 0.0;
        }
        col = float4(pack(time, xmod), 1.0);
    }

    return col;
}
ENDCG
}
}
}
