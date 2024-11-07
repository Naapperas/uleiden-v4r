#ifndef MESH_TRIPLANAR_CGINC_INCLUDED
#define MESH_TRIPLANAR_CGINC_INCLUDED

struct Input
{
	float4 vertColor;
	float4 vertUV1;
	UNITY_FOG_COORDS(5)
	float3 worldPos;
	float3 vertNormal;
};

float _tiles0x;
float _tiles0y;
float _tiles1x;
float _tiles1y;
float _tiles2x;
float _tiles2y;
float _tiles3x;
float _tiles3y;
float _tiles4x;
float _tiles4y;
float _tiles5x;
float _tiles5y;

float _offset0x;
float _offset0y;
float _offset1x;
float _offset1y;
float _offset2x;
float _offset2y;
float _offset3x;
float _offset3y;
float _offset4x;
float _offset4y;
float _offset5x;
float _offset5y;

float _normalScale0;
float _normalScale1;
float _normalScale2;
float _normalScale3;
float _normalScale4;
float _normalScale5;

sampler2D _Splat0, _Splat1, _Splat2, _Splat3, _Splat4, _Splat5;
sampler2D _Normal0, _Normal1, _Normal2, _Normal3, _Normal4, _Normal5;

fixed3 normal;
float3 worldPos;

void SplatmapVert(inout appdata_full v, out Input data)
{
	UNITY_INITIALIZE_OUTPUT(Input, data);
	data.vertColor = v.color;
	data.vertUV1 = v.texcoord1;
	float4 pos = UnityObjectToClipPos (v.vertex); 
	UNITY_TRANSFER_FOG(data, pos);
	v.tangent.xyz = cross(v.normal, float3(0,0,1));
	v.tangent.w = -1;
	data.vertNormal = v.normal;
}

void SplatmapMix(Input IN, 
                half4 defaultAlpha03, 
                half4 defaultAlpha47, 
                out half4 splat_control03, 
                out half4 splat_control47, 
                out half weight, 
                out fixed4 mixedDiffuse, 
                inout fixed3 mixedNormal)
{
    splat_control03 = IN.vertColor;
    splat_control47 = IN.vertUV1;
	weight = dot(splat_control03, half4(1,1,1,1)) + 
	         dot(splat_control47, half4(1,1,1,1));
	worldPos = IN.worldPos;
	normal = abs(IN.vertNormal);
	// Normalize weights before lighting and restore weights in final modifier functions so that the overal
	// lighting result can be correctly weighted.
	splat_control03 /= (weight + 1e-3f);
	splat_control47 /= (weight + 1e-3f);
	mixedDiffuse = 0.0f;
	
    float y = normal.y;
    float z = normal.z;
    
    float2 tile = float2(_tiles0x, _tiles0y);
    float2 offset = float2(_offset0x, _offset0y);
    float2 y0 = IN.worldPos.zy * tile + offset;
    float2 x0 = IN.worldPos.xz * tile + offset;
    float2 z0 = IN.worldPos.xy * tile + offset;
    fixed4 cX0 = tex2D(_Splat0, y0);
    fixed4 cY0 = tex2D(_Splat0, x0);
    fixed4 cZ0 = tex2D(_Splat0, z0);
    float4 side0 = lerp(cX0, cZ0, z);
    float4 top0 = lerp(side0, cY0, y);
    mixedDiffuse += splat_control03.r * top0 * half4(1.0, 1.0, 1.0, defaultAlpha03.r);
    
    tile = float2(_tiles1x, _tiles1y);
    offset = float2(_offset1x, _offset1y);
    float2 y1 = IN.worldPos.zy * tile + offset;
    float2 x1 = IN.worldPos.xz * tile + offset;
    float2 z1 = IN.worldPos.xy * tile + offset;
    fixed4 cX1 = tex2D(_Splat1, y1);
    fixed4 cY1 = tex2D(_Splat1, x1);
    fixed4 cZ1 = tex2D(_Splat1, z1);
    float4 side1 = lerp(cX1, cZ1, z);
    float4 top1 = lerp(side1, cY1, y);
    mixedDiffuse += splat_control03.g * top1 * half4(1.0, 1.0, 1.0, defaultAlpha03.g); 

    tile = float2(_tiles2x, _tiles2y);
    offset = float2(_offset2x, _offset2y);
    float2 y2 = IN.worldPos.zy * tile + offset;
    float2 x2 = IN.worldPos.xz * tile + offset;
    float2 z2 = IN.worldPos.xy * tile + offset;
    fixed4 cX2 = tex2D(_Splat2, y2);
    fixed4 cY2 = tex2D(_Splat2, x2);
    fixed4 cZ2 = tex2D(_Splat2, z2);
    float4 side2 = lerp(cX2, cZ2, z);
    float4 top2 = lerp(side2, cY2, y);
    mixedDiffuse += splat_control03.b * top2 * half4(1.0, 1.0, 1.0, defaultAlpha03.b); 
    
    tile = float2(_tiles3x, _tiles3y);
    offset = float2(_offset3x, _offset3y);
    float2 y3 = IN.worldPos.zy * tile + offset;
    float2 x3 = IN.worldPos.xz * tile + offset;
    float2 z3 = IN.worldPos.xy * tile + offset;
    fixed4 cX3 = tex2D(_Splat3, y3);
    fixed4 cY3 = tex2D(_Splat3, x3);
    fixed4 cZ3 = tex2D(_Splat3, z3);
    float4 side3 = lerp(cX3, cZ3, z);
    float4 top3 = lerp(side3, cY3, y);
    mixedDiffuse += splat_control03.a * top3 * half4(1.0, 1.0, 1.0, defaultAlpha03.a); 
    
    tile = float2(_tiles4x, _tiles4y);
    offset = float2(_offset4x, _offset4y);
    float2 y4 = IN.worldPos.zy * tile + offset;
    float2 x4 = IN.worldPos.xz * tile + offset;
    float2 z4 = IN.worldPos.xy * tile + offset;
    fixed4 cX4 = tex2D(_Splat4, y4);
    fixed4 cY4 = tex2D(_Splat4, x4);
    fixed4 cZ4 = tex2D(_Splat4, z4);
    float4 side4 = lerp(cX4, cZ4, z);
    float4 top4 = lerp(side4, cY4, y);
    mixedDiffuse += splat_control47.r * top4 * half4(1.0, 1.0, 1.0, defaultAlpha47.r); 
    
    tile = float2(_tiles5x, _tiles5y);
    offset = float2(_offset5x, _offset5y);
    float2 y5 = IN.worldPos.zy * tile + offset;
    float2 x5 = IN.worldPos.xz * tile + offset;
    float2 z5 = IN.worldPos.xy * tile + offset;
    fixed4 cX5 = tex2D(_Splat5, y5);
    fixed4 cY5 = tex2D(_Splat5, x5);
    fixed4 cZ5 = tex2D(_Splat5, z5);
    float4 side5 = lerp(cX5, cZ5, z);
    float4 top5 = lerp(side5, cY5, y);
    mixedDiffuse += splat_control47.g * top5 * half4(1.0, 1.0, 1.0, defaultAlpha47.g); 
    
    
    
    // NORMAL 
    fixed4 nX0 = tex2D(_Normal0, y0);
    fixed4 nY0 = tex2D(_Normal0, x0);
    fixed4 nZ0 = tex2D(_Normal0, z0);
    float4 side0n = lerp(nX0, nZ0, z);
    float4 top0n = lerp(side0n, nY0, y);
    mixedNormal = splat_control03.r * UnpackNormalWithScale(top0n, _normalScale0);
    
    fixed4 nX1 = tex2D(_Normal1, y1);
    fixed4 nY1 = tex2D(_Normal1, x1);
    fixed4 nZ1 = tex2D(_Normal1, z1);
    float4 side1n = lerp(nX1, nZ1, z);
    float4 top1n = lerp(side1n, nY1, y);
    mixedNormal += splat_control03.g * UnpackNormalWithScale(top1n, _normalScale1);
    
    fixed4 nX2 = tex2D(_Normal2, y2);
    fixed4 nY2 = tex2D(_Normal2, x2);
    fixed4 nZ2 = tex2D(_Normal2, z2);
    float4 side2n = lerp(nX2, nZ2, z);
    float4 top2n = lerp(side2n, nY2, y);
    mixedNormal += splat_control03.b * UnpackNormalWithScale(top2n, _normalScale2);

    fixed4 nX3 = tex2D(_Normal3, y3);
    fixed4 nY3 = tex2D(_Normal3, x3);
    fixed4 nZ3 = tex2D(_Normal3, z3);
    float4 side3n = lerp(nX3, nZ3, z);
    float4 top3n = lerp(side3n, nY3, y);
    mixedNormal += splat_control03.a * UnpackNormalWithScale(top3n, _normalScale3);
    
    fixed4 nX4 = tex2D(_Normal4, y4);
    fixed4 nY4 = tex2D(_Normal4, x4);
    fixed4 nZ4 = tex2D(_Normal4, z4);
    float4 side4n = lerp(nX4, nZ4, z);
    float4 top4n = lerp(side4n, nY4, y);
    mixedNormal += splat_control47.r * UnpackNormalWithScale(top4n, _normalScale4);
    
    fixed4 nX5 = tex2D(_Normal5, y5);
    fixed4 nY5 = tex2D(_Normal5, x5);
    fixed4 nZ5 = tex2D(_Normal5, z5);
    float4 side5n = lerp(nX5, nZ5, z);
    float4 top5n = lerp(side5n, nY5, y);
    mixedNormal += splat_control47.g * UnpackNormalWithScale(top5n, _normalScale5);

    mixedNormal.z += 1e-5f; // to avoid nan after normalizing
}

#ifndef TERRAIN_SURFACE_OUTPUT
    #define TERRAIN_SURFACE_OUTPUT SurfaceOutput
#endif

void SplatmapFinalColor(Input IN, TERRAIN_SURFACE_OUTPUT o, inout fixed4 color)
{
    color *= o.Alpha;
    UNITY_APPLY_FOG(IN.fogCoord, color);
}

void SplatmapFinalPrepass(Input IN, TERRAIN_SURFACE_OUTPUT o, inout fixed4 normalSpec)
{
    normalSpec *= o.Alpha;
}

void SplatmapFinalGBuffer(Input IN, TERRAIN_SURFACE_OUTPUT o, inout half4 outGBuffer0, inout half4 outGBuffer1, inout half4 outGBuffer2, inout half4 emission)
{
    UnityStandardDataApplyWeightToGbuffer(outGBuffer0, outGBuffer1, outGBuffer2, o.Alpha);
    emission *= o.Alpha;
}

#endif // MESH_TRIPLANAR_CGINC_INCLUDED